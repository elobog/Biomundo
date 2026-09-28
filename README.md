# Biomundo

App de gestión financiera para Biomundo SPA: flujo de caja, comercio exterior/importaciones y proyección
de ventas, sobre datos leídos de Odoo. Stack: .NET 10, Blazor Server, SQL Server, Azure OpenAI.

Ver [docs/plan-desarrollo.md](docs/plan-desarrollo.md) para las fases y decisiones, y
[docs/preguntas-pendientes.md](docs/preguntas-pendientes.md) para dudas abiertas por fase.

## Estructura

```
src/
  Biomundo.Odoo    Cliente de solo lectura hacia la API externa de Odoo (JSON-RPC / JSON-2)
  Biomundo.Data    (fase 2) EF Core + SQL Server: esquemas stg/dw/app
  Biomundo.Sync    (fase 2) Sincronización Odoo → SQL, programada y on-demand
  Biomundo.AI      (fase 7) Asistente con Azure OpenAI
  Biomundo.Web     App Blazor Server
tools/
  Biomundo.Explorer  Consola de diagnóstico de Odoo (solo lectura, no toca datos de negocio)
tests/
  Biomundo.Tests
docs/
  diagnostico-odoo/  Informes generados por Biomundo.Explorer (no versionar los que tengan datos reales)
```

Los binarios (`bin`/`obj`) se compilan fuera de esta carpeta, en `%LOCALAPPDATA%\Biomundo\artifacts`
(ver `Directory.Build.props`), para que OneDrive no intente sincronizarlos.

## Requisitos

- .NET SDK 10
- Acceso a Odoo Online de Biomundo: URL, **nombre de base de datos**, usuario y API key

### Cómo obtener el nombre de la base de datos de Odoo

**Importante:** no se puede inferir de forma confiable desde la URL. Aunque la URL de Biomundo termina en
`.odoo.com` (`kpbchile-biomundo.odoo.com`), el nombre real de la base es distinto
(`kpbchile-biomundo-main-24386933`), porque es una instancia alojada por un partner, no Odoo Online
multi-tenant estándar. Para obtenerlo:

1. Entra a Odoo con tu usuario en el navegador.
2. Presiona `Ctrl+U` (ver código fuente de la página).
3. Busca `"db":"` con `Ctrl+F` — el valor que sigue es el nombre real de la base.

## Configurar credenciales (nunca en el código ni en el repo)

Desde la carpeta del repo:

```bash
dotnet user-secrets set "Odoo:Url" "https://kpbchile-biomundo.odoo.com" --project tools/Biomundo.Explorer
dotnet user-secrets set "Odoo:Database" "kpbchile-biomundo-main-24386933" --project tools/Biomundo.Explorer
dotnet user-secrets set "Odoo:Username" "tu-correo@biomundo.cl" --project tools/Biomundo.Explorer
dotnet user-secrets set "Odoo:ApiKey" "tu-api-key" --project tools/Biomundo.Explorer
```

Esto guarda los valores cifrados en tu perfil de Windows (`%APPDATA%\Microsoft\UserSecrets\`), fuera del
repositorio. Cuando se agregue `Biomundo.Web`/`Biomundo.Sync` con la misma necesidad, se repite el comando
cambiando `--project`.

En Azure (fase 8), estos mismos valores van en **Key Vault**, no en app settings planos.

## Ejecutar el diagnóstico de Odoo

```bash
dotnet run --project tools/Biomundo.Explorer
```

Es de **solo lectura**: usa únicamente `search_read`, `search_count`, `read`, `fields_get` y similares
(ver `Biomundo.Odoo/ReadOnlyGuard.cs`); cualquier otro método se bloquea antes de salir a la red. No lee
datos de negocio en volumen, solo conteos, rangos de fechas y metadatos de campos.

Genera dos archivos en `docs/diagnostico-odoo/`:
- `diagnostico-odoo-<fecha>.md` — para leer y discutir
- `diagnostico-odoo-<fecha>.json` — mismo contenido, para procesar

## Compilar y probar

```bash
dotnet build
dotnet test
```
