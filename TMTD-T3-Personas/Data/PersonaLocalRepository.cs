using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using TMTD_T3_Personas.Models;

namespace TMTD_T3_Personas.Data
{
    public class PersonaLocalRepository
    {
        private readonly SQLiteAsyncConnection _connection;

        public PersonaLocalRepository(DatabaseService dbService)
        {
            _connection = dbService.GetConnection();
        }

        public async Task<List<Persona>> GetPersonasAsync()
        {
            return await _connection.Table<Persona>().OrderBy(p => p.IdPersona).ToListAsync();
        }

        public async Task<int> InsertarPersonaAsync(Persona persona)
        {
            return await _connection.InsertAsync(persona);
        }

        public async Task<int> ActualizarPersonaAsync(Persona persona)
        {
            return await _connection.UpdateAsync(persona);
        }

        public async Task<int> EliminarPersonaAsync(int idPersona)
        {
            return await _connection.DeleteAsync<Persona>(idPersona);
        }

        public async Task<Persona> ObtenerPersonaPorIdAsync(int id)
        {
            return await _connection.FindAsync<Persona>(id);
        }

        public async Task<List<Persona>> ObtenerPendientesSyncAsync()
        {
            return await _connection.Table<Persona>().Where(p => p.PendienteSync == true).ToListAsync();
        }
    }
}
