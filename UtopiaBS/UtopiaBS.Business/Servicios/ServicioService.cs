using System;
using System.Collections.Generic;
using System.Linq;
using UtopiaBS.Entities;
using UtopiaBS.Data;

namespace UtopiaBS.Business
{
    public class ServicioService
    {
        public List<Servicio> ObtenerTodos()
        {
            using (var db = new Context())
            {          
                return db.Servicios
                         .OrderBy(s => s.Nombre)
                         .ToList();
            }
        }

        public Servicio ObtenerPorId(int idServicio)
        {
            using (var db = new Context())
            {
                return db.Servicios.Find(idServicio);
            }
        }

        public void Crear(Servicio servicio)
        {
            using (var db = new Context())
            {
                db.Servicios.Add(servicio);
                db.SaveChanges();
            }
        }

        public void Actualizar(Servicio servicio)
        {
            using (var db = new Context())
            {
                var existente = db.Servicios.Find(servicio.IdServicio);
                if (existente == null)
                    throw new Exception("El servicio no existe.");

                existente.Nombre = servicio.Nombre;
                existente.Descripcion = servicio.Descripcion;
                existente.Precio = servicio.Precio;

                db.SaveChanges();
            }
        }

        public void Eliminar(int idServicio)
        {
            using (var db = new Context())
            {
                var servicio = db.Servicios.Find(idServicio);
                if (servicio == null)
                    throw new Exception("El servicio no existe.");

                db.Servicios.Remove(servicio);
                db.SaveChanges();
            }
        }
    }
}
