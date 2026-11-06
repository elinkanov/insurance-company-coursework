using System;
using System.Data.SqlClient;
using System.Windows;

namespace InsuranceApp
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbLogin.Text) || string.IsNullOrWhiteSpace(PbPassword.Password) ||
                string.IsNullOrWhiteSpace(TbFamiliya.Text) || string.IsNullOrWhiteSpace(TbImya.Text))
            {
                MessageBox.Show("Заполните все обязательные поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();

                    string checkQuery = "SELECT COUNT(1) FROM KURS_Admin WHERE login=@login";
                    using (var checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@login", TbLogin.Text.Trim());
                        int count = (int)checkCmd.ExecuteScalar();

                        if (count > 0)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string query = @"INSERT INTO KURS_Admin (login, parol, familiya, imya, otchestvo) 
                                     VALUES (@login, @parol, @fam, @imya, @otch)";
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", TbLogin.Text.Trim());
                        cmd.Parameters.AddWithValue("@parol", PbPassword.Password);
                        cmd.Parameters.AddWithValue("@fam", TbFamiliya.Text.Trim());
                        cmd.Parameters.AddWithValue("@imya", TbImya.Text.Trim());
                        cmd.Parameters.AddWithValue("@otch", TbOtchestvo.Text.Trim());

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Регистрация успешна! Теперь вы можете войти.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при регистрации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove();
        }
    }
}