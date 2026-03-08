using ReportAnalizerWASM.Client.Models;
using MudBlazor;

namespace ReportAnalizerWASM.Client.Services
{
    public class AppStateService
    {
        // 1. El evento que notifica a los componentes cuando la UI debe actualizarse
        public event Action OnChange;

        // 2. Estado Global (Propiedades de solo lectura desde afuera para proteger los datos)
        public List<VentaItem> VentasTodas { get; private set; } = new();
        public List<VentaItem> VentasFiltradas { get; private set; } = new();
        public DateRange RangoFechas { get; private set; } = new DateRange(null, null);

        // 3. Lógica de Negocio Centralizada

        /// <summary>
        /// Carga el listado completo de ventas proveniente del Excel o de LocalStorage.
        /// </summary>
        public void SetVentas(List<VentaItem> ventas)
        {
            VentasTodas = ventas ?? new List<VentaItem>();

            if (VentasTodas.Any())
            {
                var min = VentasTodas.Min(v => v.Fecha.Date);
                var max = VentasTodas.Max(v => v.Fecha.Date);
                RangoFechas = new DateRange(min, max);
            }
            else
            {
                RangoFechas = new DateRange(null, null);
            }

            AplicarFiltros();
        }

        /// <summary>
        /// Actualiza el rango de fechas seleccionado por el usuario.
        /// </summary>
        public void SetRangoFechas(DateRange rango)
        {
            RangoFechas = rango;
            AplicarFiltros();
        }

        /// <summary>
        /// Borra todos los datos del estado global.
        /// </summary>
        public void LimpiarDatos()
        {
            VentasTodas.Clear();
            VentasFiltradas.Clear();
            RangoFechas = new DateRange(null, null);
            NotificarCambio();
        }

        // --- MÉTODOS PRIVADOS ---

        private void AplicarFiltros()
        {
            if (!VentasTodas.Any())
            {
                VentasFiltradas.Clear();
            }
            else
            {
                var inicio = RangoFechas?.Start ?? DateTime.MinValue;
                var fin = RangoFechas?.End ?? DateTime.MaxValue;

                // Tomamos hasta el último segundo del día final para no perder ventas nocturnas
                fin = fin.Date.AddDays(1).AddTicks(-1);

                VentasFiltradas = VentasTodas.Where(v => v.Fecha >= inicio && v.Fecha <= fin).ToList();
            }

            NotificarCambio();
        }

        private void NotificarCambio() => OnChange?.Invoke();
    }
}