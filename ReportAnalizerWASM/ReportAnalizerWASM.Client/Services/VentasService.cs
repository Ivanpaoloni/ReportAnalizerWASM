using ExcelDataReader;
using System.Text.RegularExpressions;
using ReportAnalizerWASM.Client.Models;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace ReportAnalizerWASM.Client.Services
{
    public class VentasService : IVentasService
    {
        public async Task<List<VentaItem>> ProcesarArchivoVentas(IBrowserFile archivo)
        {
            var ventas = new List<VentaItem>();

            using var stream = archivo.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Position = 0;

            using var reader = ExcelReaderFactory.CreateReader(ms);

            int anioReporte = DateTime.Now.Year;

            int filasLeidas = 0;
            while (reader.Read() && filasLeidas < 8)
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var celda = reader.GetValue(i)?.ToString() ?? "";
                    var matchAño = Regex.Match(celda, @"202[0-9]");
                    if (matchAño.Success && int.TryParse(matchAño.Value, out int a))
                    {
                        anioReporte = a;
                        break;
                    }
                }
                filasLeidas++;
            }
            reader.Reset();

            // 2. MAPEO DE COLUMNAS
            bool cabeceraEncontrada = false;
            int cOperacion = -1, cFecha = -1, cCobro = -1, cResumen = -1, cImpuestos = -1, cNeto = -1, cProd = -1;

            while (reader.Read())
            {
                if (!cabeceraEncontrada)
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var v = reader.GetValue(i)?.ToString() ?? "";
                        if (v == "Número de operación") cOperacion = i;
                        if (v == "Fecha de la compra") cFecha = i;
                        if (v == "Cobro") cCobro = i;
                        if (v == "Resumen") cResumen = i;
                        if (v == "Cargos e impuestos") cImpuestos = i;
                        if (v == "Total a recibir") cNeto = i;
                        if (v == "Descripción del ítem") cProd = i;
                    }
                    if (cOperacion != -1) cabeceraEncontrada = true;
                }
                else
                {
                    var item = new VentaItem();
                    item.IdOperacion = reader.GetValue(cOperacion)?.ToString();

                    if (!string.IsNullOrEmpty(item.IdOperacion))
                    {
                        var objFecha = cFecha != -1 ? reader.GetValue(cFecha) : null;
                        if (objFecha is DateTime fechaYaLista)
                        {
                            item.Fecha = fechaYaLista;
                        }
                        else
                        {
                            item.FechaRaw = objFecha?.ToString() ?? "";
                            item.Fecha = ParsearFechaMP(item.FechaRaw, anioReporte);
                        }

                        if (item.Fecha.Year < 2000)
                        {
                            item.Fecha = DateTime.Now;
                        }

                        // LECTURA DE DINERO PRINCIPAL
                        item.MontoBruto = cCobro != -1 ? ParsearDinero(reader.GetValue(cCobro)) : 0;
                        item.MontoCostosTotal = cImpuestos != -1 ? ParsearDinero(reader.GetValue(cImpuestos)) : 0;
                        item.MontoNeto = cNeto != -1 ? ParsearDinero(reader.GetValue(cNeto)) : 0;

                        // TEXTO SUCIO PARA DESGLOSE
                        item.DesgloseFiscal = cResumen != -1 ? reader.GetValue(cResumen)?.ToString() : "";
                        item.Producto = cProd != -1 ? reader.GetValue(cProd)?.ToString() : "Varios";

                        ventas.Add(item);
                    }
                }
            }

            return ventas.OrderByDescending(x => x.Fecha).ToList();
        }

        private decimal ParsearDinero(object valorCelda)
        {
            if (valorCelda == null) return 0;

            // 1. Si Excel lo entiende como número, lo pasamos directo
            if (valorCelda is double d) return (decimal)d;
            if (valorCelda is decimal dec) return dec;
            if (valorCelda is int i) return (decimal)i;

            // 2. Si viene como texto ("$ 4.950,00"), lo limpiamos
            var texto = valorCelda.ToString().Replace("$", "").Replace(" ", "").Trim();

            // Forzamos cultura argentina para que entienda que la coma (,) son los centavos
            if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.GetCultureInfo("es-AR"), out decimal numAr))
                return numAr;

            return 0;
        }

        private DateTime ParsearFechaMP(string fechaRaw, int anioContexto)
        {
            if (string.IsNullOrWhiteSpace(fechaRaw)) return DateTime.MinValue;

            try
            {
                var texto = fechaRaw.ToLower().Replace(" hs", "").Trim();
                texto = Regex.Replace(texto, @"[^\w\s:.]", "");

                var meses = new Dictionary<string, int> {
                    {"ene", 1}, {"jan", 1}, {"feb", 2}, {"mar", 3}, {"abr", 4}, {"apr", 4},
                    {"may", 5}, {"jun", 6}, {"jul", 7}, {"ago", 8}, {"aug", 8},
                    {"sep", 9}, {"set", 9}, {"oct", 10}, {"nov", 11}, {"dic", 12}, {"dec", 12}
                };

                var match = Regex.Match(texto, @"(\d{1,2})\s+([a-z]{3,4}).*?(\d{1,2}:\d{2})");

                if (match.Success)
                {
                    int dia = int.Parse(match.Groups[1].Value);
                    string mesStr = match.Groups[2].Value.Substring(0, 3);
                    string horaStr = match.Groups[3].Value;

                    int mes = meses.ContainsKey(mesStr) ? meses[mesStr] : 1;

                    var fecha = new DateTime(anioContexto, mes, dia);
                    var partesHora = horaStr.Split(':');
                    return fecha.AddHours(int.Parse(partesHora[0])).AddMinutes(int.Parse(partesHora[1]));
                }
            }
            catch { }

            return DateTime.MinValue;
        }
    }
}