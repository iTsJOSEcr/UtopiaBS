using System;
using System.Collections.Generic;

namespace UtopiaBS.Models
{
    public class MovimientoPuntosViewModel
    {
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
        public int Puntos { get; set; }
    }

    public class MisPuntosViewModel
    {
        public int PuntosTotales { get; set; }

        public string TipoMembresia { get; set; }

        public DateTime? FechaInicioMembresia { get; set; }
        public DateTime? FechaFinMembresia { get; set; }
        public bool PuntosPorVencer
{
    get
    {
        if (!FechaFinMembresia.HasValue) return false;
        return (FechaFinMembresia.Value - DateTime.Today).TotalDays <= 7;
    }
}

public int DiasParaVencerse
{
    get
    {
        if (!FechaFinMembresia.HasValue) return 0;
        return (FechaFinMembresia.Value - DateTime.Today).Days;
    }
}

        public List<MovimientoPuntosViewModel> Movimientos { get; set; }

        public MisPuntosViewModel()
        {
            Movimientos = new List<MovimientoPuntosViewModel>();
        }
    }
}
