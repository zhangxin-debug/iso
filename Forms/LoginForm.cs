using ISO11820.DataAccess;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ISO11820.Forms
{
    public class LoginForm : Form
    {
        private RadioButton rbAdmin;
        private RadioButton rbExperimenter;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblStatus;
        private DbHelper _dbHelper;

        public string? LoggedInUser { get; private set; }
        public string? UserType { get; private set; }

        public LoginForm(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "ISO 11820 建筑材料不燃性试验系统 - 登录";
            this.Size = new Size(450, 350);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(30, 30, 30);

            var lblTitle = new Label
            {
                Text = "ISO 11820 不燃性试验系统",
                Font = new Font("微软雅黑", 16, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(80, 30)
            };

            var lblRole = new Label
            {
                Text = "选择角色：",
                ForeColor = Color.White,
                Location = new Point(80, 90),
                AutoSize = true
            };

            rbAdmin = new RadioButton
            {
                Text = "管理员",
                ForeColor = Color.White,
                Location = new Point(160, 88),
                Checked = true
            };

            rbExperimenter = new RadioButton
            {
                Text = "试验员",
                ForeColor = Color.White,
                Location = new Point(260, 88)
            };

            var lblPwd = new Label
            {
                Text = "密码：",
                ForeColor = Color.White,
                Location = new Point(80, 140),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(160, 138),
                Size = new Size(180, 25),
                PasswordChar = '*'
            };

            btnLogin = new Button
            {
                Text = "登录",
                Location = new Point(160, 200),
                Size = new Size(100, 35),
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.Click += BtnLogin_Click;

            lblStatus = new Label
            {
                Location = new Point(80, 260),
                Size = new Size(300, 25),
                ForeColor = Color.Red
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblRole);
            this.Controls.Add(rbAdmin);
            this.Controls.Add(rbExperimenter);
            this.Controls.Add(lblPwd);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(lblStatus);

            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            string username = rbAdmin.Checked ? "admin" : "experimenter";
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "请输入密码";
                return;
            }

            bool success = _dbHelper.Login(username, password, out string userid, out string usertype);
            if (success)
            {
                LoggedInUser = username;
                UserType = usertype;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblStatus.Text = "密码错误，请重新输入";
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}