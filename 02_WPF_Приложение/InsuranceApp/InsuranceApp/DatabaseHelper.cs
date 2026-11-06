using System.Data.SqlClient;

namespace InsuranceApp
{
    public static class DatabaseHelper
    {
        public static SqlConnection GetConnection()
        {
            string connectionString = "Server=localhost;Database=user90_db;Trusted_Connection=True;TrustServerCertificate=True;";
            return new SqlConnection(connectionString);
        }
    }
}
