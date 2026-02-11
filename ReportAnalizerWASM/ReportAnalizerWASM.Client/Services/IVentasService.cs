using Microsoft.AspNetCore.Components.Forms;
using ReportAnalizerWASM.Client.Models;

namespace ReportAnalizerWASM.Client.Services
{
    public interface IVentasService
    {
        Task<List<VentaItem>> ProcesarArchivoVentas(IBrowserFile archivo);
    }
}
