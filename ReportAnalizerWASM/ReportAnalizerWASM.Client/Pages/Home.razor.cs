using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using ReportAnalizerWASM.Client.Models;
using ReportAnalizerWASM.Client.Services;
using System.Globalization;

namespace ReportAnalizerWASM.Client.Pages
{
    // IMPORTANTE: Agregamos IDisposable para manejar la memoria correctamente
    public partial class Home : IDisposable
    {
        [Inject] private IVentasService _ventasService { get; set; }
        [Inject] private IExportService _exportService { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; }
        [Inject] private ILocalStorageService _localStorage { get; set; }

        // 1. INYECTAMOS NUESTRO CEREBRO CENTRAL
        [Inject] private AppStateService _appState { get; set; }

        private const string CLAVE_DATOS = "ventas_mp_cache";

        // 2. MAGIA: Conectamos las variables de la vista directo al estado global
        // Usamos "=>" (getters) para que siempre lean la versión más actualizada del servicio
        private List<VentaItem> _ventasTodas => _appState.VentasTodas;
        private List<VentaItem> _ventasFiltradas => _appState.VentasFiltradas;
        private DateRange _rangoFechas => _appState.RangoFechas;

        private bool _cargando = false;
        private string _error;

        protected override async Task OnInitializedAsync()
        {
            // Nos suscribimos para escuchar cuando el AppState cambie los datos
            _appState.OnChange += OnAppStateChanged;

            var datosGuardados = await _localStorage.GetItemAsync<List<VentaItem>>(CLAVE_DATOS);
            if (datosGuardados != null && datosGuardados.Any())
            {
                // Al enviar esto al servicio, el servicio filtra automáticamente y avisa a OnAppStateChanged
                _appState.SetVentas(datosGuardados);
            }
        }

        private void OnAppStateChanged()
        {
            StateHasChanged();
        }

        public void Dispose()
        {
            _appState.OnChange -= OnAppStateChanged;
        }
        private async Task CargarExcel(IBrowserFile archivo)
        {
            _cargando = true;
            _error = null;

            try
            {
                var ventasCrudas = await _ventasService.ProcesarArchivoVentas(archivo);

                if (ventasCrudas.Any())
                {
                    // Guardamos en navegador
                    await _localStorage.SetItemAsync(CLAVE_DATOS, ventasCrudas);

                    // Guardamos en el estado global (esto actualiza toda la app al instante)
                    _appState.SetVentas(ventasCrudas);
                }
            }
            catch (Exception ex)
            {
                _error = $"Error crítico: {ex.Message}";
            }
            finally
            {
                _cargando = false;
            }
        }

        private async Task LimpiarDatos()
        {
            await _localStorage.RemoveItemAsync(CLAVE_DATOS);
            _appState.LimpiarDatos(); // Le avisamos al estado central que borre todo
        }

        private void OnRangoCambiado(DateRange rango)
        {
            // Delegamos el cambio de fechas al servicio central
            _appState.SetRangoFechas(rango);
        }

        
        private async Task DescargarReporteContable()
        {
            if (!_ventasFiltradas.Any()) return;
            var archivoBytes = _exportService.GenerarReporteContable(_ventasFiltradas);
            var base64 = Convert.ToBase64String(archivoBytes);
            var nombreArchivo = $"Liquidacion_MP_{_rangoFechas.Start?.ToString("dd-MM-yy")}_al_{_rangoFechas.End?.ToString("dd-MM-yy")}.xlsx";
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