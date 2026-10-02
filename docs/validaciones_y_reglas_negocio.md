# Documentación de Validaciones y Reglas de Negocio - Control de Mareas

El presente documento detalla exhaustivamente todas las validaciones, chequeos y reglas de negocio aplicadas en el sistema actual durante los procesos de importación de datos (DBF), validación de formularios y auditorías de la información de las mareas.

---

## 1. Marea y Consistencia General

### 1.1 Validaciones de Entidad (Marea)

- **Año INIDEP**: Debe ser un valor válido comprendido entre los años 2000 y 2100.
- **Número INIDEP**: Debe ser mayor a 0.
- **Fechas**: La fecha de inicio es obligatoria. Si se proporciona una fecha de fin, esta no puede ser anterior a la fecha de inicio.
- **Buque**: Es obligatorio seleccionar un buque válido.
- **Mareas Finalizadas**: Si la marea está en un estado auditado y finalizado, todas sus etapas deben tener registrada obligatoriamente una Fecha de Arribo.

### 1.2 Consistencia Base y Archivos Importados

- **Correspondencia de Buque y Marea**: El nombre del buque y el número de marea presentes en los archivos de importación (Capturas `C*`, Muestras `M*`, Submuestras `S*`, Madurez `L*`, Producción `P*`, Tracking `T*`) deben coincidir con los de la marea activa.
  - *Autocorrección*: Si los archivos traen la marea en `0`, el sistema corrige automáticamente asignando el número de marea activo.
- **Rango Temporal General**: Las fechas de los lances, muestras y producción deben estar contenidas dentro del rango definido por las etapas de la marea activa (Fecha Zarpada a Fecha Arribo).
- **Unicidad L***: El archivo de madurez (`L*`) solo admite un registro por lance.

---

## 2. Capturas y Lances (Archivo C* / Edición UI)

### 2.1 Datos Generales y Cronología

- **Número y Fecha**: El número de lance debe ser mayor a 0 y la fecha es obligatoria.
- **Secuencia y Duplicados**: No puede haber lances duplicados (mismo número). Si existe un salto en la secuencia numérica, se emite una advertencia.
- **Solapamientos Cronológicos**: La fecha/hora de inicio de un lance no puede ser anterior a la finalización del lance previo.
- **Duración del Lance**:
  - Si la hora de inicio y fin son idénticas, se emite advertencia.
  - Si el fin es menor al inicio, se asume un cruce de medianoche y se suma 1 día.
  - Duraciones mayores a 12 horas emiten una advertencia.

### 2.2 Geografía y Posicionamiento

- **Rango Operativo**: La latitud inicial debe estar dentro del rango de 30°S a 65°S.
- **Profundidad**: Las profundidades inicial y final deben oscilar entre 0 y 2000 metros.
  - *Alerta de Desvío*: Si la diferencia entre profundidad inicial y final es mayor o igual a 50m **y** representa más del 60% de la profundidad inicial, se emite una advertencia.
- **Velocidad de Arrastre Interna**: Si la distancia entre la latitud/longitud inicial y final del lance indica una velocidad superior a 15 nudos, se emite advertencia.
- **Velocidad de Traslado**: Si la distancia entre el fin de un lance y el inicio del lance siguiente implica una velocidad de navegación superior a 15 nudos, se alerta.

### 2.3 Captura y Especies

- **Catálogo de Especies**: Todas las especies reportadas deben existir en el catálogo actual o histórico. Si el código está vacío o es 0, es un error fatal.
- **Duplicidad de Especies**: Una especie no puede aparecer dos veces en el registro del mismo lance.
- **Lances Vacíos**: Un lance debe reportar especies y la captura total no puede ser cero.
- **Totalizadores**:
  - *Captura Total*: La suma de los kilos de todas las especies debe coincidir con el campo de captura total. De lo contrario, se recalcula automáticamente.
  - *Descarte Total*: La suma de los descartes por especie debe coincidir con el descarte total.
- **Validación de Descarte**:
  - El descarte total nunca puede superar a la captura total.
  - *Heurística de Unidad*: Si los descartes se mezclan entre kilogramos y porcentajes, se aplica un consenso de mayoría (> 80%). Si el consenso indica porcentajes, el sistema automáticamente los convierte a kilogramos.

---

## 3. Muestras Biológicas (Archivo M* / Edición UI)

### 3.1 Datos Generales

- **Requisitos**: Especie obligatoria y peso de la muestra mayor a 0.
- **Vinculación con Lance**: La muestra debe referenciar a un lance existente en las capturas. Las fechas de la muestra se autocorrigen para coincidir con la fecha de su lance asociado.
- **Validación Cruzada de Captura**: La especie muestreada debería tener kilos reportados en el registro de captura del lance asociado. Si no, se emite una advertencia.
- **Validación de Peso Total**: El peso total de la muestra no puede exceder los kilos totales capturados para dicha especie en ese lance específico.

### 3.2 Tallas y Frecuencias

