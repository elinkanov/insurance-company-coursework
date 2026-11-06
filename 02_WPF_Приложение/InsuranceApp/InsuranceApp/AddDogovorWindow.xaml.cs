using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace InsuranceApp
{
    public partial class AddDogovorWindow : Window
    {
        public AddDogovorWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
        }

        private void LoadComboBoxes()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();

                SqlDataAdapter daKlient = new SqlDataAdapter("SELECT id_klienta, familiya + ' ' + imya AS FIO FROM KURS_Klient", conn);
                DataTable dtKlient = new DataTable();
                daKlient.Fill(dtKlient);
                CbKlient.ItemsSource = dtKlient.DefaultView;
                CbKlient.DisplayMemberPath = "FIO";
                CbKlient.SelectedValuePath = "id_klienta";

                SqlDataAdapter daVid = new SqlDataAdapter("SELECT id_vida, nazvanie FROM KURS_Vid_Strah", conn);
                DataTable dtVid = new DataTable();
                daVid.Fill(dtVid);
                CbVidStrah.ItemsSource = dtVid.DefaultView;
                CbVidStrah.DisplayMemberPath = "nazvanie";
                CbVidStrah.SelectedValuePath = "id_vida";

                SqlDataAdapter daAgent = new SqlDataAdapter("SELECT id_agenta, familiya + ' ' + imya AS FIO FROM KURS_Agent", conn);
                DataTable dtAgent = new DataTable();
                daAgent.Fill(dtAgent);
                CbAgent.ItemsSource = dtAgent.DefaultView;
                CbAgent.DisplayMemberPath = "FIO";
                CbAgent.SelectedValuePath = "id_agenta";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (CbKlient.SelectedValue == null || CbVidStrah.SelectedValue == null || CbAgent.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента, вид страхования и агента!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"INSERT INTO KURS_Dogovor (id_klienta, id_vida, id_agenta, data_zaklucheniya, srok, summa) 
                                     VALUES (@id_kl, @id_v, @id_a, @data, @srok, @summa)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id_kl", CbKlient.SelectedValue);
                        cmd.Parameters.AddWithValue("@id_v", CbVidStrah.SelectedValue);
                        cmd.Parameters.AddWithValue("@id_a", CbAgent.SelectedValue);

                        if (!DateTime.TryParse(TbData.Text, out DateTime date))
                        {
                            MessageBox.Show("Неверный формат даты. Используйте формат ГГГГ-ММ-ДД.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!int.TryParse(TbSrok.Text, out int srok) || srok <= 0)
                        {
                            MessageBox.Show("Срок действия должен быть целым положительным числом (месяцев).",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (!decimal.TryParse(TbSumma.Text, out decimal summa) || summa <= 0)
                        {
                            MessageBox.Show("Страховая сумма должна быть положительным числом.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        cmd.Parameters.AddWithValue("@data", date);
                        cmd.Parameters.AddWithValue("@srok", srok);
                        cmd.Parameters.AddWithValue("@summa", summa);

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Договор успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
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