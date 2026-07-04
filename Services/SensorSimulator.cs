using ISO11820.Config;
using ISO11820.Models;
using System;

namespace ISO11820.Services
{
    public class SensorSimulator
    {
        private readonly Random _random = new Random();
        private double _tf1;
        private double _tf2;
        private double _ts;
        private double _tc;
        private double _tcal;

        private readonly double _targetTemp;
        private readonly double _heatingRate;
        private readonly double _fluctuation;
        private readonly double _stableThreshold;

        private TestState _currentState = TestState.Idle;

        public SensorSimulator()
        {
            _targetTemp = AppConfig.TargetFurnaceTemp;
            _heatingRate = AppConfig.HeatingRatePerSecond;
            _fluctuation = AppConfig.TempFluctuation;
            _stableThreshold = AppConfig.StableThreshold;

            _tf1 = AppConfig.InitialFurnaceTemp;
            _tf2 = _tf1 + Noise();
            _ts = _tf1 * 0.3 + Noise();
            _tc = _tf1 * 0.25 + Noise();
            _tcal = _tf1 + Noise() * 2;
        }

        public void SetState(TestState state)
        {
            _currentState = state;
        }

        public SensorData Update()
        {
            double noise = Noise();

            switch (_currentState)
            {
                case TestState.Idle:
                    _tf1 -= 0.5 + noise * 0.1;
                    _tf2 -= 0.5 + noise * 0.1;
                    if (_tf1 < 25) _tf1 = 25;
                    if (_tf2 < 25) _tf2 = 25;
                    _ts = _tf1 * 0.3 + noise;
                    _tc = _tf1 * 0.25 + noise;
                    break;

                case TestState.Preparing:
                    if (_tf1 < _targetTemp - _stableThreshold)
                    {
                        _tf1 += _heatingRate * 0.8 + noise;
                        _tf2 += _heatingRate * 0.8 + noise * 0.8;
                        _ts = _tf1 * 0.3 + noise;
                        _tc = _tf1 * 0.25 + noise;
                    }
                    else
                    {
                        _tf1 = _targetTemp + noise;
                        _tf2 = _targetTemp + noise * 0.8;
                        _ts = _tf1 * 0.3 + noise;
                        _tc = _tf1 * 0.25 + noise;
                    }
                    break;

                case TestState.Ready:
                    _tf1 = _targetTemp + noise;
                    _tf2 = _targetTemp + noise * 0.8;
                    _ts = _tf1 * 0.3 + noise;
                    _tc = _tf1 * 0.25 + noise;
                    break;

                case TestState.Recording:
                    _tf1 = _targetTemp + noise;
                    _tf2 = _targetTemp + noise * 0.8;

                    double surfaceTarget = Math.Min(_tf1 * 0.95, 800);
                    _ts += (surfaceTarget - _ts) * 0.02 + noise;

                    double centerTarget = Math.Min(_tf1 * 0.85, 750);
                    _tc += (centerTarget - _tc) * 0.01 + noise;
                    break;

                case TestState.Complete:
                    break;
            }

            _tcal = _tf1 + noise * 2;

            return new SensorData
            {
                TF1 = Math.Round(_tf1, 1),
                TF2 = Math.Round(_tf2, 1),
                TS = Math.Round(_ts, 1),
                TC = Math.Round(_tc, 1),
                TCal = Math.Round(_tcal, 1)
            };
        }

        private double Noise()
        {
            return (_random.NextDouble() * 2 - 1) * _fluctuation;
        }
    }
}