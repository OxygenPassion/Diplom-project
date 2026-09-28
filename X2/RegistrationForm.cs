using System;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class RegistrationForm : Form
    {
        private const int MaxLoginLength = 50;
        private const int MaxPasswordLength = 100;

        public string RegisteredLogin { get; private set; }

        public RegistrationForm()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
            txtConfirmPassword.UseSystemPasswordChar = true;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string pass = txtPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(confirmPass))
            {
                MessageBox.Show("Введите логин и пароль.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (login.Length > MaxLoginLength)
            {
                MessageBox.Show("Логин не должен быть длиннее 50 символов.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLogin.Focus();
                return;
            }

            if (pass.Length > MaxPasswordLength)
            {
                MessageBox.Show("Пароль не должен быть длиннее 100 символов.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            if (pass != confirmPass)
            {
                MessageBox.Show("Пароли не совпадают.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtConfirmPassword.Clear();
                txtConfirmPassword.Focus();
                return;
            }

            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.Пользователи WHERE Логин = @login)
BEGIN
    SELECT CAST(0 AS int);
END
ELSE
BEGIN
    INSERT INTO dbo.Пользователи(Логин, Пароль)
    VALUES (@login, @pass);

    SELECT CAST(1 AS int);
END;";

            try
            {
                object result = DataBase.ExecuteScalarInTransaction(sql,
                    new SqlParameter("@login", login),
                    new SqlParameter("@pass", pass));

                if (Convert.ToInt32(result) == 0)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует.", "Регистрация",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtLogin.Focus();
                    return;
                }

                RegisteredLogin = login;
                MessageBox.Show("Пользователь зарегистрирован.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                MessageBox.Show("Пользователь с таким логином уже существует.", "Регистрация",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLogin.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка регистрации:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
