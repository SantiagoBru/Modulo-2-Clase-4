CREATE DATABASE VentasDB;
GO

USE VentasDB;

-- Tabla para ventas mensuales
CREATE TABLE ventas_mensuales (
    id INT PRIMARY KEY IDENTITY,
    fecha_informe DATE,
    codigo_vendedor VARCHAR(3),
    venta DECIMAL(11, 2),
    venta_empresa_grande BIT
);
GO

-- Tabla para parametria
CREATE TABLE parametria (
    id INT PRIMARY KEY IDENTITY,
    fecha_informe DATE
);
GO

-- Tabla para rechazos
CREATE TABLE rechazos (
    id INT PRIMARY KEY IDENTITY,
    fecha_informe VARCHAR(10),
    codigo_vendedor VARCHAR(3),
    venta VARCHAR(11),
    venta_empresa_grande VARCHAR(1),
    error_descripcion VARCHAR(255)
);
GO

-- Insertar valor de fecha en la tabla parametria
INSERT INTO parametria (fecha_informe) VALUES ('2023-06-18');
GO
