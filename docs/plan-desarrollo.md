# Plan de desarrollo — Plataforma Biomundo

## Objetivo

**Una sola plataforma** para Biomundo SPA (Chile, moneda funcional CLP, importaciones en USD), organizada en
módulos dentro de una misma app (no apps separadas por función):

- **Control de vendedores** (check-in/check-out GPS vía Teams) — módulo que Biomundo quiere ver primero.
- **Flujo de caja**, **comercio exterior/importaciones** y **proyección de ventas**, construidos sobre los
  datos contables de su Odoo Online.
- **Control de clientes** y **análisis de ventas**.
- Asistente de IA transversal a los módulos.

## Decisiones de arquitectura

| Tema | Decisión |
|---|---|
| Stack | .NET 10, Blazor Server, EF Core, SQL Server |
| Estructura | **Una sola app** con módulos (no microservicios ni apps separadas por función) |
| Repositorio | `github.com/elobog/Biomundo` (único, todos los módulos) |
| Recursos nuevos de Azure | En **RG-Biomundo**, creados solo cuando el usuario lo autorice explícitamente |
| Usuarios | Propios de la app (ASP.NET Core Identity), alta solo por invitación del Superadmin, MFA obligatorio |
| Odoo | Odoo 18.0 Enterprise, hosting de partner (no Odoo Online multi-tenant estándar). Acceso por API key |
| IA | Azure OpenAI con function calling sobre consultas predefinidas, no SQL libre |

### Despliegue en dos etapas (decisión 2026-10-17)

1. **Ahora — pruebas y demo:** todo se construye y se prueba en el **tenant de Melirrepu** (`melirrepu.com`,
   suscripción `APP-AITBP`), reutilizando `RG-Biomundo` y el App Service existente. Esto incluye el registro
   de la app en Entra ID para el SSO de Teams durante las pruebas.
2. **Más adelante — producción:** una vez validado con Biomundo, se despliega lo necesario en el
   **tenant propio de Biomundo**, para que sus usuarios entren con sus licencias Microsoft 365 Business Basic
   (que ya tienen, con Teams activo). **Pendiente de definir:** si Biomundo dispone de una suscripción Azure
   propia para alojar ahí la app en producción, o si Melirrepu sigue alojándola (modelo tipo SaaS) mientras el
   registro de Entra ID y el acceso de usuarios quedan en el tenant de Biomundo. Se resuelve antes de la
   Fase 8 (infraestructura).

## Módulos

### Control de vendedores (prioridad actual)

Check-in/check-out con geolocalización desde una Tab personal de Microsoft Teams. Ver el detalle completo
de la arquitectura, riesgos y fases en la conversación del 2026-10-17 (resumen abajo). Objetivo: que un
vendedor en terreno marque inicio/fin de visita desde el celular, con validación de distancia al cliente.

**Riesgo principal identificado:** es geolocalización de empleados — en Chile aplica el Código del Trabajo
(art. 154, control y fiscalización) y la Ley de Protección de Datos Personales. Antes de desplegar en
producción, Biomundo debe informar por escrito a los vendedores (reglamento interno o anexo de contrato).
El diseño ya ayuda: es registro puntual en check-in/check-out, no tracking continuo.

**Fases de este módulo** (ajustadas respecto al plan original para validar lo más riesgoso primero):

1. **Spike técnico (sin Azure):** Tab de Teams mínima vía túnel de desarrollo, cargando en Teams
   Desktop/Mobile, para confirmar que el iframe de Teams funciona bien con la app.
2. **Spike GPS (sin Azure, sin login):** botón que pide `navigator.geolocation` y muestra lat/lon/accuracy,
   probado en Android e iPhone reales dentro de Teams Mobile. Es la incógnita técnica más importante del
   proyecto — se valida antes de construir el resto.
3. **SSO de Teams (toca Entra ID del tenant de Melirrepu para las pruebas):** login silencioso vía Teams JS
   SDK, registro de app en Entra ID.
4. **Modelo de datos:** `Cliente`, `Visita` (con regla de una visita abierta por vendedor a la vez, zona
   horaria explícita).
