using ISO11820.Config;
using ISO11820.Core;
using ISO11820.DataAccess;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ISO11820.Forms
{
    public class TestRecordForm : Form
    {
        private TestMaster _testMaster;
        private DbHelper _dbHelper;

        private CheckBox chkFlame;
        private TextBox txtFlameTime, txtFlameDuration;
        private TextBox txtPostWeight, txtMemo;

        public TestRecordForm(TestMaster testMaster, DbHelper dbHelper)
        {
            _testMaster = testMaster;
            _dbHelper = dbHelper;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "试验记录";
            this.Size = new Size(450, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(40, 40, 40);

            int y = 20, x1 = 20, x2 = 150;

            chkFlame = new CheckBox
            {
                Text = "是否出现持续火焰",
                ForeColor = Color.White,
                Location = new Point(x1, y),
                AutoSize = true
            };
            chkFlame.CheckedChanged += (s, e) =>
            {
                txtFlameTime.Enabled = chkFlame.Checked;
                txtFlameDuration.Enabled = chkFlame.Checked;
            };
            this.Controls.Add(chkFlame);
            y += 30;

            AddLabel("火焰发生时刻(秒)：", x1, y, Color.White);
            txtFlameTime = AddTextBox(x2, y);
            txtFlameTime.Enabled = false;
            y += 35;

            AddLabel("火焰持续时间(秒)：", x1, y, Color.White);
            txtFlameDuration = AddTextBox(x2, y);
            txtFlameDuration.Enabled = false;
            y += 35;

            AddLabel("试验后质量(g)：", x1, y, Color.White);
            txtPostWeight = AddTextBox(x2, y);
            y += 35;

            AddLabel("备注：", x1, y, Color.White);
            txtMemo = new TextBox
            {
                Location = new Point(x2, y),
                Size = new Size(250, 60),
                Multiline = true
            };
            this.Controls.Add(txtMemo);
            y += 70;

            var btnSave = new Button
            {
                Text = "保存",
                Location = new Point(120, y),
                Size = new Size(80, 30),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(220, y),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void AddLabel(string text, int x, int y, Color color)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                ForeColor = color,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private TextBox AddTextBox(int x, int y)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(150, 25)
            };
            this.Controls.Add(txt);
            return txt;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!double.TryParse(txtPostWeight.Text, out double postWeight) || postWeight < 0)
            {
                MessageBox.Show("请输入有效的试验后质量", "提示");
                return;
            }

            int flameTime = chkFlame.Checked && int.TryParse(txtFlameTime.Text, out int ft) ? ft : 0;
            int flameDuration = chkFlame.Checked && int.TryParse(txtFlameDuration.Text, out int fd) ? fd : 0;

            var results = _testMaster.CalculateResults(postWeight);

            _dbHelper.UpdateTestResult(
                _testMaster.CurrentProductId!,
                _testMaster.CurrentTestId!,
                postWeight,
                results.lostWeight,
                results.lostWeightPer,
                results.deltaTf,
                _testMaster.ElapsedSeconds,
                chkFlame.Checked ? "FLAME" : "",
                flameTime,
                flameDuration,
                results.maxTf1, results.maxTf2, results.maxTs, results.maxTc,
                results.finalTf1, results.finalTf2, results.finalTs, results.finalTc,
                results.deltaTf1, results.deltaTf2, results.deltaTs, results.deltaTc,
                txtMemo.Text
            );

            GenerateExcelReport();

            MessageBox.Show("试验记录已保存！", "成功");
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void GenerateExcelReport()
        {
            try
            {
                string srcDir = Path.Combine(AppConfig.TestDataDirectory, _testMaster.CurrentProductId!, _testMaster.CurrentTestId!);
                string reportDir = AppConfig.ReportDirectory;
                Directory.CreateDirectory(reportDir);

                string srcCsv = Path.Combine(srcDir, "sensor_data.csv");
                string destCsv = Path.Combine(reportDir, $"{_testMaster.CurrentTestId}_温度数据.csv");

                if (File.Exists(srcCsv))
                {
                    File.Copy(srcCsv, destCsv, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"报告生成失败：{ex.Message}", "警告");
            }
        }
    }
}