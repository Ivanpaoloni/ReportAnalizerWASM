using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportAnalizerWASM.Client.Models
{
    public class VentaItem
    {
        public string IdOperacion { get; set; }
        public string FechaRaw { get; set; }
        public string DesgloseFiscal { get; set; }
        public string Producto { get; set; }
        public int Cantidad { get; set; }
        public DateTime Fecha { get; set; }

        // --- VALORES NUMÉRICOS PRINCIPALES (Asignados desde el Servicio) ---
        public decimal MontoBruto { get; set; }
        public decimal MontoNeto { get; set; }
        public decimal MontoCostosTotal { get; set; }


        // --- DESGLOSE DE DEDUCCIONES (Tu lógica original intacta) ---

        // 1. Impuestos (Retenciones IIBB, IVA, Ganancias)
        public decimal MontoImpuestos => CalcularConcepto(new[] { "Retención", "Percepción", "Impuesto", "IIBB", "IVA", "Sircreb" });

        // 2. Envíos (Si MP te cobra el envío)
        public decimal MontoEnvio => CalcularConcepto(new[] { "envío", "shipping", "correo" });

        // 3. Comisión MP (El resto: Arancel, Liberación, Procesamiento)
        public decimal MontoComisionMP => MontoCostosTotal - (MontoImpuestos + MontoEnvio);

        // --- LÓGICA DE PARSING PARA TEXTOS INTERNOS ---

        private decimal CalcularConcepto(string[] palabrasClave)
        {
            if (string.IsNullOrEmpty(DesgloseFiscal)) return 0;

            decimal total = 0;
            var lineas = DesgloseFiscal.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var linea in lineas)
            {
                if (palabrasClave.Any(p => linea.Contains(p, StringComparison.OrdinalIgnoreCase)))
                {
                    total += ExtraerMontoDeTexto(linea);
                }
            }
            return total;
        }

        private decimal ExtraerMontoDeTexto(string texto)
        {
            var match = Regex.Match(texto, @"(?:-|−)?\s?\$?\s?((?:-)?\d{1,3}(?:\.\d{3})*(?:,\d+)?)");

            if (match.Success)
            {
                string numeroLimpio = match.Groups[1].Value.Replace(".", "");
                bool esNegativo = texto.Contains("-") || texto.Contains("−");

                if (decimal.TryParse(numeroLimpio, NumberStyles.Any, new CultureInfo("es-AR"), out decimal valor))
                {
                    return Math.Abs(valor) * (esNegativo ? -1 : 1);
                }
            }
            return 0;
        }
    }
}