using ISO11820.Config;
using ISO11820.DataAccess;
using ISO11820.Forms;
using System;
using System.IO;
using System.Windows.Forms;

namespace ISO11820
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            string dbPath = AppConfig.DatabasePath;
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            var dbHelper = new DbHelper(dbPath);
            dbHelper.InitializeDatabase();

            using var loginForm = new LoginForm(dbHelper);
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                var mainForm = new MainForm(loginForm.LoggedInUser!, dbHelper);
                Application.Run(mainForm);
            }
        }
    }
}