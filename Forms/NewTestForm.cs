using ISO11820.DataAccess;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ISO11820.Forms
{
    public class NewTestForm : Form
    {
        private DbHelper _dbHelper;
        private string _operator;

        public string ProductId { get; private set; } = "";
        public string TestId { get; private set; } = "";
        public string OperatorName { get; private set; } = "";
        public double PreWeight { get; private set; }
        public double AmbTemp { get; private set; }
        public double AmbHumi { get; private set; }

        private TextBox txtProductId, txtProductName, txtSpecific;
        private TextBox txtDiameter, txtHeight, txtPreWeight;
        private TextBox txtAmbTemp, txtAmbHumi;

        public NewTestForm(DbHelper dbHelper, string operatorName)
        {
            _dbHelper = dbHelper;
            _operator = operatorName;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "新建试验";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(40, 40, 40);

            int y = 20, x1 = 20, x2 = 140;

            AddLabel("样品编号：", x1, y, Color.White);
            txtProductId = AddTextBox(x2, y);
            y += 35;

            AddLabel("样品名称：", x1, y, Color.White);
            txtProductName = AddTextBox(x2, y);
            y += 35;

            AddLabel("规格型号：", x1, y, Color.White);
            txtSpecific = AddTextBox(x2, y);
            y += 35;

            AddLabel("直径(mm)：", x1, y, Color.White);
            txtDiameter = AddTextBox(x2, y);
            txtDiameter.Text = "45";
            y += 35;

            AddLabel("高度(mm)：", x1, y, Color.White);
            txtHeight = AddTextBox(x2, y);
            txtHeight.Text = "50";
            y += 35;

            AddLabel("试验前质量(g)：", x1, y, Color.White);
            txtPreWeight = AddTextBox(x2, y);
            y += 35;

            AddLabel("环境温度(°C)：", x1, y, Color.White);
            txtAmbTemp = AddTextBox(x2, y);
            txtAmbTemp.Text = "25";
            y += 35;

            AddLabel("环境湿度(%)：", x1, y, Color.White);
            txtAmbHumi = AddTextBox(x2, y);
            txtAmbHumi.Text = "50";
            y += 45;

            var btnOK = new Button
            {
                Text = "创建",
                Location = new Point(140, y),
                Size = new Size(80, 30),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(240, y),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
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
                Size = new Size(200, 25)
            };
            this.Controls.Add(txt);
            return txt;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductId.Text))
            {
                MessageBox.Show("请输入样品编号", "提示");
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!double.TryParse(txtPreWeight.Text, out double preWeight) || preWeight <= 0)
            {
                MessageBox.Show("请输入有效的试验前质量", "提示");
                this.DialogResult = DialogResult.None;
                return;
            }

            ProductId = txtProductId.Text.Trim();
            TestId = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            OperatorName = _operator;
            PreWeight = preWeight;
            AmbTemp = double.Parse(txtAmbTemp.Text);
            AmbHumi = double.Parse(txtAmbHumi.Text);

            _dbHelper.InsertProduct(ProductId, txtProductName.Text, txtSpecific.Text,
                double.Parse(txtDiameter.Text), double.Parse(txtHeight.Text));

            var apparatus = _dbHelper.GetApparatus(0);
            if (apparatus == null)
            {
                MessageBox.Show("设备信息未找到", "错误");
                this.DialogResult = DialogResult.None;
                return;
            }

            _dbHelper.InsertTest(ProductId, TestId, OperatorName, PreWeight, AmbTemp, AmbHumi,
                apparatus.Value.InnerNumber, apparatus.Value.Name, apparatus.Value.CheckDate);
        }
    }
}