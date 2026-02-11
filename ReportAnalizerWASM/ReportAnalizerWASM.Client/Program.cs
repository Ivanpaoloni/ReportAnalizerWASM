using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ReportAnalizerWASM.Client;
using ReportAnalizerWASM.Client.Services; // <--- ESTE ES IMPORTANTE para encontrar App

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Ahora sí encontrará <App> porque creaste el archivo en el Paso 1
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<IVentasService, VentasService>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();

// Esto permite leer Excels viejos o con caracteres raros
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

await builder.Build().RunAsync();