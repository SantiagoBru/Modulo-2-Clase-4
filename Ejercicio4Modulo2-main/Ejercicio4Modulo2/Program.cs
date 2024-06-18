using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;

namespace Ejercicio4Modulo2
{
    public class VentaMensual
    {
        public DateTime FechaInforme { get; set; }
        public string? CodigoVendedor { get; set; }
        public decimal Venta { get; set; }
        public bool VentaEmpresaGrande { get; set; }
    }

    public class Rechazo
    {
        public string? FechaInforme { get; set; }
        public string? CodigoVendedor { get; set; }
        public string? Venta { get; set; }
        public string? VentaEmpresaGrande { get; set; }
        public string? ErrorDescripcion { get; set; }
    }

    internal class Program
    {
        private static string connectionString = "Server= ;Database=VentasDB;User Id= ;Password= ;";

        static void Main(string[] args)
        {
            string path = $"{AppDomain.CurrentDomain.BaseDirectory}\\data.txt";
            List<VentaMensual> ventasMensuales = new List<VentaMensual>();
            List<Rechazo> rechazos = new List<Rechazo>();

            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);

                DateTime fechaInformeParam;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("SELECT TOP 1 fecha_informe FROM parametria", connection))
                    {
                        fechaInformeParam = (DateTime)command.ExecuteScalar();
                    }
                }

                foreach (var line in lines)
                {
                    try
                    {
                        var fechaInforme = DateTime.ParseExact(line.Substring(0, 10).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        var codigoVendedor = line.Substring(10, 3).Trim();
                        var ventaStr = line.Substring(13, 11).Trim();
                        var ventaEmpresaGrande = line.Substring(24, 1).Trim();

                        if (string.IsNullOrEmpty(codigoVendedor))
                        {
                            rechazos.Add(new Rechazo { FechaInforme = fechaInforme.ToString("yyyy-MM-dd"), CodigoVendedor = codigoVendedor, Venta = ventaStr, VentaEmpresaGrande = ventaEmpresaGrande, ErrorDescripcion = "Código de vendedor vacío" });
                            continue;
                        }

                        if (fechaInforme != fechaInformeParam)
                        {
                            rechazos.Add(new Rechazo { FechaInforme = fechaInforme.ToString("yyyy-MM-dd"), CodigoVendedor = codigoVendedor, Venta = ventaStr, VentaEmpresaGrande = ventaEmpresaGrande, ErrorDescripcion = "Fecha del informe no coincide con la parametria" });
                            continue;
                        }

                        if (ventaEmpresaGrande != "S" && ventaEmpresaGrande != "N")
                        {
                            rechazos.Add(new Rechazo { FechaInforme = fechaInforme.ToString("yyyy-MM-dd"), CodigoVendedor = codigoVendedor, Venta = ventaStr, VentaEmpresaGrande = ventaEmpresaGrande, ErrorDescripcion = "Flag 'Venta a empresa grande' inválido" });
                            continue;
                        }

                        var venta = decimal.Parse(ventaStr, CultureInfo.InvariantCulture);

                        ventasMensuales.Add(new VentaMensual
                        {
                            FechaInforme = fechaInforme,
                            CodigoVendedor = codigoVendedor,
                            Venta = venta,
                            VentaEmpresaGrande = ventaEmpresaGrande == "S"
                        });
                    }
                    catch (Exception ex)
                    {
                        rechazos.Add(new Rechazo { FechaInforme = line.Substring(0, 10).Trim(), CodigoVendedor = line.Substring(10, 3).Trim(), Venta = line.Substring(13, 11).Trim(), VentaEmpresaGrande = line.Substring(24, 1).Trim(), ErrorDescripcion = ex.Message });
                    }
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    foreach (var venta in ventasMensuales)
                    {
                        using (SqlCommand command = new SqlCommand("INSERT INTO ventas_mensuales (fecha_informe, codigo_vendedor, venta, venta_empresa_grande) VALUES (@FechaInforme, @CodigoVendedor, @Venta, @VentaEmpresaGrande)", connection))
                        {
                            command.Parameters.AddWithValue("@FechaInforme", venta.FechaInforme);
                            command.Parameters.AddWithValue("@CodigoVendedor", venta.CodigoVendedor);
                            command.Parameters.AddWithValue("@Venta", venta.Venta);
                            command.Parameters.AddWithValue("@VentaEmpresaGrande", venta.VentaEmpresaGrande);

                            command.ExecuteNonQuery();
                        }
                    }

                    foreach (var rechazo in rechazos)
                    {
                        using (SqlCommand command = new SqlCommand("INSERT INTO rechazos (fecha_informe, codigo_vendedor, venta, venta_empresa_grande, error_descripcion) VALUES (@FechaInforme, @CodigoVendedor, @Venta, @VentaEmpresaGrande, @ErrorDescripcion)", connection))
                        {
                            command.Parameters.AddWithValue("@FechaInforme", rechazo.FechaInforme);
                            command.Parameters.AddWithValue("@CodigoVendedor", rechazo.CodigoVendedor);
                            command.Parameters.AddWithValue("@Venta", rechazo.Venta);
                            command.Parameters.AddWithValue("@VentaEmpresaGrande", rechazo.VentaEmpresaGrande);
                            command.Parameters.AddWithValue("@ErrorDescripcion", rechazo.ErrorDescripcion);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                GenerarReportes();
            }
            else
            {
                Console.WriteLine("El archivo data.txt no existe.");
            }
        }

        public static void GenerarReportes()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("SELECT codigo_vendedor, SUM(venta) as total_ventas FROM ventas_mensuales GROUP BY codigo_vendedor HAVING SUM(venta) > 100000", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Vendedores que superaron los 100.000 en el mes:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"El vendedor {reader["codigo_vendedor"]} vendió {reader["total_ventas"]}");
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand("SELECT codigo_vendedor, SUM(venta) as total_ventas FROM ventas_mensuales GROUP BY codigo_vendedor HAVING SUM(venta) <= 100000", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Vendedores que no superaron los 100.000 en el mes:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"El vendedor {reader["codigo_vendedor"]} vendió {reader["total_ventas"]}");
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand("SELECT DISTINCT codigo_vendedor FROM ventas_mensuales WHERE venta_empresa_grande = 1", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Vendedores que vendieron al menos una vez a una empresa grande:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"{reader["codigo_vendedor"]}");
                        }
                    }
                }

                using (SqlCommand command = new SqlCommand("SELECT * FROM rechazos", connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Rechazos:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"Fecha: {reader["fecha_informe"]}, Vendedor: {reader["codigo_vendedor"]}, Venta: {reader["venta"]}, Empresa Grande: {reader["venta_empresa_grande"]}, Error: {reader["error_descripcion"]}");
                        }
                    }
                }
            }
        }
    }
}
