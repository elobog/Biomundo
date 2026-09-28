# Plan de desarrollo — App Biomundo

## Objetivo

Plataforma para Biomundo SPA (Chile, moneda funcional CLP, importaciones en USD) con tres módulos:
flujo de caja, control de comercio exterior/importaciones y proyección de ventas, más un asistente de IA,
construida sobre los datos contables de su Odoo Online.

## Decisiones tomadas (2026-09-28)

| Tema | Decisión |
|---|---|
| Stack | .NET 10, Blazor Server, EF Core, SQL Server |
| Hosting | App Service Linux existente en la suscripción **APP-AITBP** (tenant melirrepu.com) |
| Recursos nuevos de Azure | En **RG-Biomundo**, creados solo cuando el usuario lo autorice explícitamente |
| Usuarios | Propios de la app (ASP.NET Core Identity), alta solo por invitación del Superadmin, MFA obligatorio. No se usa el tenant de Melirrepu ni el de Biomundo para login |
| Odoo | Odoo 18.0 Enterprise, hosting de partner (no Odoo Online multi-tenant estándar). Acceso por API key |
| IA | Azure OpenAI con function calling sobre consultas predefinidas, no SQL libre |
| Sincronización | Programada + on-demand |
| Repositorio Git | Lo entrega el usuario más adelante; mientras tanto se trabaja en esta carpeta (fuera de la sincronización de OneDrive vía `Directory.Build.props`) |

## Fases

### Fase 0 — Preparación ✅ completa
Estructura de la solución (`Biomundo.Odoo`, `Biomundo.Data`, `Biomundo.Sync`, `Biomundo.AI`, `Biomundo.Web`,
`Biomundo.Explorer`, `Biomundo.Tests`), `Directory.Build.props`, `.gitignore`, `.editorconfig`.

### Fase 1 — Exploración de Odoo ✅ completa
Cliente de solo lectura hacia la API de Odoo (`Biomundo.Odoo`) y consola de diagnóstico
(`Biomundo.Explorer`) que generó un informe real contra la instancia de Biomundo.

**Resultado:** ver el informe más reciente en `docs/diagnostico-odoo/` (no versionado, contiene datos
reales) y los hallazgos resumidos en el historial de la conversación. Puntos clave:
- Odoo 18.0 Enterprise, base `kpbchile-biomundo-main-24386933`, compañía única "Biomundo SPA" en CLP.
- 13.269 asientos contables, 11 meses de historia (feb–sep 2026).
- Biomundo usa **factoring** de facturas (campos Studio dedicados) — impacta el diseño del flujo de caja.
- `stock.landed.cost` (modelo estándar de costos de importación) casi sin uso → **cómo registran hoy las
  importaciones está por confirmar** (ver `docs/preguntas-pendientes.md`).
- Tasas de cambio USD/EUR/UF vs CLP ya están en Odoo, actualizadas a diario.

### Fase 2 — Modelo de datos y sincronización (siguiente)
Esquemas `stg` (espejo de Odoo), `dw` (hechos y dimensiones) y `app` (usuarios, roles, auditoría).
Sincronización incremental por `write_date` con Hangfire (cron diario + botón "Sincronizar ahora").
Requiere: resolver preguntas de factoring e importaciones, y tener una base SQL (local o en Azure).

### Fase 3 — Base de la app web
Login, MFA, invitaciones, recuperación de clave, roles, auditoría, panel de Superadmin.

### Fase 4 — Flujo de caja
Real (bancos, pagos) + proyectado a 13 semanas/12 meses (CxC/CxP por vencimiento, compras comprometidas,
pagos de importaciones, ventas proyectadas), con escenarios de tipo de cambio y alertas de déficit.

### Fase 5 — Comercio exterior / importaciones
Seguimiento de cada importación, costo puesto en bodega, exposición cambiaria, calendario de pagos.
Diseño pendiente de definir según cómo Biomundo registre esto hoy (ver preguntas pendientes).

### Fase 6 — Proyección de ventas
Pronóstico con ML.NET por producto/cliente, ajustable manualmente, alimenta el flujo de caja.

### Fase 7 — Asistente IA
Azure OpenAI con function calling sobre funciones predefinidas de cada módulo.

### Fase 8 — Infraestructura y despliegue
Bicep para `RG-Biomundo` (SQL Database, Azure OpenAI, Key Vault, Communication Services Email), Web App
sobre el plan existente en APP-AITBP con Managed Identity, CI/CD en el repositorio que entregue el usuario.

Ver `docs/preguntas-pendientes.md` para las dudas abiertas de cada fase.
