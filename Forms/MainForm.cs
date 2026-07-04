using ISO11820.Config;
using ISO11820.Core;
using ISO11820.DataAccess;
using ISO11820.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ISO11820.Forms
{
    public class MainForm : Form
    {
        private TestMaster _testMaster;
        private DbHelper _dbHelper;

        private Label lblStatus;
        private Label lblTimer;
        private Label lblTF1, lblTF2, lblTS, lblTC, lblTCal;
        private Label lblDrift;
        private Label lblProductId;
        private RichTextBox richTextBoxLog;
        private Button btnNewTest, btnStartHeat, btnStopHeat, btnStartRecord, btnStopRecord;
        private TabControl tabControl;
        private PlotView plotView;

        private string _currentUser;

        public MainForm(string currentUser, DbHelper dbHelper)
        {
            _currentUser = currentUser;
            _dbHelper = dbHelper;
            _testMaster = new TestMaster();
            _testMaster.DataBroadcast += OnDataBroadcast;

            InitializeComponent();
            _testMaster.Start();

            AddLog("系统初始化，操作员：" + currentUser, Color.White);
        }

        private void InitializeComponent()
        {
            this.Text = "ISO 11820 建筑材料不燃性试验系统";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            lblStatus = new Label
            {
                Text = "状态：空闲",
                ForeColor = Color.LightGreen,
                AutoSize = true,
                Location = new Point(20, 10),
                Font = new Font("微软雅黑", 12, FontStyle.Bold)
            };

            lblTimer = new Label
            {
                Text = "计时：0 秒",
                ForeColor = Color.Yellow,
                AutoSize = true,
                Location = new Point(300, 10),
                Font = new Font("微软雅黑", 12, FontStyle.Bold)
            };

            lblProductId = new Label
            {
                Text = "样品编号：--",
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(500, 10),
                Font = new Font("微软雅黑", 10)
            };

            panelTop.Controls.Add(lblStatus);
            panelTop.Controls.Add(lblTimer);
            panelTop.Controls.Add(lblProductId);

            var panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            int y = 20;
            lblTF1 = CreateTempLabel("炉温1", Color.Red, ref y);
            lblTF2 = CreateTempLabel("炉温2", Color.Orange, ref y);
            lblTS = CreateTempLabel("表面温", Color.Cyan, ref y);
            lblTC = CreateTempLabel("中心温", Color.Lime, ref y);
            lblTCal = CreateTempLabel("校准温", Color.Gray, ref y);

            y += 20;
            var lblDriftTitle = new Label
            {
                Text = "温度漂移",
                ForeColor = Color.White,
                Location = new Point(20, y),
                AutoSize = true
            };
            panelLeft.Controls.Add(lblDriftTitle);
            y += 25;

            lblDrift = new Label
            {
                Text = "0.00 °C/10min",
                ForeColor = Color.Yellow,
                Location = new Point(20, y),
                AutoSize = true,
                Font = new Font("Consolas", 14, FontStyle.Bold)
            };
            panelLeft.Controls.Add(lblDrift);

            panelLeft.Controls.Add(lblTF1);
            panelLeft.Controls.Add(lblTF2);
            panelLeft.Controls.Add(lblTS);
            panelLeft.Controls.Add(lblTC);
            panelLeft.Controls.Add(lblTCal);

            var panelRight = new Panel
            {
                Dock = DockStyle.Right,
                Width = 150,
                BackColor = Color.FromArgb(40, 40, 40)
            };

            y = 20;
            btnNewTest = CreateButton("新建试验", ref y, BtnNewTest_Click);
            btnStartHeat = CreateButton("开始升温", ref y, BtnStartHeat_Click);
            btnStopHeat = CreateButton("停止升温", ref y, BtnStopHeat_Click);
            btnStartRecord = CreateButton("开始记录", ref y, BtnStartRecord_Click);
            btnStopRecord = CreateButton("停止记录", ref y, BtnStopRecord_Click);

            panelRight.Controls.Add(btnNewTest);
            panelRight.Controls.Add(btnStartHeat);
            panelRight.Controls.Add(btnStopHeat);
            panelRight.Controls.Add(btnStartRecord);
            panelRight.Controls.Add(btnStopRecord);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var tabRealTime = new TabPage("实时曲线");
            plotView = new PlotView
            {
                Dock = DockStyle.Fill,
                Model = CreatePlotModel()
            };
            tabRealTime.Controls.Add(plotView);

            var tabMessages = new TabPage("系统消息");
            richTextBoxLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10),
                ReadOnly = true
            };
            tabMessages.Controls.Add(richTextBoxLog);

            var tabHistory = new TabPage("记录查询");
            var lblHistory = new Label
            {
                Text = "记录查询功能待实现",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            tabHistory.Controls.Add(lblHistory);

            tabControl.TabPages.Add(tabRealTime);
            tabControl.TabPages.Add(tabMessages);
            tabControl.TabPages.Add(tabHistory);

            var panelCenter = new Panel
            {
                Dock = DockStyle.Fill
            };
            panelCenter.Controls.Add(tabControl);

            this.Controls.Add(panelCenter);
            this.Controls.Add(panelRight);
            this.Controls.Add(panelLeft);
            this.Controls.Add(panelTop);

            UpdateButtonStates();
        }

        private Label CreateTempLabel(string name, Color color, ref int y)
        {
            var lbl = new Label
            {
                Text = $"{name}\n--.- °C",
                ForeColor = color,
                Location = new Point(20, y),
                Size = new Size(200, 50),
                Font = new Font("Consolas", 16, FontStyle.Bold)
            };
            y += 60;
            return lbl;
        }

        private Button CreateButton(string text, ref int y, EventHandler click)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(15, y),
                Size = new Size(120, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.Click += click;
            y += 50;
            return btn;
        }

        private PlotModel CreatePlotModel()
        {
            var model = new PlotModel { Title = "温度实时曲线", TitleColor = OxyColors.White };
            model.PlotAreaBorderColor = OxyColors.Gray;

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "时间（秒）",
                Minimum = 0,
                Maximum = 600,
                TitleColor = OxyColors.White,
                TextColor = OxyColors.White,
                TicklineColor = OxyColors.Gray
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "温度（°C）",
                Minimum = 0,
                Maximum = 800,
                TitleColor = OxyColors.White,
                TextColor = OxyColors.White,
                TicklineColor = OxyColors.Gray
            });

            model.Series.Add(new LineSeries { Title = "炉温1", Color = OxyColors.Red });
            model.Series.Add(new LineSeries { Title = "炉温2", Color = OxyColors.Orange });
            model.Series.Add(new LineSeries { Title = "表面温", Color = OxyColors.Cyan });
            model.Series.Add(new LineSeries { Title = "中心温", Color = OxyColors.Lime });

            return model;
        }

        private void OnDataBroadcast(object? sender, DataBroadcastEventArgs e)
        {
            this.Invoke(() =>
            {
                lblTF1.Text = $"炉温1\n{e.SensorData.TF1:F1} °C";
                lblTF2.Text = $"炉温2\n{e.SensorData.TF2:F1} °C";
                lblTS.Text = $"表面温\n{e.SensorData.TS:F1} °C";
                lblTC.Text = $"中心温\n{e.SensorData.TC:F1} °C";
                lblTCal.Text = $"校准温\n{e.SensorData.TCal:F1} °C";

                lblStatus.Text = "状态：" + GetStateText(e.CurrentState);
                lblTimer.Text = $"计时：{e.ElapsedSeconds} 秒";

                double drift = _testMaster.GetTemperatureDrift();
                lblDrift.Text = $"{drift:F2} °C/10min";

                UpdatePlot(e);
                UpdateButtonStates();

                foreach (var msg in e.Messages)
                {
                    Color color = msg.Message.Contains("终止") ? Color.Yellow : Color.White;
                    AddLog($"{msg.Time}  {msg.Message}", color);
                }
            });
        }

        private void UpdatePlot(DataBroadcastEventArgs e)
        {
            var model = plotView.Model;
            if (model == null) return;

            ((LineSeries)model.Series[0]).Points.Add(new DataPoint(e.ElapsedSeconds, e.SensorData.TF1));
            ((LineSeries)model.Series[1]).Points.Add(new DataPoint(e.ElapsedSeconds, e.SensorData.TF2));
            ((LineSeries)model.Series[2]).Points.Add(new DataPoint(e.ElapsedSeconds, e.SensorData.TS));
            ((LineSeries)model.Series[3]).Points.Add(new DataPoint(e.ElapsedSeconds, e.SensorData.TC));

            foreach (var series in model.Series.OfType<LineSeries>())
            {
                while (series.Points.Count > 600)
                    series.Points.RemoveAt(0);
            }

            if (model.Axes.Count > 0)
            {
                double maxX = e.ElapsedSeconds;
                model.Axes[0].Minimum = Math.Max(0, maxX - 600);
                model.Axes[0].Maximum = maxX + 10;
            }

            plotView.InvalidatePlot(true);
        }

        private string GetStateText(TestState state)
        {
            return state switch
            {
                TestState.Idle => "空闲",
                TestState.Preparing => "升温中",
                TestState.Ready => "就绪",
                TestState.Recording => "记录中",
                TestState.Complete => "完成",
                _ => "未知"
            };
        }

        private void AddLog(string message, Color color)
        {
            richTextBoxLog.SelectionStart = richTextBoxLog.TextLength;
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.AppendText(message + "\n");
            richTextBoxLog.ScrollToCaret();
        }

        private void UpdateButtonStates()
        {
            var state = _testMaster.CurrentState;

            btnNewTest.Enabled = state == TestState.Idle || state == TestState.Complete;
            btnStartHeat.Enabled = state == TestState.Idle;
            btnStopHeat.Enabled = state == TestState.Preparing || state == TestState.Ready || state == TestState.Complete;
            btnStartRecord.Enabled = state == TestState.Ready;
            btnStopRecord.Enabled = state == TestState.Recording;
        }

        private void BtnNewTest_Click(object? sender, EventArgs e)
        {
            var form = new NewTestForm(_dbHelper, _currentUser);
            if (form.ShowDialog() == DialogResult.OK)
            {
                _testMaster.CurrentProductId = form.ProductId;
                _testMaster.CurrentTestId = form.TestId;
                _testMaster.CurrentOperator = form.OperatorName;
                _testMaster.PreWeight = form.PreWeight;
                _testMaster.AmbTemp = form.AmbTemp;
                _testMaster.AmbHumi = form.AmbHumi;

                lblProductId.Text = $"样品编号：{form.ProductId}";
                AddLog($"新建试验：{form.ProductId} / {form.TestId}", Color.LightGreen);
            }
        }

        private void BtnStartHeat_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_testMaster.CurrentProductId))
            {
                MessageBox.Show("请先新建试验", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _testMaster.StartHeating();
        }

        private void BtnStopHeat_Click(object? sender, EventArgs e)
        {
            _testMaster.StopHeating();
        }

        private void BtnStartRecord_Click(object? sender, EventArgs e)
        {
            _testMaster.StartRecording();
        }

        private void BtnStopRecord_Click(object? sender, EventArgs e)
        {
            _testMaster.StopRecording();

            var form = new TestRecordForm(_testMaster, _dbHelper);
            form.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _testMaster.Stop();
            base.OnFormClosing(e);
        }
    }
}