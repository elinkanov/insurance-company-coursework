using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace InsuranceApp
{
    public partial class AddCaseWindow : Window
    {
        public AddCaseWindow()
        {
            InitializeComponent();
            LoadDogovors();
        }

        private void LoadDogovors()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT d.id_dogovora, 
                                        d.id_dogovora AS Num, 
                                        k.familiya + ' ' + k.imya AS FIO,
                                        d.summa 
                                 FROM KURS_Dogovor d
                                 JOIN KURS_Klient k ON d.id_klienta = k.id_klienta";
                SqlDataAdapter da = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);

                CbDogovor.ItemsSource = dt.DefaultView;
                CbDogovor.DisplayMemberPath = "Num"; 
                CbDogovor.SelectedValuePath = "id_dogovora";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbDogovor.SelectedValue == null)
            {
                MessageBox.Show("Выберите договор!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TbOpisanie.Text))
            {
                MessageBox.Show("Введите описание страхового случая!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sumQuery = "SELECT summa FROM KURS_Dogovor WHERE id_dogovora = @id_dog";
                    decimal dogovorSumma;
                    using (var sumCmd = new SqlCommand(sumQuery, conn))
                    {
                        sumCmd.Parameters.AddWithValue("@id_dog", CbDogovor.SelectedValue);
                        object value = sumCmd.ExecuteScalar();
                        if (value == null || value == DBNull.Value)
                        {
                            MessageBox.Show("Договор не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        dogovorSumma = Convert.ToDecimal(value);
                    }

                    if (!DateTime.TryParse(TbData.Text, out DateTime date))
                    {
                        MessageBox.Show("Неверный формат даты. Используйте формат ГГГГ-ММ-ДД.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (!decimal.TryParse(TbSumma.Text, out decimal sum) || sum <= 0)
                    {
                        MessageBox.Show("Сумма выплаты должна быть положительным числом.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (sum > dogovorSumma)
                    {
                        MessageBox.Show("Сумма выплаты не может превышать страховую сумму по договору!",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string query = @"INSERT INTO KURS_Strah_Sluchai (id_dogovora, id_admina, data, opisanie, summa_viplaty) 
                                     VALUES (@id_dog, 1, @data, @op, @sum)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_dog", CbDogovor.SelectedValue);
                        cmd.Parameters.AddWithValue("@data", date);
                        cmd.Parameters.AddWithValue("@op", TbOpisanie.Text.Trim());
                        cmd.Parameters.AddWithValue("@sum", sum);

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Страховой случай зарегистрирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) this.DragMove();
        }
    }
}