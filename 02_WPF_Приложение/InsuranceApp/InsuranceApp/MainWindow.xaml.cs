using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace InsuranceApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadClients();
            LoadDogovors();
            LoadCases();
        }

        private void MenuClients_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Клиенты";
            PageSubtitle.Text = "Управление базой клиентов";

            DgClients.Visibility = Visibility.Visible;
            DgDogovors.Visibility = Visibility.Collapsed;
            DgCases.Visibility = Visibility.Collapsed;

            SearchPanel.Visibility = Visibility.Visible;
            BtnAddClient.Visibility = Visibility.Visible;
            BtnAddDogovor.Visibility = Visibility.Collapsed;
            BtnAddCase.Visibility = Visibility.Collapsed;

            LoadClients();
        }

        private void MenuDogovors_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Договоры";
            PageSubtitle.Text = "Список оформленных договоров страхования";

            DgClients.Visibility = Visibility.Collapsed;
            DgDogovors.Visibility = Visibility.Visible;
            DgCases.Visibility = Visibility.Collapsed;

            SearchPanel.Visibility = Visibility.Collapsed;
            BtnAddClient.Visibility = Visibility.Collapsed;
            BtnAddDogovor.Visibility = Visibility.Visible;
            BtnAddCase.Visibility = Visibility.Collapsed;

            LoadDogovors();
        }

        private void MenuCases_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "Страховые случаи";
            PageSubtitle.Text = "Реестр зарегистрированных происшествий";

            DgClients.Visibility = Visibility.Collapsed;
            DgDogovors.Visibility = Visibility.Collapsed;
            DgCases.Visibility = Visibility.Visible;

            SearchPanel.Visibility = Visibility.Collapsed;
            BtnAddClient.Visibility = Visibility.Collapsed;
            BtnAddDogovor.Visibility = Visibility.Collapsed;
            BtnAddCase.Visibility = Visibility.Visible;

            LoadCases();
        }

        private void LoadClients(string search = "")
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = "SELECT id_klienta AS [ID], familiya AS [Фамилия], imya AS [Имя], otchestvo AS [Отчество], telefon AS [Телефон] FROM KURS_Klient";
                    if (!string.IsNullOrWhiteSpace(search))
                        query += @" WHERE familiya LIKE @search OR imya LIKE @search OR otchestvo LIKE @search
                                    OR EXISTS (SELECT 1 FROM KURS_Dogovor d
                                               WHERE d.id_klienta = KURS_Klient.id_klienta
                                                 AND CAST(d.id_dogovora AS nvarchar(20)) LIKE @search)";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    if (!string.IsNullOrWhiteSpace(search))
                        adapter.SelectCommand.Parameters.AddWithValue("@search", "%" + search + "%");

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    DgClients.ItemsSource = dt.DefaultView;
                }
                catch (SqlException ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
            }
        }

        private void LoadDogovors()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT d.id_dogovora AS DogovorNum, k.familiya + ' ' + k.imya AS ClientName,
                               v.nazvanie AS InsuranceType, a.familiya AS AgentName,
                               d.data_zaklucheniya AS Date, d.summa AS Sum
                        FROM KURS_Dogovor d
                        JOIN KURS_Klient k ON d.id_klienta = k.id_klienta
                        JOIN KURS_Vid_Strah v ON d.id_vida = v.id_vida
                        JOIN KURS_Agent a ON d.id_agenta = a.id_agenta";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    DgDogovors.ItemsSource = dt.DefaultView;
                }
                catch (SqlException ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
            }
        }

        private void LoadCases()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                try
                {
                    conn.Open();
                    string query = @"
                        SELECT s.id_sluchaya AS CaseId, d.id_dogovora AS DogovorNum,
                               k.familiya + ' ' + k.imya AS ClientName, s.data AS Date,
                               s.opisanie AS Description, s.summa_viplaty AS Sum
                        FROM KURS_Strah_Sluchai s
                        JOIN KURS_Dogovor d ON s.id_dogovora = d.id_dogovora
                        JOIN KURS_Klient k ON d.id_klienta = k.id_klienta";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    DgCases.ItemsSource = dt.DefaultView;
                }
                catch (SqlException ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
            }
        }

        private void TbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadClients(TbSearch.Text);
        }

        private void BtnAddClient_Click(object sender, RoutedEventArgs e)
        {
            AddClientWindow addWindow = new AddClientWindow();
            addWindow.ShowDialog();
            LoadClients();
        }

        private void BtnAddDogovor_Click(object sender, RoutedEventArgs e)
        {
            AddDogovorWindow addDog = new AddDogovorWindow();
            addDog.ShowDialog();
            LoadDogovors();
        }

        private void BtnAddCase_Click(object sender, RoutedEventArgs e)
        {
            AddCaseWindow addCase = new AddCaseWindow();
            addCase.ShowDialog();
            LoadCases();
        }
    }
}