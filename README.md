# MP Analyzer - Dashboard de Rentabilidad Real 🚀

https://ivanpaoloni.github.io/ReportAnalizerWASM/

MP Analyzer es una herramienta de Inteligencia de Negocios (BI) diseñada para vendedores de Mercado Pago en Argentina. Permite transformar los reportes de "Cobros" (comercialmente ricos pero contablemente confusos) en un tablero de control estratégico con márgenes netos reales.

💡 El Problema
Los reportes de Mercado Pago suelen mezclar ingresos por ventas, transferencias propias (DEBIN) y retenciones impositivas sin un desglose claro de rentabilidad por producto. Los comerciantes "venden mucho, pero no saben cuánto ganan" después de comisiones e impuestos.

✨ Características Principales
Procesamiento Local: Los datos se procesan 100% en el navegador (Client-Side) mediante Blazor WebAssembly. Privacidad total: los datos financieros nunca tocan un servidor externo.

Filtro Inteligente de Operaciones: Identifica y separa automáticamente ventas reales (regular_payment) de movimientos de fondos propios (account_fund), evitando duplicaciones de facturación.

Cálculo Dinámico de Impuestos: Deduce matemáticamente las retenciones ocultas (IVA, IIBB, SIRCREB) comparando el Monto Bruto vs. el Monto Recibido Neto.

Análisis de Productos Top: Agrupa ventas por ítem para mostrar cuáles son los productos con mayor margen de ganancia real.

Interfaz Adaptativa: Diseño moderno con MudBlazor, soporte para Modo Oscuro persistente y Onboarding guiado para nuevos usuarios.

Exportación Profesional: Genera reportes en Excel (.xlsx) listos para enviar al contador o para análisis interno de stock.

🛠️ Stack Tecnológico
Frontend: .NET 9 / Blazor WebAssembly.

UI Components: MudBlazor (Material Design).

Data Processing: ExcelDataReader & ClosedXML.

Persistence: Blazored.LocalStorage.

📈 Próximos Pasos (Roadmap)

[ ] Módulo de Clientes VIP: Identificación de compradores recurrentes y LTV (Lifetime Value).

[ ] Soporte Multitipo: Procesamiento automático de reportes de "Liquidaciones Contables".

[ ] Configuración de Alícuotas: Personalización de tasas de IIBB por provincia.
<img width="1920" height="960" alt="image" src="https://github.com/user-attachments/assets/c814a693-4140-4776-af9f-22b8688d79a9" />
<img width="1920" height="961" alt="image" src="https://github.com/user-attachments/assets/1959672f-8355-4f06-8aa7-0f435e0462f0" />
<img width="1920" height="961" alt="image" src="https://github.com/user-attachments/assets/07e09db7-804b-453c-b845-a175b1399b26" />
