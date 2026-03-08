using ClosedXML.Excel;
using ReportAnalizerWASM.Client.Models;

namespace ReportAnalizerWASM.Client.Services
{
    public interface IExportService
    {
        public byte[] GenerarReporteContable(List<VentaItem> ventas);
        public byte[] GenerarReporteRentabilidad(List<VentaItem> ventas); 
    }

    public class ExportService : IExportService
    {
        public byte[] GenerarReporteContable(List<VentaItem> ventas)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Liquidación Contable");

            // 1. Configurar Cabeceras
            string[] cabeceras = {
                "Fecha", "Nro. Operación", "Detalle",
                "Bruto Cobrado", "Retenciones/Impuestos", "Neto en Cuenta"
            };

            for (int i = 0; i < cabeceras.Length; i++)
            {
                var celda = worksheet.Cell(1, i + 1);
                celda.Value = cabeceras[i];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            // 2. Llenar Datos
            int fila = 2;
            foreach (var venta in ventas)
            {
                worksheet.Cell(fila, 1).Value = venta.Fecha.Date;
                worksheet.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";

                worksheet.Cell(fila, 2).Value = venta.IdOperacion;
                worksheet.Cell(fila, 3).Value = venta.Producto;

                // CORRECCIÓN: Casteamos explícitamente a (double) para evitar el bug de escala de ClosedXML
                worksheet.Cell(fila, 4).Value = (double)venta.MontoBruto;
                worksheet.Cell(fila, 4).Style.NumberFormat.Format = "$ #,##0.00";

                worksheet.Cell(fila, 5).Value = (double)venta.MontoImpuestos;
                worksheet.Cell(fila, 5).Style.NumberFormat.Format = "$ #,##0.00";

                worksheet.Cell(fila, 6).Value = (double)venta.MontoNeto;
                worksheet.Cell(fila, 6).Style.NumberFormat.Format = "$ #,##0.00";

                fila++;
            }

            // 3. Estética: Auto-ajustar columnas y crear formato de tabla
            var rangoDatos = worksheet.Range(1, 1, fila - 1, cabeceras.Length);
            var tabla = rangoDatos.CreateTable();
            tabla.Theme = XLTableTheme.TableStyleMedium2; // Un diseño azul profesional
            worksheet.Columns().AdjustToContents();

            // 4. Convertir a Bytes para enviarlo al navegador
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        public byte[] GenerarReporteRentabilidad(List<VentaItem> ventas)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Rentabilidad Comercial");

            // 1. Cabeceras Estratégicas
            string[] cabeceras = {
                "Producto", "Unidades Vendidas", "Facturación Bruta",
                "Comisiones MP", "Impuestos (IIBB/IVA)", "Costo Envío",
                "Ganancia Neta", "Margen Real (%)"
            };

            for (int i = 0; i < cabeceras.Length; i++)
            {
                var celda = worksheet.Cell(1, i + 1);
                celda.Value = cabeceras[i];
                celda.Style.Font.Bold = true;
                celda.Style.Fill.BackgroundColor = XLColor.DarkBlue;
                celda.Style.Font.FontColor = XLColor.White;
            }

            // 2. Inteligencia de Negocio (Agrupamiento con LINQ)
            var ventasPorProducto = ventas
                .GroupBy(v => string.IsNullOrWhiteSpace(v.Producto) ? "Sin Especificar" : v.Producto)
                .Select(g => new
                {
                    Producto = g.Key,
                    Unidades = g.Count(),
                    FacturacionBruta = g.Sum(x => x.MontoBruto),
                    // Usamos Math.Abs para que los costos se vean como números positivos en el reporte
                    Comisiones = g.Sum(x => Math.Abs(x.MontoComisionMP)),
                    Impuestos = g.Sum(x => Math.Abs(x.MontoImpuestos)),
                    Envios = g.Sum(x => Math.Abs(x.MontoEnvio)),
                    GananciaNeta = g.Sum(x => x.MontoNeto)
                })
                .OrderByDescending(x => x.GananciaNeta) // Los más rentables arriba
                .ToList();

            // 3. Llenado Seguro de Celdas
            int fila = 2;
            foreach (var item in ventasPorProducto)
            {
                worksheet.Cell(fila, 1).Value = item.Producto;
                worksheet.Cell(fila, 2).Value = item.Unidades;

                worksheet.Cell(fila, 3).Value = (double)item.FacturacionBruta;
                worksheet.Cell(fila, 4).Value = (double)item.Comisiones;
                worksheet.Cell(fila, 5).Value = (double)item.Impuestos;
                worksheet.Cell(fila, 6).Value = (double)item.Envios;
                worksheet.Cell(fila, 7).Value = (double)item.GananciaNeta;

                // Cálculo del Margen de Ganancia
                decimal margen = item.FacturacionBruta > 0 ? (item.GananciaNeta / item.FacturacionBruta) : 0;
                worksheet.Cell(fila, 8).Value = (double)margen;

                // Formateo visual
                worksheet.Range(fila, 3, fila, 7).Style.NumberFormat.Format = "$ #,##0.00";
                worksheet.Cell(fila, 8).Style.NumberFormat.Format = "0.00%";

                fila++;
            }

            // 4. Estética de la Tabla
            var rangoDatos = worksheet.Range(1, 1, fila - 1, cabeceras.Length);
            var tabla = rangoDatos.CreateTable();
            tabla.Theme = XLTableTheme.TableStyleMedium15; // Un tono azul/gris distinto al otro reporte
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
