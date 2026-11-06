using System;
using System.Data.SqlClient;
using System.Windows;

namespace InsuranceApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string password = PbPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(1) FROM KURS_Admin WHERE login=@login AND parol=@password";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        cmd.Parameters.AddWithValue("@password", password);

                        int count = (int)cmd.ExecuteScalar();

                        if (count > 0)
                        {
                            MainWindow mainWindow = new MainWindow();
                            mainWindow.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль!", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                            PbPassword.Clear();
                            PbPassword.Focus();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}