- **Tallas**: La última talla reportada no puede ser menor a la primera talla. El intervalo de tallas debe ser mayor a 0. No pueden existir registros de frecuencias con la misma talla duplicada para una misma muestra.
- **Reglas Específicas Langostino**:
  - *Machos*: El número de machos maduros no debe superar el total de machos contabilizados para la talla.
  - *Hembras*: La suma de hembras maduras más hembras impregnadas no debe superar el total de hembras contabilizadas para la talla.

### 3.3 Archivo L*(Datos de Madurez) vs M* (Muestras)

- Si existe un registro `L*` (madurez) para un lance, debe existir obligatoriamente una muestra `M*` de Langostino (estándar).
- Las tallas reportadas en `L*` deben estar presentes en el archivo `M*`.
- **Validación Biológica Cruzada**: Los machos maduros reportados en `L*` deben ser menores o iguales a los machos en `M*`. Las hembras maduras + impregnadas en `L*` deben ser menores o iguales a las hembras registradas en `M*`.

### 3.4 Cálculo de Peso Alométrico

- Si el peso de la muestra viene en 0, el sistema intenta calcularlo (auto-reparación) multiplicando la cantidad de ejemplares de cada talla por la fórmula alométrica ($Peso = A \times Largo^B$).
- El sistema utiliza los parámetros $A$ y $B$ específicos del catálogo priorizando por especie y sexo. Si falla, el registro queda con error.

---

## 4. Submuestras (Archivo S* / Edición UI)

### 4.1 Consistencia e Integridad

- **Unicidad**: El número de ejemplar no debe estar duplicado dentro de una misma especie y lance.
- **Muestra Padre**: Cada submuestra debe estar vinculada obligatoriamente a una muestra de talla "Estándar" (no de descarte) del mismo lance y especie.
  - *Auto-reparación*: Si la fecha difiere de la muestra padre, se autocorrige. Si no existe muestra padre, dependiendo de la configuración, el sistema la auto-reconstruye infiriendo la distribución de tallas.
- **Validación de Pesos (M* vs S*)**: La suma de los pesos individuales de todos los ejemplares de la submuestra no debe exceder el peso total de la muestra padre (con una tolerancia de redondeo de 50 gramos).

### 4.2 Biometría Individual

- **Largos**: El "Largo Estándar" nunca puede ser mayor al "Largo Total".
- **Largo Atípico**: Largos totales que superen los 250 mm generan una advertencia de revisión.

### 4.3 Consistencia Biológica de Tallas (S*vs M*)

- Para tallas mayores a 19 mm, se espera que la cantidad de ejemplares extraídos en la submuestra (por sexo) sea el equivalente al **20% (1/5)** de los ejemplares de la muestra total para dicha talla.
- El sistema evalúa esta relación y si hay desvíos emite una advertencia reportando el porcentaje de "Consistencia Biológica" acertada.

---

## 5. Producción (Archivo P* / Edición UI)

### 5.1 Datos Obligatorios y Catálogo

- Se requiere: Fecha, Etapa asociada, Producto, Especie y Kilogramos.
- **Catálogo**: La especie debe ser resoluble en el catálogo del sistema (por nombre vulgar, científico o código, tolerando omisión de acentos). El producto también debe estar registrado.
- Los kilogramos deben ser > 0 y el factor de conversión >= 0.

### 5.2 Lógica de Producción

- **Regla Factor de Conversión**: Si el nombre del producto incluye la palabra "ENTERO" y el factor de conversión es distinto de 1, se corrige automáticamente a 1.0.
- **Límites Razonables**: Factores de conversión mayores a 10 o kilos producidos mayores a 100,000 generan alertas de datos inusuales.
- **Duplicidad Relativa**: Si para una misma fecha, especie, producto y categoría se carga más de un registro, se alerta para revisión de posible duplicado de carga.

### 5.3 Balance de Masa Diario

Esta es una validación fundamental de auditoría (REQ Producción vs Captura Neta):

- Por cada día de la marea y por cada especie, el sistema reconstruye la "Captura Equivalente" sumando el total de la producción: $(Kilogramos Producidos \times Factor de Conversión)$.
- Esta Captura Reconstruida **se compara contra la Captura Neta Real** del día proveniente de los lances (Total Capturado - Descartes).
- **Error**: Si la captura reconstruida supera a la captura neta disponible (contemplando un % de tolerancia configurable, ej. 2%), se emite un error indicando que se produjo más de lo que físicamente se capturó.
- **Advertencia**: Si hay diferencias (sobrantes o faltantes) que exceden la tolerancia esperada.

---

## 6. Auditoría Geográfica y Tracking Satelital (Archivo T*)

- **Validación de Cruces Geográficos**: Se verifica la coherencia entre las coordenadas iniciales y finales reportadas por el observador en el lance contra el historial de puntos de posicionamiento satelital (Tracking).
- **Algoritmo de Alcanzabilidad**:
  - Para cada inicio/fin de lance, se busca el punto satelital más cercano dentro de una ventana de ± 2 horas.
  - Se calcula la distancia teórica máxima que el buque pudo haber recorrido en ese diferencial de tiempo asumiendo una velocidad crucero máxima (11 nudos).
  - Si la diferencia de distancia física es mayor a 10 millas náuticas a pesar del margen de tiempo, se levanta una alerta por severa discrepancia geográfica.
