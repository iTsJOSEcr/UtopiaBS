using System.ComponentModel.DataAnnotations.Schema;


namespace UtopiaBS.Entities
{
    [Table("UsuarioActividad")] // ← Nombre real en SQL
    public class UsuarioActividad
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public System.DateTime FechaInicio { get; set; }
        public System.DateTime? FechaFin { get; set; }
    }
}
