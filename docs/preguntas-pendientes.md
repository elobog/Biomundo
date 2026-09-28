# Preguntas pendientes por fase

Recopiladas durante el desarrollo. Se responden con Biomundo/Ignacio a medida que avanza cada fase.

## Fase 1 — Exploración (hallazgos que generan preguntas)

- [ ] **Factoring — ningún campo actual sirve para identificarlo (confirmado con datos reales, 2026-09-28).**
  Se probaron los 5 campos Studio candidatos en `account.move` contra el histórico completo y contra agosto
  2026: `x_studio_factoring`, `x_studio_factoring_1` y `x_studio_factoring_2` están **vacíos en el 100% de los
  13.269 registros**. `x_studio_empresa_factoring` está **seteado en el 100%** (no discrimina nada).
  `x_studio_estado_de_la_cesion` tiene el mismo valor fijo ("En proceso de pago") en ~95% de todos los
  asientos, **incluidas facturas de compra**, donde una cesión de factoring no debería aplicar — es decir,
  no refleja el estado real de ninguna cesión. Preguntas para Biomundo:
  - ¿Cómo identifican hoy, dentro de Odoo, que una factura específica fue cedida a una empresa de factoring?
    ¿Es un campo que no detectamos, una etiqueta, un diario distinto, o se lleva fuera de Odoo (planilla,
    correo, el sistema de la empresa de factoring)?
  - ¿En qué momento se marca (al emitir la factura, al recibir el anticipo, al conciliar el pago final)?
  - Si se lleva fuera de Odoo: ¿nos pueden compartir el criterio y, si existe, un archivo con el historial?
  Sin esta respuesta, el módulo de flujo de caja no puede distinguir una CxC propia de una ya cedida.
- [ ] **Importaciones — cómo se registran hoy.** `stock.landed.cost` (el modelo estándar de Odoo para costos
  de importación) tiene un solo registro en 11 meses de historia. Preguntas:
  - ¿Cómo calculan hoy el costo puesto en bodega de un producto importado (flete, seguro, aduana, agente)?
  - ¿Se hace en una planilla aparte? ¿En otro sistema? ¿Con qué frecuencia?
  - ¿Hay una lista de importaciones en curso en algún lugar (Excel, correo, ERP de otro proveedor)?
- [ ] **DTE / estado del documento tributario** (`x_studio_estado_dte`): ¿qué valores puede tomar? ¿Hay casos
  de facturas rechazadas por el SII que deban excluirse del flujo de caja?
- [ ] **N° OC Cliente** (`x_studio_related_field_8f3_1jispm2o2`): ¿se usa de forma consistente? ¿Sirve para
  conciliar ventas contra pedidos de clientes grandes?
- [ ] Varios campos Studio sin nombre asignado ("Nuevo Texto", "Nuevo Many2One", "Nuevo Campo relacionado...):
  confirmar si están en uso real o son pruebas antiguas a ignorar.
- [ ] Confirmar **vencimiento de la suscripción de Odoo** (aparecía "vence en 3 días" el 2026-09-28) y quién la
  renueva, para no perder acceso a la API a mitad de proyecto.

## Fase 2 — Modelo de datos y sincronización

- [ ] ¿Cuánto histórico cargar? Odoo solo tiene datos desde oct-2025/feb-2026 según el modelo. ¿Hay datos
  anteriores en otro sistema que debamos importar para tener comparativos interanuales?
- [ ] ¿Qué conceptos de caja **no están en Odoo** y hay que agregar manualmente? Ej: sueldos, honorarios,
  IVA/F29, arriendos, dividendos, créditos bancarios. ¿Se registran en otro lugar o los carga alguien a mano
  en la nueva app?
- [ ] ¿La empresa de factoring cobra directo al cliente o Biomundo sigue gestionando la cobranza?
- [ ] Confirmar si hay más de una cuenta bancaria/banco a monitorear y sus monedas.

## Fase 4 — Flujo de caja

- [ ] ¿Qué fuente de tipo de cambio usar para la proyección (no el histórico)? ¿Dólar observado del Banco
  Central, un forward pactado, o un supuesto manual del usuario?
- [ ] ¿Existen líneas de crédito, factoring con cupo, o financiamiento de importaciones (cartas de crédito)
  que deban modelarse como fuente de caja disponible?
- [ ] Días de pago/cobro típicos por tipo de cliente/proveedor, para default de la proyección cuando no hay
  fecha de vencimiento clara.

## Fase 5 — Comercio exterior / importaciones

- [ ] Definido el hallazgo de la Fase 1: ¿de dónde sale el dato real de importaciones? Diseño depende
  totalmente de la respuesta.
- [ ] ¿Incoterm habitual (FOB, CIF, etc.)? ¿Un solo agente de aduana o varios?
- [ ] ¿Se paga anticipo a proveedores extranjeros? ¿Con qué plazo respecto al embarque/recepción?

## Fase 8 — Infraestructura y despliegue

- [ ] Confirmar si `RG-Biomundo` puede crearse en la misma región que el App Service Plan existente en
  APP-AITBP (Azure exige mismo grupo de recursos que el plan para desplegar una Web App sobre un plan
  existente, o al menos misma región).
- [ ] Tier del App Service Plan existente: ¿soporta *Always On* (necesario para Hangfire) y WebSockets
  (necesario para Blazor Server)? ¿Cuánta carga tiene hoy con otras apps?
- [ ] Rol de Ignacio en el proyecto (¿dueño del repo Git, revisor de código, ambos?) y cuándo se crea el
  repositorio.
- [ ] Remitente de correo para invitaciones/recuperación de clave (dominio a autorizar en Azure
  Communication Services Email).
