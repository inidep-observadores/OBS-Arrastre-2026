# Plan de Implementación: Validación Avanzada de Producción

Este plan detalla la implementación de reglas de validación de negocio para el módulo de producción, integrándolas en el motor de validación existente (`MareaValidationEngine`).

## Objetivos
1. Implementar reglas de consistencia interna para registros de producción.
2. Implementar validaciones cruzadas entre Producción y Captura (Balance de Masa).
3. Asegurar la consistencia temporal entre producción y etapas de marea.

## Cambios Propuestos

### 1. MareaValidationEngine.cs
Potenciar el motor con las reglas críticas extraídas de la documentación legacy:

#### Producción (P*)
- **Factor vs Producto (REQ-3.6.1)**: Error si `Producto == "ENTERO"` y `Factor != 1`.
- **Límites (REQ-3.6.2/3)**: Advertencia si `Factor > 10` o `Kilos > 100000`.
- **Balance de Masa**: Validar que `Suma(Kilos / Factor)` por especie no exceda significativamente la `CapturaTotal` de los lances.

#### Captura (C*)
- **Detección de Descarte (REQ-3.5.4/5)**: Identificar si los descartes están en porcentaje (ratio > 1) y aplicar conversión automática a kilos si es consistente en toda la marea.
- **Totales**: Asegurar que `Suma(Especies) == CapturaTotal` y `Suma(Descartes_Especies) == DescarteTotal`.

#### Muestras y Submuestras (M* / S*)
- **Integridad (REQ-4.1.2)**: Validar que cada submuestra tenga una muestra padre y cada muestra un lance.
- **Peso Alométrico (LARGOPM.PRG)**: Implementar la estimación de `PesoMuestra` usando parámetros A y B cuando el valor sea 0.
- **Geografía (REQ-3.3.2)**: Ajustar el umbral de área válida a **3500** (Atlántico SW).

#### Coherencia Temporal (REQ-10B.3.6)
- Validar que todos los registros (Lances, Producción, Muestras) estén dentro del rango de fechas de las etapas de la marea.

### 2. MareaValidationService.cs
- Actualizar la llamada a `ValidateProduction` para pasar los datos necesarios.

## Verificación Plan
### Pruebas Manuales
1. Cargar un registro de producción con producto "ENTERO" y factor 1.2 -> Verificar que aparezca el error.
2. Cargar registros de producción cuya suma de peso vivo supere la captura del lance -> Verificar advertencia de balance de masa.
3. Verificar que las advertencias aparezcan correctamente en el reporte de auditoría de la marea.

## Definiciones de Negocio
- **Balance de Masa**: 
  - Diferencia > 2%: Advertencia (Warning).
  - Diferencia > 10%: Error Grave (Error).
- **Producto ENTERO**: Se validará de forma flexible (Case-insensitive, buscando la subcadena "ENTERO").
