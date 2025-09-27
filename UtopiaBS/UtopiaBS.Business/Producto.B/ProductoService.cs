using System;
using System.Linq;
using UtopiaBS.Entities;
using UtopiaBS.Data;

namespace UtopiaBS.Business
{
    public class ProductoService
    {
        public string AgregarProducto(Producto nuevo)
        {
            try
            {
                using (var db = new Context())
                {
                    nuevo.Fecha = DateTime.Now; 
                    nuevo.Threshold = (nuevo.CantidadStock > 0) ? 0 : 1;
                    nuevo.IdEstado = 1; 

                    db.Productos.Add(nuevo);
                    db.SaveChanges();
                }
                return "Producto agregado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al agregar el producto: {ex.Message}";
            }
        }

        public string EditarProducto(Producto producto)
        {
            try
            {
                using (var db = new Context())
                {
                    var existente = db.Productos.Find(producto.IdProducto);
                    if (existente == null)
                        return "Producto no encontrado.";

                    existente.Nombre = producto.Nombre;
                    existente.Tipo = producto.Tipo;
                    existente.Descripcion = producto.Descripcion;
                    existente.Proveedor = producto.Proveedor;
                    existente.PrecioUnitario = producto.PrecioUnitario;
                    existente.CantidadStock = producto.CantidadStock;
                    existente.Threshold = (producto.CantidadStock > 0) ? 0 : 1;
                    existente.IdEstado = producto.IdEstado;


                    existente.Fecha = DateTime.Now;

                    db.SaveChanges();
                }
                return "Producto editado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al editar el producto: {ex.Message}";
            }
        }

        public string EliminarProducto(int id)
        {
            try
            {
                using (var db = new Context())
                {
                    var producto = db.Productos.Find(id);
                    if (producto == null)
                        return "Producto no encontrado.";

                    db.Productos.Remove(producto);
                    db.SaveChanges();
                }
                return "Producto eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                return $"Error al eliminar el producto: {ex.Message}";
            }
        }
    }
}
