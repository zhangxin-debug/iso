using ISO11820.Config;
using ISO11820.Models;
using ISO11820.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace ISO11820.Core
{
    public class DataBroadcastEventArgs : EventArgs
    {
        public SensorData SensorData { get; set; } = new();
        public TestState CurrentState { get; set; }
        public List<MasterMessage> Messages { get; set; } = new();
        public int ElapsedSeconds { get; set; }
        public bool IsStable { get; set; }
    }

    public class TestMaster
    {
        private readonly SensorSimulator _simulator;
        private readonly System.Timers.Timer _timer;
        private readonly List<MasterMessage> _messages = new();
        private readonly List<double> _tf1History = new();
        private readonly List<SensorData> _recordedData = new();

        public string? CurrentProductId { get; set; }
        public string? CurrentTestId { get; set; }
        public string? CurrentOperator { get; set; }
        public double PreWeight { get; set; }
        public double AmbTemp { get; set; }
        public double AmbHumi { get; set; }

        public TestState CurrentState { get; private set; } = TestState.Idle;
        public bool IsStable { get; private set; } = false;
        private int _stableCounter = 0;
        public int ElapsedSeconds { get; private set; } = 0;

        private readonly double _stableThreshold;
        private readonly int _targetDuration = 3600;
        private readonly string _baseDirectory;

        public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

        public TestMaster()
        {
            _simulator = new SensorSimulator();
            _stableThreshold = AppConfig.StableThreshold;
            _baseDirectory = AppConfig.BaseDirectory;

            _timer = new System.Timers.Timer(800);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void StartHeating()
        {
            if (CurrentState != TestState.Idle) return;
            CurrentState = TestState.Preparing;
            _simulator.SetState(CurrentState);
            IsStable = false;
            _stableCounter = 0;
            AddMessage("开始升温，系统升温中");
        }

        public void StopHeating()
        {
            if (CurrentState == TestState.Recording)
            {
                StopRecording();
            }
            CurrentState = TestState.Idle;
            _simulator.SetState(CurrentState);
            IsStable = false;
            _stableCounter = 0;
            AddMessage("停止加热");
        }

        public void StartRecording()
        {
            if (CurrentState != TestState.Ready) return;
            CurrentState = TestState.Recording;
            _simulator.SetState(CurrentState);
            ElapsedSeconds = 0;
            _recordedData.Clear();
            AddMessage("开始记录，计时开始");
        }

        public void StopRecording()
        {
            if (CurrentState != TestState.Recording) return;
            CurrentState = TestState.Complete;
            _simulator.SetState(CurrentState);
            AddMessage("用户手动停止记录");
        }

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            var data = _simulator.Update();

            if (CurrentState == TestState.Preparing)
            {
                if (data.TF1 >= 747)
                {
                    _stableCounter++;
                    if (_stableCounter > 3)
                    {
                        IsStable = true;
                        if (data.TF1 >= 745 && data.TF1 <= 755 && IsStable)
                        {
                            CurrentState = TestState.Ready;
                            _simulator.SetState(CurrentState);
                            AddMessage("温度已稳定，可以开始记录");
                        }
                    }
                }
                else
                {
                    _stableCounter = 0;
                    IsStable = false;
                }
            }

            if (CurrentState == TestState.Recording)
            {
                ElapsedSeconds++;
                _recordedData.Add(data);
                SaveToCsv(data);

                if (ElapsedSeconds >= _targetDuration)
                {
                    CurrentState = TestState.Complete;
                    _simulator.SetState(CurrentState);
                    AddMessage("记录时间到达 3600 秒，试验自动结束");
                }
            }

            _tf1History.Add(data.TF1);
            if (_tf1History.Count > 600) _tf1History.RemoveAt(0);

            var args = new DataBroadcastEventArgs
            {
                SensorData = data,
                CurrentState = CurrentState,
                Messages = new List<MasterMessage>(_messages),
                ElapsedSeconds = ElapsedSeconds,
                IsStable = IsStable
            };
            _messages.Clear();

            DataBroadcast?.Invoke(this, args);
        }

        private void AddMessage(string msg)
        {
            _messages.Add(new MasterMessage
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = msg
            });
        }

        private void SaveToCsv(SensorData data)
        {
            if (CurrentProductId == null || CurrentTestId == null) return;

            string dir = Path.Combine(_baseDirectory, "TestData", CurrentProductId, CurrentTestId);
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "sensor_data.csv");

            bool fileExists = File.Exists(path);
            using var writer = new StreamWriter(path, append: true);
            if (!fileExists)
            {
                writer.WriteLine("Time,Temp1,Temp2,TempSurface,TempCenter,TempCalibration");
            }
            writer.WriteLine($"{ElapsedSeconds},{data.TF1},{data.TF2},{data.TS},{data.TC},{data.TCal}");
        }

        public double GetTemperatureDrift()
        {
            if (_tf1History.Count < 10) return 0;
            int count = Math.Min(_tf1History.Count, 600);
            var recent = _tf1History.Skip(_tf1History.Count - count).ToList();
            if (recent.Count < 2) return 0;
            double slope = (recent.Last() - recent.First()) / recent.Count * 600;
            return Math.Round(slope, 2);
        }

        public (double lostWeight, double lostWeightPer, double deltaTf,
                double maxTf1, double maxTf2, double maxTs, double maxTc,
                double finalTf1, double finalTf2, double finalTs, double finalTc,
                double deltaTf1, double deltaTf2, double deltaTs, double deltaTc) CalculateResults(double postWeight)
        {
            if (_recordedData.Count == 0) return (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            double lostWeight = PreWeight - postWeight;
            double lostWeightPer = PreWeight > 0 ? (lostWeight / PreWeight * 100) : 0;

            double maxTf1 = _recordedData.Max(d => d.TF1);
            double maxTf2 = _recordedData.Max(d => d.TF2);
            double maxTs = _recordedData.Max(d => d.TS);
            double maxTc = _recordedData.Max(d => d.TC);

            var last = _recordedData.Last();
            double finalTf1 = last.TF1;
            double finalTf2 = last.TF2;
            double finalTs = last.TS;
            double finalTc = last.TC;

            double deltaTf1 = finalTf1 - AmbTemp;
            double deltaTf2 = finalTf2 - AmbTemp;
            double deltaTs = finalTs - AmbTemp;
            double deltaTc = finalTc - AmbTemp;
            double deltaTf = deltaTs;

            return (lostWeight, lostWeightPer, deltaTf,
                    maxTf1, maxTf2, maxTs, maxTc,
                    finalTf1, finalTf2, finalTs, finalTc,
                    deltaTf1, deltaTf2, deltaTs, deltaTc);
        }

        public List<SensorData> GetRecordedData() => new List<SensorData>(_recordedData);
    }
}