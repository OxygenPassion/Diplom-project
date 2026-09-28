using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Text; // пароль не trim

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Введите логин и пароль.", "Авторизация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            const string sql = @"
SELECT COUNT(1)
FROM dbo.Пользователи
WHERE Логин = @login AND Пароль = @pass;";

            try
            {
                object result = DataBase.ExecuteScalar(sql,
                    new SqlParameter("@login", login),
                    new SqlParameter("@pass", pass));

                bool ok = Convert.ToInt32(result) > 0;

                if (!ok)
                {
                    MessageBox.Show("Неверный логин или пароль.", "Авторизация",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                this.Hide();
                using (var main = new MainForm())
                {
                    main.ShowDialog();
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к БД:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOpenRegistration_Click(object sender, EventArgs e)
        {
            using (var form = new RegistrationForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                txtLogin.Text = form.RegisteredLogin;
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}
