using System.Data.SqlClient;

namespace InsuranceApp
{
    public static class DatabaseHelper
    {
        public static SqlConnection GetConnection()
        {
            // Для локального SQL Server. Для учебного сервера укажите свои параметры подключения.
            string connectionString = "Server=localhost;Database=user90_db;Trusted_Connection=True;TrustServerCertificate=True;";
            return new SqlConnection(connectionString);
        }
    }
}
