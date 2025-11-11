-- BD para Utopia Beauty Salon
CREATE DATABASE UtopiaBS_DB
GO
USE UtopiaBS_DB;
GO

-------------------------Tablas de catálogo-------------------------
CREATE TABLE Estado (
    IdEstado INT IDENTITY(1,1) PRIMARY KEY,
    NombreEstado NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE EstadoCita (
    IdEstadoCita INT IDENTITY(1,1) PRIMARY KEY,
    NombreEstado NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE TipoMembresia (
    IdTipoMembresia INT IDENTITY(1,1) PRIMARY KEY,
    NombreTipo NVARCHAR(50) NOT NULL,
    Descripcion NVARCHAR(200) NULL,
    Descuento DECIMAL(5,2) NULL, 
    Costo DECIMAL(10,2) NOT NULL 
);
GO

CREATE TABLE TipoMovimiento (
    IdTipoMovimiento INT IDENTITY(1,1) PRIMARY KEY,
    NombreTipo NVARCHAR(50) NOT NULL     
);
GO

CREATE TABLE TipoReporte (
    IdTipoReporte INT IDENTITY(1,1) PRIMARY KEY,
    NombreTipo NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE FormatoReporte (
    IdFormatoReporte INT IDENTITY(1,1) PRIMARY KEY,
    NombreFormato NVARCHAR(50) NOT NULL, 
    Extension NVARCHAR(10) NULL 
);
GO

------------------------- Tablas principales -------------------------

CREATE TABLE Clientes (
    IdCliente INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL, 
    IdTipoMembresia INT NULL,
    CONSTRAINT FK_Clientes_TipoMembresia FOREIGN KEY (IdTipoMembresia)
    REFERENCES TipoMembresia(IdTipoMembresia)
);
GO

CREATE TABLE Empleados (
    IdEmpleado INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NULL,
    Puesto NVARCHAR(100) NULL,
    Especialidad NVARCHAR(150) NULL
);
GO

CREATE TABLE Membresias (
    IdMembresia INT IDENTITY(1,1) PRIMARY KEY,
    IdCliente INT NOT NULL,
    IdTipoMembresia INT NOT NULL,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NULL, 
	PuntosAcumulados INT NOT NULL DEFAULT 0
    CONSTRAINT FK_Membresias_Cliente FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    CONSTRAINT FK_Membresias_Tipo FOREIGN KEY (IdTipoMembresia) REFERENCES TipoMembresia(IdTipoMembresia)
);
GO

CREATE TABLE Servicios (
    IdServicio INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Precio DECIMAL(10,2) NOT NULL,
    IdEstado INT NOT NULL,
    CONSTRAINT FK_Servicios_Estado FOREIGN KEY (IdEstado) REFERENCES Estado(IdEstado)
);
GO

CREATE TABLE Producto (
    IdProducto INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(150) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    Proveedor NVARCHAR(150) NULL,
    Tipo NVARCHAR(100) NULL,
    PrecioUnitario DECIMAL(12,2) NOT NULL,
    CantidadStock INT NOT NULL DEFAULT 0,
    IdEstado INT NOT NULL,
    CONSTRAINT FK_Producto_Estado FOREIGN KEY (IdEstado) REFERENCES Estado(IdEstado)
);
GO


ALTER TABLE Producto
ALTER COLUMN PrecioUnitario DECIMAL(12,2) NOT NULL;


CREATE TABLE Ventas (
    IdVenta INT IDENTITY(1,1) PRIMARY KEY,
    FechaVenta DATETIME NOT NULL DEFAULT GETDATE(),
	IdUsuario INT NOT NULL,
    Total DECIMAL(12,2) NOT NULL, 
    CONSTRAINT FK_Ventas_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario)
); 
GO

CREATE TABLE DetalleVentaProducto (
    IdDetalleProducto INT IDENTITY(1,1) PRIMARY KEY,
    IdVenta INT NOT NULL,
    IdProducto INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(12,2) NOT NULL,
    SubTotal AS (Cantidad * PrecioUnitario) PERSISTED,
    CONSTRAINT FK_DVP_Venta FOREIGN KEY (IdVenta) REFERENCES Ventas(IdVenta),
    CONSTRAINT FK_DVP_Producto FOREIGN KEY (IdProducto) REFERENCES Producto(IdProducto)
);
GO


CREATE TABLE DetalleVentaServicio (
    IdDetalleServicio INT IDENTITY(1,1) PRIMARY KEY,
    IdVenta INT NOT NULL,
    IdServicio INT NOT NULL,
    Cantidad INT NOT NULL CHECK (Cantidad > 0),
    PrecioUnitario DECIMAL(12,2) NOT NULL,
    SubTotal AS (Cantidad * PrecioUnitario) PERSISTED,
    CONSTRAINT FK_DVS_Venta FOREIGN KEY (IdVenta) REFERENCES Ventas(IdVenta),
    CONSTRAINT FK_DVS_Servicio FOREIGN KEY (IdServicio) REFERENCES Servicios(IdServicio),
);
GO

CREATE TABLE MovimientoContable (
    IdMovimiento INT IDENTITY(1,1) PRIMARY KEY,
    FechaMovimiento DATETIME NOT NULL DEFAULT GETDATE(),
    IdTipoMovimiento INT NOT NULL,
    Monto DECIMAL(12,2) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    IdVenta INT NULL,
    CONSTRAINT FK_Mov_Tipo FOREIGN KEY (IdTipoMovimiento) REFERENCES TipoMovimiento(IdTipoMovimiento),
    CONSTRAINT FK_Mov_Venta FOREIGN KEY (IdVenta) REFERENCES Ventas(IdVenta)
);
GO

CREATE TABLE Reportes (
    IdReporte INT IDENTITY(1,1) PRIMARY KEY,
    IdTipoReporte INT NOT NULL,
    IdFormatoReporte INT NOT NULL,
    GeneradoPor INT NOT NULL,
    FechaGeneracion DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Reportes_Tipo FOREIGN KEY (IdTipoReporte) REFERENCES TipoReporte(IdTipoReporte),
    CONSTRAINT FK_Reportes_Formato FOREIGN KEY (IdFormatoReporte) REFERENCES FormatoReporte(IdFormatoReporte),
    CONSTRAINT FK_Reportes_Usuario FOREIGN KEY (GeneradoPor) REFERENCES Usuarios(IdUsuario)
);
GO

CREATE TABLE Citas (
    IdCita INT IDENTITY(1,1) PRIMARY KEY,
    FechaHora DATETIME NOT NULL,
    IdCliente INT NOT NULL,    -- quien solicita la cita (usuario registrado)
    IdEmpleado INT NOT NULL,   -- quien atenderá
    IdServicio INT NOT NULL,
    IdEstadoCita INT NOT NULL,
    Observaciones NVARCHAR(500) NULL,
    CONSTRAINT FK_Citas_Cliente FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    CONSTRAINT FK_Citas_Empleado FOREIGN KEY (IdEmpleado) REFERENCES Empleados(IdEmpleado),
    CONSTRAINT FK_Citas_Servicio FOREIGN KEY (IdServicio) REFERENCES Servicios(IdServicio),
    CONSTRAINT FK_Citas_Estado FOREIGN KEY (IdEstadoCita) REFERENCES EstadoCita(IdEstadoCita)
);
GO
 
CREATE TABLE Resenas (
    IdResena INT IDENTITY(1,1) PRIMARY KEY,
    IdCliente INT NOT NULL,
    IdServicio INT NOT NULL,
    Comentario NVARCHAR(500) NULL,
    Puntuacion INT NOT NULL CHECK (Puntuacion BETWEEN 1 AND 5),
    FechaResena DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CONSTRAINT FK_Resenas_Cliente FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    CONSTRAINT FK_Resenas_Servicio FOREIGN KEY (IdServicio) REFERENCES Servicios(IdServicio)
);
GO

CREATE TABLE DescuentoProducto (
    DescuentoId INT IDENTITY(1,1) PRIMARY KEY,
    IdProducto INT NOT NULL,
    Tipo NVARCHAR(20) NOT NULL, -- 'Porcentaje' o 'MontoFijo'
    Valor DECIMAL(10,2) NOT NULL,
    FechaInicio DATETIME NOT NULL,
    FechaFin DATETIME NOT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (IdProducto) REFERENCES Producto(IdProducto)
);

CREATE TABLE [dbo].[CuponDescuento](
    [CuponId] INT IDENTITY(1,1) NOT NULL,              
    [Codigo] NVARCHAR(50) NOT NULL,                   
    [Tipo] NVARCHAR(20) NOT NULL,                     
    [Valor] DECIMAL(10,2) NOT NULL,                   
    [Activo] BIT NOT NULL CONSTRAINT DF_CuponDescuento_Activo DEFAULT(1), 
    [FechaInicio] DATETIME NOT NULL,                  
    [FechaFin] DATETIME NOT NULL,                     
    [UsoMaximo] INT NULL,                             
    [UsoActual] INT NOT NULL CONSTRAINT DF_CuponDescuento_UsoActual DEFAULT(0), 
    CONSTRAINT PK_CuponDescuento PRIMARY KEY CLUSTERED ([CuponId] ASC),
    CONSTRAINT UQ_CuponDescuento_Codigo UNIQUE NONCLUSTERED ([Codigo] ASC)
) ON [PRIMARY];
GO


------------------------CAMBIOS/ ALTER TABLES

--Agregar Tipo, Fecha y Threshold a la tabla Producto

ALTER TABLE Producto ADD Fecha DATE NOT NULL DEFAULT GETDATE(),
Threshold INT NOT NULL DEFAULT 0;


--Cambiar a Null las llaves foraneas para poder agregar Citas
ALTER TABLE Citas DROP CONSTRAINT FK_Citas_Cliente;
ALTER TABLE Citas DROP CONSTRAINT FK_Citas_Empleado;
ALTER TABLE Citas DROP CONSTRAINT FK_Citas_Servicio;

ALTER TABLE Citas
ALTER COLUMN IdCliente INT NULL;

ALTER TABLE Citas
ALTER COLUMN IdEmpleado INT NULL;

ALTER TABLE Citas
ALTER COLUMN IdServicio INT NULL;

ALTER TABLE Citas
ADD CONSTRAINT FK_Citas_Cliente FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente);

ALTER TABLE Citas
ADD CONSTRAINT FK_Citas_Empleado FOREIGN KEY (IdEmpleado) REFERENCES Empleados(IdEmpleado);

ALTER TABLE Citas
ADD CONSTRAINT FK_Citas_Servicio FOREIGN KEY (IdServicio) REFERENCES Servicios(IdServicio);

ALTER TABLE Ventas
ADD CuponId INT NULL,
    MontoDescuento DECIMAL(10,2) NULL,
    FOREIGN KEY (CuponId) REFERENCES CuponDescuento(CuponId);

    ALTER TABLE Citas
ADD FechaUltimoRecordatorio DATETIME NULL,
    RegistradoPor VARCHAR(100) NULL;


--INSERTS
-- Estado
INSERT INTO Estado (NombreEstado) VALUES 
('Activo'),
('Inactivo');
GO

-- EstadoCita
INSERT INTO EstadoCita (NombreEstado) VALUES 
('Pendiente'),
('Confirmada'),
('Cancelada'),
('Disponible');

-- TipoMembresia
INSERT INTO TipoMembresia (NombreTipo, Descripcion, Descuento, Costo) VALUES
('Básica', 'Membresía básica sin descuentos', 0, 0),
('Premium', 'Membresía con descuentos del 10%', 10, 50);

-- TipoMovimiento
INSERT INTO TipoMovimiento (NombreTipo) VALUES
('Ingreso'),
('Egreso');

-- TipoReporte
INSERT INTO TipoReporte (NombreTipo) VALUES
('Reporte de Ventas'),
('Reporte de Citas');

-- FormatoReporte
INSERT INTO FormatoReporte (NombreFormato, Extension) VALUES
('PDF', '.pdf'),
('Excel', '.xlsx');

-- Clientes
INSERT INTO Clientes (Nombre, IdTipoMembresia) VALUES
('Ana Pérez', 1),
('Luis Gómez', 2);

INSERT INTO Empleados (Nombre, Puesto, Especialidad) VALUES
('María López', 'Estilista', 'Cortes y peinados'),
('Juan Torres', 'Esteticista', 'Tratamientos faciales'),
('Sofía Ramírez', 'Manicurista', 'Uñas y manicure');

INSERT INTO Servicios (Nombre, Descripcion, Precio, IdEstado) VALUES
('Corte de cabello', 'Corte profesional para hombre y mujer', 9000, 1),
('Manicure', 'Manicure completo con esmaltado', 15000, 1),
('Tratamiento facial', 'Limpieza profunda y rejuvenecimiento', 17000, 1);


INSERT INTO Membresias (IdCliente, IdTipoMembresia, FechaInicio, FechaFin, PuntosAcumulados) VALUES
(1, 1, '2025-09-01', NULL, 0),
(2, 2, '2025-09-10', NULL, 20);


INSERT INTO Citas (FechaHora, IdEmpleado, IdServicio, IdEstadoCita, Observaciones) VALUES
('2025-09-28 10:00',  1, 1, 1, 'Corte regular'),
('2025-09-28 11:00', 2, 3, 1, 'Tratamiento premium'),
('2025-09-30 17:00',  3, 3, 1, 'Tinte'),
('2025-09-30 17:00',  2, 3, 1, 'Tratamiento premium');


INSERT INTO Resenas (IdCliente, IdServicio, Comentario, Puntuacion) VALUES
(1, 1, 'Muy buen corte, quedé satisfecho.', 5),
(2, 3, 'Excelente tratamiento facial.', 4);


INSERT INTO [dbo].[CuponDescuento]
    ([Codigo], [Tipo], [Valor], [FechaInicio], [FechaFin], [Activo], [UsoMaximo], [UsoActual])
VALUES
    ('BIENVENIDO10', 'Porcentaje', 10, GETDATE(), DATEADD(DAY, 30, GETDATE()), 1, 100, 0),
    ('FREESHIP', 'Monto', 2000, GETDATE(), DATEADD(DAY, 15, GETDATE()), 1, 50, 0),
    ('PRIMERA20', 'Porcentaje', 20, GETDATE(), DATEADD(DAY, 60, GETDATE()), 1, 200, 0),
    ('VIP30', 'Porcentaje', 30, GETDATE(), DATEADD(DAY, 45, GETDATE()), 1, 20, 0),
    ('DESCUENTO5K', 'Monto', 5000, GETDATE(), DATEADD(DAY, 10, GETDATE()), 1, 10, 0);

    ALTER TABLE Ventas
ADD CuponId INT NULL,
    MontoDescuento DECIMAL(10,2) NULL,
    FOREIGN KEY (CuponId) REFERENCES CuponDescuento(CuponId);

INSERT INTO EstadoCita ( NombreEstado)
VALUES ('Completada');


ALTER TABLE AspNetUsers
ADD Nombre NVARCHAR(100) NULL,
    Apellido NVARCHAR(100) NULL,
    Cedula NVARCHAR(50) NULL,
    FechaNacimiento DATE NULL;


    USE UtopiaBS_DB;
GO

---------------------------------------------------
-- 1. Eliminar las llaves foráneas existentes
---------------------------------------------------

ALTER TABLE Ventas DROP CONSTRAINT FK_Ventas_Usuario;
ALTER TABLE Reportes DROP CONSTRAINT FK_Reportes_Usuario;

---------------------------------------------------
-- 2. Cambiar columna IdUsuario / GeneradoPor a NVARCHAR(450)
--   (Tipo de clave de AspNetUsers.Id)
---------------------------------------------------

ALTER TABLE Ventas ALTER COLUMN IdUsuario NVARCHAR(128) NOT NULL;
ALTER TABLE Reportes ALTER COLUMN GeneradoPor NVARCHAR(128) NOT NULL;


---------------------------------------------------
-- 3. Crear nueva relación con AspNetUsers
---------------------------------------------------

ALTER TABLE Ventas
ADD CONSTRAINT FK_Ventas_AspNetUsers FOREIGN KEY (IdUsuario)
REFERENCES AspNetUsers(Id);

ALTER TABLE Reportes
ADD CONSTRAINT FK_Reportes_AspNetUsers FOREIGN KEY (GeneradoPor)
REFERENCES AspNetUsers(Id);

---------------------------------------------------
ALTER TABLE Clientes
ADD IdUsuario NVARCHAR(128) NULL
    CONSTRAINT FK_Clientes_AspNetUsers
    REFERENCES AspNetUsers(Id);

    ALTER TABLE Empleados
ADD IdUsuario NVARCHAR(128) NULL
    CONSTRAINT FK_Empleados_AspNetUsers
    REFERENCES AspNetUsers(Id);

---------------------------------------------------

ALTER TABLE Usuarios DROP CONSTRAINT [FK_Usuarios_Rol];
ALTER TABLE Usuarios DROP CONSTRAINT [FK_Usuarios_Estado];
ALTER TABLE Usuarios DROP CONSTRAINT [UQ__Usuarios__60695A19532DD380];

drop table UsuarioEmpleados
drop table UsuarioClientes
drop table Usuarios
drop table Rol

-- Debe devolver una fila
SELECT kc.name AS PKName
FROM sys.key_constraints kc
JOIN sys.tables t ON kc.parent_object_id = t.object_id
WHERE kc.[type] = 'PK' AND t.name = 'Clientes';

CREATE INDEX IX_Clientes_IdUsuario ON Clientes(IdUsuario);

ALTER TABLE Ventas DROP CONSTRAINT FK_Ventas_AspNetUsers;

ALTER TABLE Ventas
ALTER COLUMN IdUsuario NVARCHAR(128) NOT NULL;

ALTER TABLE Ventas
ADD CONSTRAINT FK_Ventas_AspNetUsers
FOREIGN KEY (IdUsuario) REFERENCES AspNetUsers(Id);

ALTER TABLE Ingresos DROP CONSTRAINT IF EXISTS FK_Ingresos
ALTER TABLE Egresos DROP CONSTRAINT IF EXISTS FK_Egresos

ALTER TABLE Ingresos ALTER COLUMN UsuarioId NVARCHAR(128) NULL;
ALTER TABLE Egresos ALTER COLUMN UsuarioId NVARCHAR(128) NULL;

ALTER TABLE Ingresos
ADD CONSTRAINT FK_Ingresos_AspNetUsers
FOREIGN KEY (UsuarioId)
REFERENCES AspNetUsers(Id);

ALTER TABLE Egresos
ADD CONSTRAINT FK_Egresos_AspNetUsers
FOREIGN KEY (UsuarioId)
REFERENCES AspNetUsers(Id);

use UtopiaBS_DB
go
ALTER TABLE Producto ALTER COLUMN Descripcion NVARCHAR(500) NULL;
ALTER TABLE Producto ALTER COLUMN Tipo NVARCHAR(MAX) NULL;
ALTER TABLE Producto ALTER COLUMN PrecioUnitario DECIMAL(18,2) NOT NULL;
ALTER TABLE Producto ALTER COLUMN Fecha DATETIME NOT NULL;
ALTER TABLE Producto ALTER COLUMN Proveedor NVARCHAR(150) NOT NULL;