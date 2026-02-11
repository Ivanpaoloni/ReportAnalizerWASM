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

            // 1. ESTRATEGIA DE AÑO ROBUSTA
            // Por defecto usamos el año actual. Si el archivo es viejo, intentaremos detectarlo.
            int anioReporte = DateTime.Now.Year;

            // Leemos primeras filas buscando "Ventas desde... 202X"
            int filasLeidas = 0;
            while (reader.Read() && filasLeidas < 8) // Leemos un poco más abajo por si acaso
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var celda = reader.GetValue(i)?.ToString() ?? "";
                    // Buscamos patrones de año explícitos
                    var matchAño = Regex.Match(celda, @"202[0-9]");
                    if (matchAño.Success && int.TryParse(matchAño.Value, out int a))
                    {
                        anioReporte = a;
                        // Console.WriteLine($"[DEBUG] Año detectado en encabezado: {anioReporte}");
                        break;
                    }
                }
                filasLeidas++;
            }
            reader.Reset(); // Volvemos al inicio

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
                    // Leemos el ID como string para evitar problemas de formato numérico
                    item.IdOperacion = reader.GetValue(cOperacion)?.ToString();

                    if (!string.IsNullOrEmpty(item.IdOperacion))
                    {
                        // --- PARSEO DE FECHA CRÍTICO ---
                        var objFecha = cFecha != -1 ? reader.GetValue(cFecha) : null;

                        // Caso A: Excel ya lo interpreta como fecha
                        if (objFecha is DateTime fechaYaLista)
                        {
                            item.Fecha = fechaYaLista;
                        }
                        // Caso B: Viene como texto ("9 feb 20:59 hs")
                        else
                        {
                            item.FechaRaw = objFecha?.ToString() ?? "";
                            item.Fecha = ParsearFechaMP(item.FechaRaw, anioReporte);
                        }

                        // SEGURIDAD FINAL: Si la fecha quedó en año 0001, la traemos al presente
                        // Esto evita que desaparezca del gráfico.
                        if (item.Fecha.Year < 2000)
                        {
                            item.Fecha = DateTime.Now;
                        }
                        // -------------------------------

                        item.IngresoBrutoRaw = cCobro != -1 ? reader.GetValue(cCobro)?.ToString() : "";
                        item.CostosRaw = cImpuestos != -1 ? reader.GetValue(cImpuestos)?.ToString() : "";
                        item.NetoRaw = cNeto != -1 ? reader.GetValue(cNeto)?.ToString() : "";
                        item.DesgloseFiscal = cResumen != -1 ? reader.GetValue(cResumen)?.ToString() : "";
                        item.Producto = cProd != -1 ? reader.GetValue(cProd)?.ToString() : "Varios";

                        ventas.Add(item);
                    }
                }
            }

            return ventas.OrderByDescending(x => x.Fecha).ToList();
        }

        private DateTime ParsearFechaMP(string fechaRaw, int anioContexto)
        {
            if (string.IsNullOrWhiteSpace(fechaRaw)) return DateTime.MinValue;

            try
            {
                // Limpieza agresiva: espacios duros, hs, mayúsculas
                var texto = fechaRaw.ToLower().Replace(" hs", "").Trim();
                // Eliminar caracteres invisibles (común en excels web)
                texto = Regex.Replace(texto, @"[^\w\s:.]", "");

                var meses = new Dictionary<string, int> {
                    {"ene", 1}, {"jan", 1}, {"feb", 2}, {"mar", 3}, {"abr", 4}, {"apr", 4},
                    {"may", 5}, {"jun", 6}, {"jul", 7}, {"ago", 8}, {"aug", 8},
                    {"sep", 9}, {"set", 9}, {"oct", 10}, {"nov", 11}, {"dic", 12}, {"dec", 12}
                };

                // Regex: Dia (1-2 digitos) + Mes (letras) + Hora
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