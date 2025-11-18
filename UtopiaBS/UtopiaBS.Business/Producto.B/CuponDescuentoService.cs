using System;
using System.Linq;
using UtopiaBS.Entities;
using UtopiaBS.Data;

namespace UtopiaBS.Business
{
    public class CuponDescuentoService
    {
        public string AgregarCupon(CuponDescuento nuevo)
        {
            try
            {
                using (var db = new Context())
                {
                    if (db.CuponDescuento.Any(c => c.Codigo == nuevo.Codigo))
                        return "Ya existe un cupón con ese código.";

                    nuevo.Activo = true;
                    nuevo.UsoActual = 0;
                    nuevo.FechaInicio = nuevo.FechaInicio == default ? DateTime.Now : nuevo.FechaInicio;

                    db.CuponDescuento.Add(nuevo);
                    db.SaveChanges();
                }
                return "Cupón agregado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al agregar el cupón: {ex.Message}";
            }
        }

        public string EditarCupon(CuponDescuento model)
        {
            try
            {
                using (var db = new Context())
                {
                    var cuponDB = db.CuponDescuento.FirstOrDefault(c => c.CuponId == model.CuponId);

                    if (cuponDB == null)
                        return "Cupón no encontrado.";

                    cuponDB.Codigo = model.Codigo;
                    cuponDB.Tipo = model.Tipo;
                    cuponDB.Valor = model.Valor;
                    cuponDB.Activo = model.Activo;
                    cuponDB.FechaInicio = model.FechaInicio;
                    cuponDB.FechaFin = model.FechaFin;

                    db.SaveChanges();
                    return "Cupón editado exitosamente.";
                }
            }
            catch (Exception ex)
            {
                return "Error al editar el cupón: " +
                       (ex.InnerException?.InnerException?.Message ?? ex.Message);
            }
        }

        public string EliminarCupon(int id)
        {
            try
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento.Find(id);
                    if (cupon == null)
                        return "Cupón no encontrado.";

                    db.CuponDescuento.Remove(cupon);
                    db.SaveChanges();
                }
                return "Cupón eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al eliminar el cupón: {ex.Message}";
            }
        }

        public string CambiarEstado(int id)
        {
            try
            {
                using (var db = new Context())
                {
                    var cupon = db.CuponDescuento.Find(id);
                    if (cupon == null)
                        return "Cupón no encontrado.";

                    cupon.Activo = !cupon.Activo;
                    db.SaveChanges();
                }
                return "Estado del cupón actualizado.";
            }
            catch (Exception ex)
            {
                return $"Error al cambiar el estado del cupón: {ex.Message}";
            }
        }
    }
}
