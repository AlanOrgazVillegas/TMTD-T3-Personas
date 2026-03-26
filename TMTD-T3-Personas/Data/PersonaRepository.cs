using Microsoft.Data.SqlClient;

namespace TMTD_T3_Personas.Data
{
    public class PersonaRepository
    {
        private readonly string _connectionString =
            "workstation id=veterinaria.mssql.somee.com;" +
            "packet size=4096;" +
            "user id=AlanOrgazV_SQLLogin_1;" +
            "pwd=fnxs9wf9hj;" +
            "data source=veterinaria.mssql.somee.com;" +
            "persist security info=False;" +
            "initial catalog=veterinaria;" +
            "TrustServerCertificate=True";

        public async Task<SqlConnection> AbrirConexionAsync()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
    }
}