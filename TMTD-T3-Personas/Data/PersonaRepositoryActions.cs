using System;
using Microsoft.Data.SqlClient;
using TMTD_T3_Personas.Models;

namespace TMTD_T3_Personas.Data
{
    public class PersonaRepositoryActions
    {
        private readonly PersonaRepository _repo;

        public PersonaRepositoryActions()
        {
            _repo = new PersonaRepository();
        }

        // ---------------------------------------------------------------
        // LEER TODOS
        // ---------------------------------------------------------------
        public async Task<List<Persona>> GetPersonasAsync()
        {
            var lista = new List<Persona>();

            using var connection = await _repo.AbrirConexionAsync();

            string query = @"SELECT IdPersona, Nombre, Apellido, 
                                    Email, Telefono, FechaNacimiento, Activo 
                             FROM Personas 
                             ORDER BY IdPersona";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                lista.Add(new Persona
                {
                    IdPersona = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Email = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Telefono = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    FechaNacimiento = reader.IsDBNull(5) ? new DateTime(2000, 1, 1) : reader.GetDateTime(5),
                    Activo = reader.IsDBNull(6) ? false : reader.GetBoolean(6)
                });
            }

            return lista;
        }

        // ---------------------------------------------------------------
        // INSERTAR (sin ID, se genera automáticamente)
        // ---------------------------------------------------------------
        public async Task InsertarPersonaAsync(Persona persona)
        {
            using var connection = await _repo.AbrirConexionAsync();

            string query = @"INSERT INTO Personas 
                                (Nombre, Apellido, Email, Telefono, FechaNacimiento, Activo)
                             VALUES 
                                (@Nombre, @Apellido, @Email, @Telefono, @FechaNacimiento, @Activo)";

            using var command = new SqlCommand(query, connection);
            AsignarParametrosSinId(command, persona);
            await command.ExecuteNonQueryAsync();
        }

        // ---------------------------------------------------------------
        // INSERTAR Y DEVOLVER ID (sin ID, se genera automáticamente)
        // ---------------------------------------------------------------
        public async Task<int> InsertarPersonaYDevolverIdAsync(Persona persona)
        {
            using var connection = await _repo.AbrirConexionAsync();

            string query = @"
        INSERT INTO Personas (Nombre, Apellido, Email, Telefono, FechaNacimiento, Activo)
        VALUES (@Nombre, @Apellido, @Email, @Telefono, @FechaNacimiento, @Activo);
        SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre",
                string.IsNullOrEmpty(persona.Nombre) ? DBNull.Value : (object)persona.Nombre);
            command.Parameters.AddWithValue("@Apellido",
                string.IsNullOrEmpty(persona.Apellido) ? DBNull.Value : (object)persona.Apellido);
            command.Parameters.AddWithValue("@Email",
                string.IsNullOrEmpty(persona.Email) ? DBNull.Value : (object)persona.Email);
            command.Parameters.AddWithValue("@Telefono",
                string.IsNullOrEmpty(persona.Telefono) ? DBNull.Value : (object)persona.Telefono);
            command.Parameters.AddWithValue("@Activo", persona.Activo);
            command.Parameters.AddWithValue("@FechaNacimiento",
                persona.FechaNacimiento.Year < 1753
                    ? new DateTime(2000, 1, 1)
                    : persona.FechaNacimiento);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        // ---------------------------------------------------------------
        // ACTUALIZAR
        // ---------------------------------------------------------------
        public async Task ActualizarPersonaAsync(Persona persona)
        {
            using var connection = await _repo.AbrirConexionAsync();

            string query = @"UPDATE Personas SET
                                Nombre          = @Nombre,
                                Apellido        = @Apellido,
                                Email           = @Email,
                                Telefono        = @Telefono,
                                FechaNacimiento = @FechaNacimiento,
                                Activo          = @Activo
                             WHERE IdPersona = @IdPersona";

            using var command = new SqlCommand(query, connection);
            AsignarParametros(command, persona);
            await command.ExecuteNonQueryAsync();
        }

        // ---------------------------------------------------------------
        // ELIMINAR
        // ---------------------------------------------------------------
        public async Task EliminarPersonaAsync(int idPersona)
        {
            using var connection = await _repo.AbrirConexionAsync();

            string query = "DELETE FROM Personas WHERE IdPersona = @IdPersona";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@IdPersona", idPersona);
            await command.ExecuteNonQueryAsync();
        }

        // ---------------------------------------------------------------
        // INSERTAR CON ID ESPECÍFICO (para migraciones o sync con ID fijo)
        // ---------------------------------------------------------------
        public async Task InsertarPersonaConIdAsync(Persona persona)
        {
            using var connection = await _repo.AbrirConexionAsync();
            string query = @"
                SET IDENTITY_INSERT Personas ON;
                INSERT INTO Personas (IdPersona, Nombre, Apellido, Email, Telefono, FechaNacimiento, Activo)
                VALUES (@IdPersona, @Nombre, @Apellido, @Email, @Telefono, @FechaNacimiento, @Activo);
                SET IDENTITY_INSERT Personas OFF;";

            using var command = new SqlCommand(query, connection);
            AsignarParametros(command, persona);
            await command.ExecuteNonQueryAsync();
        }

        // ---------------------------------------------------------------
        // AUXILIAR: parámetros comunes (con IdPersona)
        // ---------------------------------------------------------------
        private void AsignarParametros(SqlCommand cmd, Persona persona)
        {
            cmd.Parameters.AddWithValue("@IdPersona", persona.IdPersona);
            cmd.Parameters.AddWithValue("@Nombre",
                string.IsNullOrEmpty(persona.Nombre) ? DBNull.Value : (object)persona.Nombre);
            cmd.Parameters.AddWithValue("@Apellido",
                string.IsNullOrEmpty(persona.Apellido) ? DBNull.Value : (object)persona.Apellido);
            cmd.Parameters.AddWithValue("@Email",
                string.IsNullOrEmpty(persona.Email) ? DBNull.Value : (object)persona.Email);
            cmd.Parameters.AddWithValue("@Telefono",
                string.IsNullOrEmpty(persona.Telefono) ? DBNull.Value : (object)persona.Telefono);
            cmd.Parameters.AddWithValue("@Activo", persona.Activo);
            cmd.Parameters.AddWithValue("@FechaNacimiento",
                persona.FechaNacimiento.Year < 1753
                    ? new DateTime(2000, 1, 1)
                    : persona.FechaNacimiento);
        }

        // ---------------------------------------------------------------
        // AUXILIAR: parámetros sin IdPersona (para insert sin ID)
        // ---------------------------------------------------------------
        private void AsignarParametrosSinId(SqlCommand cmd, Persona persona)
        {
            cmd.Parameters.AddWithValue("@Nombre",
                string.IsNullOrEmpty(persona.Nombre) ? DBNull.Value : (object)persona.Nombre);
            cmd.Parameters.AddWithValue("@Apellido",
                string.IsNullOrEmpty(persona.Apellido) ? DBNull.Value : (object)persona.Apellido);
            cmd.Parameters.AddWithValue("@Email",
                string.IsNullOrEmpty(persona.Email) ? DBNull.Value : (object)persona.Email);
            cmd.Parameters.AddWithValue("@Telefono",
                string.IsNullOrEmpty(persona.Telefono) ? DBNull.Value : (object)persona.Telefono);
            cmd.Parameters.AddWithValue("@Activo", persona.Activo);
            cmd.Parameters.AddWithValue("@FechaNacimiento",
                persona.FechaNacimiento.Year < 1753
                    ? new DateTime(2000, 1, 1)
                    : persona.FechaNacimiento);
        }
    }
}