using Blazored.LocalStorage;
using ExcelDataReader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
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
        [Inject] private IVentasService _ventasService { get; set; }
        [Inject] private IExportService _exportService { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; }
        [Inject] private ILocalStorageService _localStorage { get; set; }
        private const string CLAVE_DATOS = "ventas_mp_cache"; // El nombre del "cajón" donde guardaremos

        private List<VentaItem> _ventasTodas = new();      // Todos los datos del archivo
        private List<VentaItem> _ventasFiltradas = new(); // Los datos que se ven en pantalla

        // Inicializamos con un rango por defecto (último mes) para evitar nulos
        private DateRange _rangoFechas = new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now);

        private bool _cargando = false;
        private string _error;
        private CultureInfo _cultureArg;

        // Gráfico de Productos (Dona)
        private double[] _seriesProductos = { };
        private string[] _labelsProductos = { };

        // Gráfico de Evolución
        private List<ChartSeries> _seriesEvolucion = new();
        private string[] _labelsEvolucion = { };

        protected override async Task OnInitializedAsync()
        {
            _cultureArg = (CultureInfo)CultureInfo.GetCultureInfo("es-AR").Clone();
            _cultureArg.NumberFormat.CurrencySymbol = "$";

            // Intentamos buscar si hay datos de una sesión anterior
            var datosGuardados = await _localStorage.GetItemAsync<List<VentaItem>>(CLAVE_DATOS);
            
            if (datosGuardados != null && datosGuardados.Any())
            {
                _ventasTodas = datosGuardados;
                var fechaMin = _ventasTodas.Min(x => x.Fecha.Date);
                var fechaMax = _ventasTodas.Max(x => x.Fecha.Date);
                _rangoFechas = new DateRange(fechaMin, fechaMax);
                
                FiltrarVentas();
            }
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
                    var fechaMin = _ventasTodas.Min(x => x.Fecha.Date);
                    var fechaMax = _ventasTodas.Max(x => x.Fecha.Date);
                    _rangoFechas = new DateRange(fechaMin, fechaMax);

                    // 3. GUARDAMOS EN EL NAVEGADOR
                    await _localStorage.SetItemAsync(CLAVE_DATOS, _ventasTodas);
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
                StateHasChanged();
            }
        }
        private async Task LimpiarDatos()
        {
            await _localStorage.RemoveItemAsync(CLAVE_DATOS);
            _ventasTodas.Clear();
            _ventasFiltradas.Clear();
            _seriesEvolucion.Clear();
            StateHasChanged();
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
            if (!_ventasFiltradas.Any())
            {
                _seriesEvolucion.Clear();
                _labelsEvolucion = Array.Empty<string>();
                _seriesProductos = Array.Empty<double>();
                _labelsProductos = Array.Empty<string>();
                return;
            }

            // GRÁFICO DE LÍNEA Evolución
            var ventasPorDia = _ventasFiltradas
                .GroupBy(x => x.Fecha.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Fecha = g.Key, Total = (double)g.Sum(x => x.MontoBruto) })
                .ToList();

            _seriesEvolucion = new List<ChartSeries>()
            {
                new ChartSeries() { Name = "Ventas ($)", Data = ventasPorDia.Select(x => x.Total).ToArray() }
            };

            int totalPuntos = ventasPorDia.Count;
            int paso = totalPuntos <= 10 ? 1 : (int)Math.Ceiling(totalPuntos / 10.0);
            _labelsEvolucion = ventasPorDia.Select((x, i) => (i % paso == 0 || i == totalPuntos - 1) ? x.Fecha.ToString("dd/MM") : "").ToArray();

            // GRÁFICO DE DONA
            // los 5 mejores
            var topProductos = _ventasFiltradas
                .GroupBy(x => string.IsNullOrWhiteSpace(x.Producto) ? "Varios" : x.Producto)
                .Select(g => new { Nombre = g.Key, Total = (double)g.Sum(x => x.MontoNeto) })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            _seriesProductos = topProductos.Select(x => x.Total).ToArray();
            _labelsProductos = topProductos.Select(x => x.Nombre).ToArray();
        }
        private async Task DescargarReporteContable()
        {
            if (!_ventasFiltradas.Any()) return;

            // Excel en memoria usando el servicio
            var archivoBytes = _exportService.GenerarReporteContable(_ventasFiltradas);

            var base64 = Convert.ToBase64String(archivoBytes);
            var nombreArchivo = $"Liquidacion_MP_{_rangoFechas.Start?.ToString("dd-MM-yy")}_al_{_rangoFechas.End?.ToString("dd-MM-yy")}.xlsx";

            // Llamamos a la función JS que pusimos en el index.html
            await _jsRuntime.InvokeVoidAsync("descargarArchivo", nombreArchivo, base64);
        }
        private async Task DescargarReporteRentabilidad()
        {
            if (!_ventasFiltradas.Any()) return;

            var archivoBytes = _exportService.GenerarReporteRentabilidad(_ventasFiltradas);
            var base64 = Convert.ToBase64String(archivoBytes);

            var nombreArchivo = $"Rentabilidad_MP_{_rangoFechas.Start?.ToString("dd-MM-yy")}_al_{_rangoFechas.End?.ToString("dd-MM-yy")}.xlsx";

            await _jsRuntime.InvokeVoidAsync("descargarArchivo", nombreArchivo, base64);
        }
    }
}