using System;
using SQLite;

namespace TMTD_T3_Personas.Models
{
    [Table("Personas")]
    public class Persona
    {
        [PrimaryKey]
        public int IdPersona { get; set; }

        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public bool Activo { get; set; }

        public bool EsLocal { get; set; }
        public bool PendienteSync { get; set; }
        public DateTime FechaModificacion { get; set; }
        public bool Eliminado { get; set; }   // <--- IMPORTANTE
    }
}