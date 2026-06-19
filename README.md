# MP Analyzer - Dashboard de Rentabilidad Real

https://ivanpaoloni.github.io/ReportAnalizerWASM/

MP Analyzer es una herramienta de Inteligencia de Negocios (BI) diseñada para vendedores de Mercado Pago en Argentina. Permite transformar los reportes de "Cobros" (comercialmente ricos pero contablemente confusos) en un tablero de control estratégico con márgenes netos reales.

El Problema
Los reportes de Mercado Pago suelen mezclar ingresos por ventas, transferencias propias (DEBIN) y retenciones impositivas sin un desglose claro de rentabilidad por producto. Los comerciantes "venden mucho, pero no saben cuánto ganan" después de comisiones e impuestos.

Características Principales
Procesamiento Local: Los datos se procesan 100% en el navegador (Client-Side) mediante Blazor WebAssembly. Privacidad total: los datos financieros nunca tocan un servidor externo.

Filtro Inteligente de Operaciones: Identifica y separa automáticamente ventas reales (regular_payment) de movimientos de fondos propios (account_fund), evitando duplicaciones de facturación.

Cálculo Dinámico de Impuestos: Deduce matemáticamente las retenciones ocultas (IVA, IIBB, SIRCREB) comparando el Monto Bruto vs. el Monto Recibido Neto.

Análisis de Productos Top: Agrupa ventas por ítem para mostrar cuáles son los productos con mayor margen de ganancia real.

Interfaz Adaptativa: Diseño moderno con MudBlazor, soporte para Modo Oscuro persistente y Onboarding guiado para nuevos usuarios.

Exportación Profesional: Genera reportes en Excel (.xlsx) listos para enviar al contador o para análisis interno de stock.

Módulo de Clientes VIP: Identificación de compradores recurrentes y LTV (Lifetime Value).

Stack Tecnológico
Frontend: .NET 8 (LongTerm) / Blazor WebAssembly.

UI Components: MudBlazor (Material Design).

Data Processing: ExcelDataReader & ClosedXML.

Persistence: Blazored.LocalStorage.

Próximos Pasos (Roadmap)

[ ] Soporte Multitipo: Procesamiento automático de reportes de "Liquidaciones Contables".

[ ] Configuración de Alícuotas: Personalización de tasas de IIBB por provincia.
<img width="1920" height="1440" alt="716shots_so" src="https://github.com/user-attachments/assets/611c12b6-f615-4181-aaf7-a37accfb18b7" />


<img width="1920" height="1440" alt="468shots_so" src="https://github.com/user-attachments/assets/9ce68320-0773-4f3e-ada7-a0bca4e937b3" />


<img width="1920" height="1440" alt="543shots_so" src="https://github.com/user-attachments/assets/c59a8a22-ec73-44d6-a614-9ae729dc261f" />
