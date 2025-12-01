using System;

namespace UtopiaBS.ViewModels
{
    public class ClienteMembresiaViewModel
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Cedula { get; set; }

        public string TipoMembresia { get; set; }
        public bool Activa { get; set; }

        public int Puntos { get; set; }

        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }
}
