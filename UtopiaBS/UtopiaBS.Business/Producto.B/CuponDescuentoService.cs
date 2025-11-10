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

        public string EditarCupon(CuponDescuento cupon)
        {
            try
            {
                using (var db = new Context())
                {
                    var existente = db.CuponDescuento.Find(cupon.CuponId);
                    if (existente == null)
                        return "Cupón no encontrado.";

                    if (db.CuponDescuento.Any(c => c.Codigo == cupon.Codigo && c.CuponId != cupon.CuponId))
                        return "Ya existe otro cupón con ese código.";

                    existente.Codigo = cupon.Codigo;
                    existente.Tipo = cupon.Tipo;
                    existente.Valor = cupon.Valor;
                    existente.FechaInicio = cupon.FechaInicio;
                    existente.FechaFin = cupon.FechaFin;
                    existente.UsoMaximo = cupon.UsoMaximo;
                    existente.UsoActual = cupon.UsoActual;

                    db.SaveChanges();
                }
                return "Cupón editado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al editar el cupón: {ex.Message}";
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
