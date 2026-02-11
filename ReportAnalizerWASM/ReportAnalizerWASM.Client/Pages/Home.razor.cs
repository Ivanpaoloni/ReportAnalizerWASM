using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using ReportAnalizerWASM.Client.Models;
using ReportAnalizerWASM.Client.Services;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportAnalizerWASM.Client.Pages
{
    public partial class Home
    {
        [Inject]
        private IVentasService _ventasService { get; set; }

        private List<VentaItem> _ventasTodas = new();      // Todos los datos del archivo
        private List<VentaItem> _ventasFiltradas = new(); // Los datos que se ven en pantalla

        // Inicializamos con un rango por defecto (último mes) para evitar nulos
        private DateRange _rangoFechas = new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now);

        private bool _cargando = false;
        private string _error;
        private CultureInfo _cultureArg;

        // Gráfico de Evolución
        private List<ChartSeries> _seriesEvolucion = new();
        private string[] _labelsEvolucion = { };

        protected override void OnInitialized()
        {
            // Configuración regional para moneda argentina
            _cultureArg = (CultureInfo)CultureInfo.GetCultureInfo("es-AR").Clone();
            _cultureArg.NumberFormat.CurrencySymbol = "$";
        }

        private async Task CargarExcel(IBrowserFile archivo)
        {
            _cargando = true;
            _error = null;

            try
            {
                _ventasTodas = await _ventasService.ProcesarArchivoVentas(archivo);

                if (_ventasTodas.Any())
                {
                    // TRUCO: Buscamos el rango real de los datos cargados
                    var fechaMin = _ventasTodas.Min(x => x.Fecha.Date);
                    var fechaMax = _ventasTodas.Max(x => x.Fecha.Date);

                    // Si todas son hoy (porque falló el parseo), damos un margen visual de 1 mes atrás
                    if (fechaMin == fechaMax) fechaMin = fechaMin.AddMonths(-1);

                    // Actualizamos el DatePicker para que COINCIDA con los datos
                    _rangoFechas = new DateRange(fechaMin, fechaMax);
                }

                FiltrarVentas();
            }
            catch (Exception ex)
            {
                _error = $"Error crítico: {ex.Message}";
            }
            finally
            {
                _cargando = false;
                StateHasChanged(); // Forzamos refresco de pantalla
            }
        }

        // ESTE ES EL MÉTODO QUE FALTABA
        // Se llama cada vez que el usuario mueve el selector de fechas
        private void OnRangoCambiado(DateRange rango)
        {
            _rangoFechas = rango;
            FiltrarVentas();
        }

        private void FiltrarVentas()
        {
            // Solo filtramos si hay un rango válido seleccionado
            if (_rangoFechas.Start.HasValue && _rangoFechas.End.HasValue)
            {
                _ventasFiltradas = _ventasTodas
                    .Where(x => x.Fecha.Date >= _rangoFechas.Start.Value.Date &&
                                x.Fecha.Date <= _rangoFechas.End.Value.Date)
                    .OrderByDescending(x => x.Fecha) // Siempre ordenado por fecha
                    .ToList();
            }
            else
            {
                // Si borran el filtro, mostramos todo
                _ventasFiltradas = _ventasTodas.OrderByDescending(x => x.Fecha).ToList();
            }

            // Recalculamos los gráficos con los nuevos datos filtrados
            ActualizarGraficos();
        }

        private void ActualizarGraficos()
        {
            // PROTECCIÓN: Si no hay datos filtrados, limpiamos el gráfico y salimos
            if (_ventasFiltradas == null || !_ventasFiltradas.Any())
            {
                _seriesEvolucion = new List<ChartSeries>(); // Lista vacía
                _labelsEvolucion = Array.Empty<string>();
                return;
            }

            var ventasPorDia = _ventasFiltradas
                .GroupBy(x => x.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Fecha = g.Key, Total = (double)g.Sum(x => x.MontoBruto) })
                .ToList();

            // CHART SERIES
            _seriesEvolucion = new List<ChartSeries>()
            {
                new ChartSeries()
                {
                    Name = "Ventas ($)",
                    Data = ventasPorDia.Select(x => x.Total).ToArray()
                }
            };

            // ETIQUETAS (LABELS)
            int totalPuntos = ventasPorDia.Count;
            int paso = totalPuntos <= 10 ? 1 : (int)Math.Ceiling(totalPuntos / 10.0);

            _labelsEvolucion = ventasPorDia
                .Select((x, index) =>
                {
                    if (index % paso == 0 || index == totalPuntos - 1)
                        return x.Fecha.ToString("dd/MM");
                    return "";
                })
                .ToArray();
        }
    }
}