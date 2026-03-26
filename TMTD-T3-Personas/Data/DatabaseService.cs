using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using TMTD_T3_Personas.Models;

namespace TMTD_T3_Personas.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;

        public DatabaseService()
        {
            // Ruta de la base de datos en el sistema de archivos
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "personas.db3");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitAsync()
        {
            await _database.CreateTableAsync<Persona>();
        }

        public SQLiteAsyncConnection GetConnection() => _database;
    }
}
