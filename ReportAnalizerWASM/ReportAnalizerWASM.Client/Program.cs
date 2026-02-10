using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ReportAnalizerWASM.Client; // <--- ESTE ES IMPORTANTE para encontrar App

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Ahora sí encontrará <App> porque creaste el archivo en el Paso 1
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

await builder.Build().RunAsync();