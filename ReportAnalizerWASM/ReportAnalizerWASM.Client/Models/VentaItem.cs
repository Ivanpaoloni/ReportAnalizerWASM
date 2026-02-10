using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportAnalizerWASM.Client.Models
{
    public class VentaItem
    {
        // Propiedades mapeadas desde el Excel
        public string IdOperacion { get; set; }
        public string FechaRaw { get; set; }
        public string IngresoBrutoRaw { get; set; } // Columna "Cobro"
        public string CostosRaw { get; set; }       // Columna "Cargos e impuestos" (El total negativo)
        public string NetoRaw { get; set; }         // Columna "Total a recibir"
        public string DesgloseFiscal { get; set; }  // Columna "Resumen" (El detalle texto)
        public string Producto { get; set; }
        public int Cantidad { get; set; }

        // --- VALORES NUMÉRICOS CALCULADOS ---

        public decimal MontoBruto => LimpiarDinero(IngresoBrutoRaw);
        public decimal MontoNeto => LimpiarDinero(NetoRaw);
        public decimal MontoCostosTotal => LimpiarDinero(CostosRaw); // Siempre es negativo o cero

        // --- DESGLOSE DE DEDUCCIONES ---

        // 1. Impuestos (Retenciones IIBB, IVA, Ganancias)
        public decimal MontoImpuestos => CalcularConcepto(new[] { "Retención", "Percepción", "Impuesto", "IIBB", "IVA", "Sircreb" });

        // 2. Envíos (Si MP te cobra el envío)
        public decimal MontoEnvio => CalcularConcepto(new[] { "envío", "shipping", "correo" });

        // 3. Comisión MP (El resto: Arancel, Liberación, Procesamiento)
        // Calculamos esto por diferencia para que los números cierren perfecto matemáticamente.
        public decimal MontoComisionMP => MontoCostosTotal - (MontoImpuestos + MontoEnvio);

        // --- LÓGICA DE PARSING ---

        private decimal CalcularConcepto(string[] palabrasClave)
        {
            if (string.IsNullOrEmpty(DesgloseFiscal)) return 0;

            decimal total = 0;
            // Normalizamos saltos de línea por si Excel usa \r\n o solo \n
            var lineas = DesgloseFiscal.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var linea in lineas)
            {
                // Si la línea contiene alguna palabra clave (ignorando mayúsculas/minúsculas)
                if (palabrasClave.Any(p => linea.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    total += ExtraerMontoDeTexto(linea);
                }
            }
            return total;
        }

        private decimal ExtraerMontoDeTexto(string texto)
        {
            // Buscamos patrones de dinero como: "$ -123,45" o "-$ 123.45" o "-123,45"
            // Regex robusto para capturar el número con signo negativo opcional
            var match = Regex.Match(texto, @"(?:-|−)?\s?\$?\s?((?:-)?\d{1,3}(?:\.\d{3})*(?:,\d+)?)");

            if (match.Success)
            {
                // Limpiamos el string para que sea un número parseable
                // Ejemplo entrada: "$ -1.234,56" -> Salida: "-1234,56"
                string numeroLimpio = match.Groups[1].Value.Replace(".", ""); // Sacamos punto de mil

                // Si el texto original tenía un signo menos explícito antes del match, lo aplicamos
                bool esNegativo = texto.Contains("-") || texto.Contains("−");

                if (decimal.TryParse(numeroLimpio, NumberStyles.Any, new CultureInfo("es-AR"), out decimal valor))
                {
                    // Si el valor parseado es positivo pero en el texto era una resta, lo volvemos negativo
                    // (Los costos siempre restan)
                    return Math.Abs(valor) * (esNegativo ? -1 : 1);
                }
            }
            return 0;
        }

        private decimal LimpiarDinero(string valor)
        {
            if (string.IsNullOrEmpty(valor)) return 0;
            // Limpieza básica para columnas directas
            var limpio = valor.Replace("$", "").Replace(" ", "").Trim();
            if (decimal.TryParse(limpio, NumberStyles.Any, new CultureInfo("es-AR"), out decimal resultado))
            {
                return resultado;
            }
            return 0;
        }
    }
}