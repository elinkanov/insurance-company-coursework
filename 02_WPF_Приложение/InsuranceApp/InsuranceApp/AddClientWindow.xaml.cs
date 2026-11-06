using System;
using System.Data.SqlClient;
using System.Windows;

namespace InsuranceApp
{
    public partial class AddClientWindow : Window
    {
        public AddClientWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TbFamiliya.Text) || string.IsNullOrWhiteSpace(TbImya.Text))
            {
                MessageBox.Show("Заполните хотя бы Фамилию и Имя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime? dataRozhdeniya = null;
            if (!string.IsNullOrWhiteSpace(TbData.Text))
            {
                if (!DateTime.TryParse(TbData.Text, out DateTime date))
                {
                    MessageBox.Show("Неверный формат даты рождения. Используйте формат ГГГГ-ММ-ДД.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                dataRozhdeniya = date;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"INSERT INTO KURS_Klient (familiya, imya, otchestvo, data_rozhdeniya, pasport, adres, telefon) 
                                     VALUES (@fam, @imya, @otch, @data, @pasport, @adres, @tel)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@fam", TbFamiliya.Text.Trim());
                        cmd.Parameters.AddWithValue("@imya", TbImya.Text.Trim());
                        cmd.Parameters.AddWithValue("@otch", TbOtchestvo.Text.Trim());
                        cmd.Parameters.AddWithValue("@data", (object)dataRozhdeniya ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@pasport",
                            string.IsNullOrWhiteSpace(TbPasport.Text) ? (object)DBNull.Value : TbPasport.Text.Trim());
                        cmd.Parameters.AddWithValue("@adres",
                            string.IsNullOrWhiteSpace(TbAdres.Text) ? (object)DBNull.Value : TbAdres.Text.Trim());
                        cmd.Parameters.AddWithValue("@tel", TbTelefon.Text.Trim());

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Клиент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }
                catch (SqlException ex)
                {
                    MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }
    }
}