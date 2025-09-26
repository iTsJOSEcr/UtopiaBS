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
            if (string.IsNullOrWhiteSpace(nuevo.Nombre) ||
                string.IsNullOrWhiteSpace(nuevo.Tipo) ||
                string.IsNullOrWhiteSpace(nuevo.Proveedor) ||
                nuevo.Precio <= 0 ||
                nuevo.Threshold <= 0)
            {
                return "El producto no se puede agregar por campos en blanco o inválidos.";
            }

            try
            {
                using (var db = new Context())
                {
                    bool existe = db.Productos.Any(p => p.Nombre == nuevo.Nombre);
                    if (existe)
                        return "Ya existe un producto con ese nombre.";

                    db.Productos.Add(nuevo);
                    db.SaveChanges();
                }
                return "Producto agregado exitosamente.";
            }
            catch (Exception ex)
            {
                var inner = ex;
                string detalles = "";
                while (inner != null)
                {
                    detalles += inner.Message + " | ";
                    inner = inner.InnerException;
                }
                return $"Error al agregar producto: {detalles}";
            }
        }

        // 🔹 Aquí agregas el método EditarProducto
        public string EditarProducto(Producto productoEditado)
        {
            if (productoEditado == null)
                return "No se recibió ningún producto para editar.";

            if (string.IsNullOrWhiteSpace(productoEditado.Nombre) ||
                string.IsNullOrWhiteSpace(productoEditado.Tipo) ||
                string.IsNullOrWhiteSpace(productoEditado.Proveedor) ||
                productoEditado.PrecioUnitario <= 0 ||
                productoEditado.Threshold < 0)
            {
                return "No se puede editar el producto: hay campos en blanco o inválidos.";
            }

            try
            {
                using (var db = new Context())
                {
                    var productoExistente = db.Productos.Find(productoEditado.IdProducto);
                    if (productoExistente == null)
                        return "No se puede editar: el producto no existe.";

                    productoExistente.Nombre = productoEditado.Nombre;
                    productoExistente.Tipo = productoEditado.Tipo;
                    productoExistente.Proveedor = productoEditado.Proveedor;
                    productoExistente.PrecioUnitario = productoEditado.PrecioUnitario;
                    productoExistente.Threshold = productoEditado.Threshold;
                    productoExistente.Fecha = productoEditado.Fecha;

                    db.SaveChanges();
                }

                return "Producto editado exitosamente.";
            }
            catch (Exception ex)
            {
                var inner = ex;
                string detalles = "";
                while (inner != null)
                {
                    detalles += inner.Message + " | ";
                    inner = inner.InnerException;
                }
                return $"Error al editar producto: {detalles}";
            }

        }

        public string EliminarProducto(int idProducto)
        {
            try
            {
                using (var db = new Context())
                {
                    var producto = db.Productos.FirstOrDefault(p => p.IdProducto == idProducto);
                    if (producto == null)
                        return "No se encontró el producto.";

                    db.Productos.Remove(producto);
                    db.SaveChanges();
                }
                return "Producto eliminado exitosamente.";
            }
            catch (Exception ex)
            {
                var inner = ex;
                string detalles = "";
                while (inner != null)
                {
                    detalles += inner.Message + " | ";
                    inner = inner.InnerException;
                }
                return $"Error al eliminar producto: {detalles}";
            }
        }

    }
}
