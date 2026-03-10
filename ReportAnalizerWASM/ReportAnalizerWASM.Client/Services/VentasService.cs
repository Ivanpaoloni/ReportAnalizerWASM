using ExcelDataReader;
using Microsoft.AspNetCore.Components.Forms;
using ReportAnalizerWASM.Client.Models;
using System.Globalization;

namespace ReportAnalizerWASM.Client.Services
{
    public class VentasService : IVentasService
    {
        public async Task<List<VentaItem>> ProcesarArchivoVentas(IBrowserFile archivo)
        {
            var ventas = new List<VentaItem>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            using var stream = new MemoryStream();
            await archivo.OpenReadStream(maxAllowedSize: 15 * 1024 * 1024).CopyToAsync(stream);
            stream.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(stream);

            // 1. Mapeo de Columnas
            reader.Read();
            var columnas = new Dictionary<string, int>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var nombre = reader.GetValue(i)?.ToString()?.Trim().ToLower() ?? "";
                if (!string.IsNullOrEmpty(nombre)) columnas[nombre] = i;
            }

            int colFecha = BuscarColumna(columnas, "fecha de acreditación", "date_approved");
            int colProducto = BuscarColumna(columnas, "descripción de la operación", "reason");
            int colEmail = BuscarColumna(columnas, "e-mail de la contraparte", "counterpart_email");
            int colEstado = BuscarColumna(columnas, "estado de la operación", "status");
            int colTipoOp = BuscarColumna(columnas, "tipo de operación", "operation_type");
            int colBruto = BuscarColumna(columnas, "valor del producto", "transaction_amount");
            int colComision = BuscarColumna(columnas, "tarifa de mercado pago", "mercadopago_fee");
            int colEnvio = BuscarColumna(columnas, "costo de envío", "shipping_cost");
            int colNeto = BuscarColumna(columnas, "monto recibido", "net_received_amount");
            int colId = BuscarColumna(columnas, "número de operación", "operation_id");

            // 2. Procesamiento de Filas
            while (reader.Read())
            {
                // FILTRO A: Solo aprobadas
                var estado = ObtenerString(reader, colEstado);
                if (!estado.Contains("approved", StringComparison.OrdinalIgnoreCase)) continue;

                // FILTRO B: Solo ventas reales (Ignoramos account_fund, withdraw, etc.)
                var tipoOp = ObtenerString(reader, colTipoOp);
                if (tipoOp != "regular_payment" && tipoOp != "pos_payment") continue;

                var bruto = ObtenerDecimal(reader, colBruto);
                var comision = Math.Abs(ObtenerDecimal(reader, colComision));
                var envio = Math.Abs(ObtenerDecimal(reader, colEnvio));
                var neto = ObtenerDecimal(reader, colNeto);

                // Cálculo de impuestos (Diferencia entre lo que debería haber y lo que hay)
                var impuestos = bruto - comision - envio - neto;
                if (impuestos < 0) impuestos = 0;

                ventas.Add(new VentaItem
                {
                    IdOperacion = ObtenerString(reader, colId),
                    Fecha = ObtenerFecha(reader, colFecha),
                    Producto = ObtenerString(reader, colProducto),
                    Comprador = ObtenerString(reader, colEmail),
                    MontoBruto = bruto,
                    MontoComisionMP = comision,
                    MontoImpuestos = impuestos,
                    CostoEnvio = envio,
                    MontoNeto = neto
                });
            }
            return ventas;
        }

        private int BuscarColumna(Dictionary<string, int> cols, params string[] nombres) =>
            cols.FirstOrDefault(c => nombres.Any(n => c.Key.Contains(n))).Value;

        private decimal ObtenerDecimal(IExcelDataReader r, int i)
        {
            if (i < 0 || r.IsDBNull(i)) return 0;
            var val = r.GetValue(i).ToString()!.Replace("$", "").Trim();
            return decimal.TryParse(val, NumberStyles.Any, new CultureInfo("en-US"), out var res) ? res :
                   decimal.TryParse(val, NumberStyles.Any, new CultureInfo("es-AR"), out var res2) ? res2 : 0;
        }

        private string ObtenerString(IExcelDataReader r, int i) => i < 0 || r.IsDBNull(i) ? "" : r.GetValue(i).ToString()!.Trim();

        private DateTime ObtenerFecha(IExcelDataReader r, int i)
        {
            if (i < 0 || r.IsDBNull(i)) return DateTime.Today;
            var v = r.GetValue(i);
            return v is DateTime dt ? dt : DateTime.TryParse(v?.ToString(), out var p) ? p : DateTime.Today;
        }
    }
}