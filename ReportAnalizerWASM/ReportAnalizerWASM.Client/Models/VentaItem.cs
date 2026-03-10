using System;

namespace ReportAnalizerWASM.Client.Models
{
    public class VentaItem
    {
        public string IdOperacion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }

        public string Producto { get; set; } = string.Empty;
        public int Cantidad { get; set; } = 1;

        // ¡Agregamos el comprador para el futuro módulo de Clientes!
        public string Comprador { get; set; } = string.Empty;

        // --- VALORES NUMÉRICOS FINANCIEROS (Asignados desde el Excel directamente) ---
        public decimal MontoBruto { get; set; }

        // Lo que nos cobra MP
        public decimal MontoComisionMP { get; set; }

        // El cálculo matemático que hace nuestro servicio (Bruto - Comision - Envio - Neto)
        public decimal MontoImpuestos { get; set; }

        // Si pagaste envío
        public decimal CostoEnvio { get; set; }

        // La plata limpia en tu cuenta
        public decimal MontoNeto { get; set; }
    }
}