5. **Check-in/check-out:** con cola offline simple (localStorage + reintento) para zonas sin señal.
6. **Validación de distancia** (fórmula de Haversine): verde ≤100m, amarillo 100-300m, rojo >300m.
7. **Dashboard de supervisor:** visitas del día/semana, tiempo promedio, visitas fuera de rango.
8. **Extras (según prioridad de Biomundo):** foto de evidencia y firma digital en Azure Blob Storage
   (solo la URL en SQL), Azure Maps para visualizar visitas, notificación al supervisor si una visita
   queda fuera del radio permitido.

### Flujo de caja, comercio exterior, proyección de ventas (sobre datos de Odoo)

#### Fase 0 — Preparación ✅ completa
Estructura de la solución (`Biomundo.Odoo`, `Biomundo.Data`, `Biomundo.Sync`, `Biomundo.AI`, `Biomundo.Web`,
`Biomundo.Explorer`, `Biomundo.Tests`), `Directory.Build.props`, `.gitignore`, `.editorconfig`.

#### Fase 1 — Exploración de Odoo ✅ completa
Cliente de solo lectura hacia la API de Odoo (`Biomundo.Odoo`) y consola de diagnóstico
(`Biomundo.Explorer`) que generó un informe real contra la instancia de Biomundo.

**Resultado:** ver el informe más reciente en `docs/diagnostico-odoo/` (no versionado, contiene datos
reales) y los hallazgos resumidos en el historial de la conversación. Puntos clave:
- Odoo 18.0 Enterprise, base `kpbchile-biomundo-main-24386933`, compañía única "Biomundo SPA" en CLP.
- 13.269 asientos contables, 11 meses de historia (feb–sep 2026).
- Biomundo usa **factoring** de facturas, pero **ningún campo de Odoo identifica de forma confiable qué
  facturas están factorizadas** (verificado con datos reales) — impacta el diseño del flujo de caja.
- `stock.landed.cost` (modelo estándar de costos de importación) casi sin uso → **cómo registran hoy las
  importaciones está por confirmar** (ver `docs/preguntas-pendientes.md`).
- Tasas de cambio USD/EUR/UF vs CLP ya están en Odoo, actualizadas a diario.
- No tienen módulo de remuneraciones/nómina en Odoo (solo ficha de empleados).

#### Fase 2 — Modelo de datos y sincronización
Esquemas `stg` (espejo de Odoo), `dw` (hechos y dimensiones) y `app` (usuarios, roles, auditoría — **puede
reutilizarse para el módulo de control de vendedores**, es la misma base de usuarios de la plataforma).
Sincronización incremental por `write_date` con Hangfire (cron diario + botón "Sincronizar ahora").
Requiere: resolver preguntas de factoring e importaciones, y tener una base SQL (local o en Azure).

#### Fase 3 — Base de la app web
Login, MFA, invitaciones, recuperación de clave, roles, auditoría, panel de Superadmin. **Compartida por
todos los módulos** (incluido control de vendedores).

#### Fase 4 — Flujo de caja
Real (bancos, pagos) + proyectado a 13 semanas/12 meses (CxC/CxP por vencimiento, compras comprometidas,
pagos de importaciones, ventas proyectadas), con escenarios de tipo de cambio y alertas de déficit.

#### Fase 5 — Comercio exterior / importaciones
Seguimiento de cada importación, costo puesto en bodega, exposición cambiaria, calendario de pagos.
Diseño pendiente de definir según cómo Biomundo registre esto hoy (ver preguntas pendientes).

#### Fase 6 — Proyección de ventas
Pronóstico con ML.NET por producto/cliente, ajustable manualmente, alimenta el flujo de caja.

#### Fase 7 — Asistente IA
Azure OpenAI con function calling sobre funciones predefinidas de cada módulo.

#### Fase 8 — Infraestructura y despliegue
Bicep para `RG-Biomundo` (SQL Database, Azure OpenAI, Key Vault, Communication Services Email), Web App
sobre el plan existente en APP-AITBP con Managed Identity, CI/CD en `github.com/elobog/Biomundo`. Incluye
resolver la migración a producción en el tenant propio de Biomundo (ver "Despliegue en dos etapas" arriba).

Ver `docs/preguntas-pendientes.md` para las dudas abiertas de cada fase.
