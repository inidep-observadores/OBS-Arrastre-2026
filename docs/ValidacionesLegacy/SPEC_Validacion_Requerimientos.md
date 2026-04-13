# SPEC.md - Aplicación de Validación de Datos de Observadores a Bordo

## 1. Introducción y Contexto

### 1.1 Propósito

Este documento establece los requerimientos funcionales para una nueva aplicación de validación de datos de pesca de Observadores a Bordo, diseñada para reemplazar y mejorar el sistema actual en Visual FoxPro.

### 1.2 Fuentes de Datos

Los datos provienen de 6 archivos DBF (formato dBase III) con la convención de nombres:

```
{Prefijo}{NroMarea}{Año2dígitos}.DBF
```

| Prefijo | Tabla | Descripción | Registros Típicos |
|---------|-------|-------------|-------------------|
| C | CAPTURA | Lances/operaciones de pesca | ~100-200 |
| M | MUESTRAS | Muestras de captura | ~50-150 |
| S | SUBMUES | Submuestras (individuos medidos) | ~200-500 |
| MD | MUESDES | Muestras de descarte | ~30-100 |
| P | PRODUCC | Producción/desembarque | ~80-200 |
| L | LG | Parámetros alométricos | ~40-60 |
| T | TRACKS | Registros satelitales de posición del buque | ~500-2000 |

### 1.3 Estructuras de Datos

#### CAPTURA (116 campos)

- Identificación: BARCO (C20), MAREA (N3), LANCE (N3), FECHA (D8)
- Tiempo: HORA_INIC (N5), HORA_FINAL (N5)
- Geográfica: LAT_INIC/LAT_FINAL (N6), LONG_INIC/LONG_FINAL (N6)
- Ambiental: RUMBO (N3), PROF_INIC/PROF_FINAL (N4), MAR (N2), EDAD_LUNA (N2)
- Meteorología: DIR_VIENTO (N3), VEL_VIENTO (N2), TMP_A_SECO/HUM (N5), TMP_MAR_S/F (N5), PRESION_B (N6)
- Captura: CAPT_TOTAL (N10), DESCARTE (N10)
- Arte: TARTE (N2), NARTE (N2), VEL_ARRAS (N4), AREA_BARR (N8), MALL_ALAS/COPO/SOBRE (N4/3/3)
- Especies (25 máx): ESPECIE_1 a ESPECIE_25 (N10), KG_1 a KG_25 (N9), DESCAR_1 a DESCAR_25 (N9)

#### MUESTRAS (104 campos)

- Identificación: BARCO (C20), FECHA (D8), MAREA (N3), LANCE (N3)
- Especie: ESPECIE (C34), COD_ESPEC (N10), FUENTE (N1)
- Área: AREA (N6,1) - código geográfico
- Tallas: PRIM_TALLA (N3), ULT_TALLA (N3), INTERVALO (N2)
- Peso: PESO_MUES (N7), FACT_POND (N17)
- Distribución: TALLA_1 a TALLA_90 (N15) - frecuencias

#### SUBMUES (21 campos)

- Identificación: BARCO (C20), MAREA (N3), LANCE (N3), FECHA (D8)
- Geográfica: AREA (N6,1), TARTE (N2)
- Especie: ESPECIE (C24), NEJEMPLAR (N3)
- Medidas: LARGO_TOT (N3), LARGO_STA (N3), PESO_TOT (N7), PESO_VAC (N6)
- Sexo/Reproducción: SEXO (N1), ESTADIO (N1), PESO_GON (N6), PESO_HIG (N6), REPLESION (N1)
- Contenido: CONTENIDO (C30)
- Edad: EDAD (N2), R_TOTAL (N4)

#### PRODUCC (9 campos)

- Identificación: BARCO (C20), MAREA (N3), FECHA (D8)
- Producto: ESPECIE (C25), PRODUCTO (C25), CATEGORIA (C3)
- Producción: OPERARIOS (N3), FACTOR (N5), KILOS (N9)

#### LG (74 campos)

- Identificación: BARCO (C20), MAREA (N3), LANCE (N3), FECHA (D8)
- Parámetros: TALLA_1 a TALLA_70 (N9) - frecuencias por talla

#### TRACKS (Nuevo - archivo opcional)

- Identificación: BARCO (C20), FECHA (D8), HORA (N5)
- Geográfica: LAT (N8), LONG (N8)
- Estructura: Registros discontinuos de posición satelital tomados cada 15-60 minutos

---

## 2. Validaciones de Entrada (Directas)

### 2.1 Validaciones de Existencia de Archivos

**REQ-2.1.1**: El sistema debe verificar que todos los archivos requeridos existan antes de procesar.

**Archivos obligatorios**:

- CAPTURA (C*.DBF)
- SUBMUES (S*.DBF)
- PRODUCC (P*.DBF)

**Archivos opcionales**:

- MUESTRAS (M*.DBF)
- MUESDES (MD*.DBF)
- LG (L*.DBF)
- TRACKS (T*.DBF) - archivo de tracks satelitales del buque

**Comportamiento**: Si falta un archivo obligatorio, mostrar error y no permitir procesamiento. Si falta un archivo opcional, mostrar advertencia pero continuar.

---

### 2.2 Validaciones de Estructura de CAPTURA

**REQ-2.2.1**: Número de campos esperados: 116

**REQ-2.2.2**: Verificar presencia de campos obligatorios:

- BARCO, MAREA, LANCE, FECHA
- LAT_INIC, LONG_INIC
- ESPECIE_1 a ESPECIE_25
- KG_1 a KG_25

**REQ-2.2.3**: Verificar rangos de valores:

| Campo | Tipo | Rango Válido | Validación |
|-------|------|---------------|------------|
| LANCE | N3 | 1-999 | Mayor a 0 |
| HORA_INIC/FINAL | N5 | 0.00-23.59 | Formato HH.MM |
| LAT_INIC/FINAL | N6 | -90 a 90 | Grados decimales |
| LONG_INIC/FINAL | N6 | -180 a 180 | Grados decimales |
| RUMBO | N3 | 0-360 | Grados |
| PROF_INIC/FINAL | N4 | 0-2000 | Metros |
| CAPT_TOTAL | N10 | >= 0 | No negativo |
| DESCARTE | N10 | >= 0 | No negativo |

---

### 2.3 Validaciones de Estructura de MUESTRAS

**REQ-2.3.1**: Número de campos esperados: 104

**REQ-2.3.2**: Verificar rangos:

| Campo | Tipo | Rango Válido |
|-------|------|---------------|
| LANCE | N3 | 1-999 |
| COD_ESPEC | N10 | > 0 si existe |
| PRIM_TALLA | N3 | 0-300 |
| ULT_TALLA | N3 | >= PRIM_TALLA |
| INTERVALO | N2 | 1-10 |
| PESO_MUES | N7 | >= 0 |
| FACT_POND | N17 | > 0 |
| TALLA_1 a TALLA_90 | N15 | >= 0 |

---

### 2.4 Validaciones de Estructura de SUBMUES

**REQ-2.4.1**: Número de campos esperados: 21

**REQ-2.4.2**: Verificar rangos:

| Campo | Tipo | Rango Válido | Validación |
|-------|------|---------------|------------|
| LARGO_TOT | N3 | 1-500 | mm |
| LARGO_STA | N3 | 1-500 | mm, <= LARGO_TOT |
| PESO_TOT | N7 | >= 0 | gramos |
| SEXO | N1 | 1-3 | 1=Macho, 2=Hembra, 3=Indet |
| ESTADIO | N1 | 1-6 | 1-5 madurez, 6=Indet |

---

### 2.5 Validaciones de Estructura de PRODUCC

**REQ-2.5.1**: Número de campos esperados: 9

**REQ-2.5.2**: Verificar rangos:

| Campo | Tipo | Rango Válido |
|-------|------|---------------|
| FACTOR | N5 | > 0 |
| KILOS | N9 | >= 0 |
| OPERARIOS | N3 | 1-99 |

**REQ-2.5.3**: Validar PRODUCTO contra lista de valores válidos:

- ENTERO
- FILETE
- DESCABEZADO
- EVISCERADO
- OTRO (con especificación)

---

## 3. Validaciones Cruzadas (Entre Tablas)

### 3.1 Validación de Consistencia de Marea/Barco

**REQ-3.1.1**: Todos los registros de todas las tablas deben tener valores consistentes de BARCO y MAREA.

**Lógica actual (PROBLEMÁTICA)**:

```
Extraer BARCO del primer registro no vacío de CAPTURA
Extraer MAREA del último registro de ARCHIVOS
Aplicar a todos los registros
```

**PROBLEMA**: Esta lógica fuerza valores uniformes incluso cuando hay datos de múltiples mareas.

**SOLUCIÓN PROPUESTA**:

```
Para cada archivo de entrada:
  Verificar que BARCO y MAREA sean consistentes DENTRO del archivo
  Si hay inconsistencias:
    - Opción A: Reportar como error
    - Opción B: Permitir corrección interactiva
    - Opción C: Crear nuevos archivos separados

Verificar consistencia ENTRE archivos:
  - CAPTURA y MUESTRAS del mismo lance deben tener mismo BARCO/MAREA
  - Si difieren, reportar advertencia
```

**REQ-3.1.2**: Implementar verificación de consistencia multi-archivo:

```
Para cada LANCE en CAPTURA:
  Verificar que MUESTRAS, SUBMUES, MUESDES con mismo LANCE
  tengan valores de BARCO y MAREA compatibles
```

---

### 3.2 Validación de Fechas

**REQ-3.2.1**: La fecha de cada registro debe ser válida y estar en formato ISO (YYYY-MM-DD).

**REQ-3.2.2**: Verificar rango de fechas:

- Mínima: 1990-01-01
- Máxima: Fecha actual + 30 días

**REQ-3.2.3**: Problema Y2K detectado:

El sistema actual corrige fechas sumando 100 al año si year < 2000. Esto sugiere que los datos originales tenían fechas como 1925 en lugar de 2025.

**LÓGICA ACTUAL (PROBLEMÁTICA)**:

```
SI year(fecha) < 2000:
  fecha = fecha + 100 años
```

**PROBLEMA**: No distingue entre datos históricos genuinos (pescas de 1975) y errores de entrada (1925 debió ser 2025).

**SOLUCIÓN PROPUESTA**:

```
Para datos nuevos (año entrada < 2020):
  SI year < 1950: considerar como error
  SI 1950 <= year < 2000: interpretar como 20XX (ej: 95 -> 1995)
  SI year >= 2000: usar directamente

Para datos históricos (año entrada >= 2020):
  Mantener año original

Regla general: Solo aplicar corrección Y2K si el año resultante
cae en un rango de años con datos de esta flota/marea
```

**REQ-3.2.4**: Validación de FECHA vs LANCE:

```
Para cada registro en MUESTRAS:
  Buscar CAPTURA con mismo LANCE
  SI MUESTRAS.fecmue <> CAPTURA.fecha:
    REPORTAR: "Fecha de muestra difiere de fecha del lance"
    - Opción de corrección automática o interactiva
```

---

### 3.3 Validación de Área Geográfica

**REQ-3.3.1**: El campo AREA es un código geográfico calculado desde las coordenadas.

**Lógica de Cálculo**:

```
base = INT(lat) * 100 + INT(lon)

lat_min = (lat - INT(lat)) * 100   // minutos
lon_min = (lon - INT(lon)) * 100   // minutos

// Corrección por cuadrante
SI lat_min <= 30 Y lon_min > 30:  area = base + 0.1
SI lat_min <= 30 Y lon_min <= 30: area = base + 0.2
SI lat_min > 30 Y lon_min > 30:   area = base + 0.3
SI lat_min > 30 Y lon_min <= 30:   area = base + 0.4
```

**Ejemplo**: lat=35.25, lon=55.40

- base = 35*100 + 55 = 3555
- lat_min=25, lon_min=40
- area = 3555 + 0.1 = 3555.1

**REQ-3.3.2**: Umbral de Validez:

```
SI area < 3500:
  REPORTAR: "Área inválida (< 3500)"
  RECALCULAR desde coordenadas de CAPTURA
```

El valor 3500 representa aproximadamente la región del Atlántico Southwest (costas de Uruguay/Argentina).

**REQ-3.3.3**: Validación de Consistencia de Área:

```
Para cada registro en SUBMUES o MUESTRAS:
  Buscar CAPTURA con mismo LANCE
  SI (MUESTRAS.area <> CAPTURA.area) O (MUESTRAS.area = 0):
    REPORTAR: "Área inconsistente con coordenadas del lance"
    RECALCULAR automáticamente
```

**REQ-3.3.4**: Verificar rango de coordenadas:

```
SI lat < -60 O lat > 0:
  REPORTAR: "Latitud fuera de rango (-60 a 0)"

SI lon < -70 O lon > -30:
  REPORTAR: "Longitud fuera de rango operativo"
```

---

### 3.4 Validación de Especies

**REQ-3.4.1**: Tabla de Referencia de Especies:

El sistema debe mantener una tabla de especies válidas con estructura:

- CODINIDEP (N10): Código único
- NOMCIENT (C34): Nombre científico
- NOMVULCAS (C24): Nombre vulgar castellano

**REQ-3.4.2**: Validación de Códigos en CAPTURA:

```
Para cada campo ESPECIE_1 a ESPECIE_25 en CAPTURA:
  SI especie <> 0:
    BUSCAR en especie1 WHERE codinidep = especie
    SI no encontrado:
      REPORTAR: "Código especie X no existe en base de datos"
      BUSCAR en especievie (histórico) para sugerencia
```

**REQ-3.4.3**: Validación de Nombres en PRODUCC:

```
Para cada registro en PRODUCC:
  especie = espepro
  BUSCAR en especie1 WHERE nomcient = especie OR nomvulcas = especie
  SI no encontrado:
    REPORTAR: "Especie 'X' de fecha Y no está en la base"
```

**REQ-3.4.4**: Verificar Consistencia de Especie:

```
Para cada LANCE:
  Para cada ESPECIE_i en CAPTURA:
    SI KG_i > 0:
      Verificar que COD_ESPEC de MUESTRAS correspondiente coincida
```

---

### 3.5 Validación de Totales y Consistencia Numérica

**REQ-3.5.1**: Recalcular y verificar CAPT_TOTAL:

```
capt_total_calc = KG_1 + KG_2 + ... + KG_25
SI capt_total_calc <> CAPTURA.capt_total:
  SI diferencia > 0.01 (tolerancia por redondeo):
    REPORTAR: "CAPT_TOTAL inconsistente con suma de especies"
    MOSTRAR: valor calculado vs valor registrado
```

**REQ-3.5.2**: Recalcular DESCARTE:

```
descarte_calc = DESCAR_1 + DESCAR_2 + ... + DESCAR_25
SI abs(descarte_calc - DESCARTE) > 0.01:
  REPORTAR: "DESCARTE inconsistente"
```

**REQ-3.5.3**: Verificar Relación Captura-Descarte:

```
SI DESCARTE > CAPT_TOTAL:
  REPORTAR: "Error: Descarte no puede ser mayor que captura total"

SI DESCARTE / CAPT_TOTAL > 0.95:
  ADVERTENCIA: "Descarte sospechosamente alto (> 95% de captura)"
```

**REQ-3.5.4**: Deducir formato de DESCARTE (kilos o porcentaje):

```
ASUNCIÓN: Todos los registros de descarte están cargados de la misma manera.
La validación de mix (algunos en kilos, otros en porcentaje) se realiza
en el proceso de migración (REQ-10B.3.7).

Para cada LANCE en CAPTURA:
  // Calcular ratio DESCARTE / CAPT_TOTAL
  SI CAPT_TOTAL > 0:
    ratio = DESCARTE / CAPT_TOTAL
  SINO:
    ratio = NULL

  // Clasificar
  SI ratio > 1.0:
    MARCAR lance como "posible_porcentaje"
  SI ratio <= 1.0 Y ratio > 0:
    MARCAR lance como "posible_kilos"
  SI ratio = 0 O ratio = NULL:
    MARCAR lance como "indeterminado"
```

**REQ-3.5.5**: Convertir DESCARTE de porcentaje a kilos:

```
REQUISITO: La validación de mix debe pasar (REQ-10B.3.7 EG-011)
PRECONDICIÓN: CAPT_TOTAL ha sido validado y corregido (REQ-3.5.1)

Si todos los lances son "posible_porcentaje":
  descarte_kilos = (DESCARTE * CAPT_TOTAL) / 100
  REEMPLAZAR DESCARTE con descarte_kilos

  // También convertir DESCAR_1..25
  PARA cada i de 1 a 25:
    SI KG_i > 0:
      DESCAR_i = (DESCAR_i * KG_i) / 100

  REPORTAR: "DESCARTE convertido de {:.1f}% a {:.2f} kg"
```

---

### 3.6 Validación de Producción

**REQ-3.6.1**: Validación de Factor para ENTERO:

```
Para cada registro en PRODUCC:
  SI producto = "ENTERO":
    SI factor <> 1:
      REPORTAR: "Factor incorrecto para producto ENTERO (debe ser 1)"
```

**REQ-3.6.2**: Verificar Factor de Conversión:

```
SI factor <= 0:
  REPORTAR: "Factor debe ser mayor a 0"

SI factor > 10:
  ADVERTENCIA: "Factor muy alto (> 10), verificar"
```

**REQ-3.6.3**: Validar KILOS vs Producción:

```
SI kilos < 0:
  REPORTAR: "Kilos no puede ser negativo"

SI kilos > 100000:
  ADVERTENCIA: "Cantidad muy alta, verificar"
```

---

## 4. Validaciones de Consistencia Lógica

### 4.1 Relación Lance-Muestra-Submuestra

**REQ-4.1.1**: Verificar que todo LANCE en MUESTRAS exista en CAPTURA:

```
Para cada registro en MUESTRAS:
  SI LANCE no existe en CAPTURA:
    REPORTAR: "Lance X en MUESTRAS no existe en CAPTURA"
```

**REQ-4.1.2**: Verificar que cada LANCE en SUBMUES exista en MUESTRAS:

```
Para cada registro en SUBMUES:
  SI LANCE no existe en MUESTRAS:
    REPORTAR: "Lance X en SUBMUES no existe en MUESTRAS"
```

**REQ-4.1.3**: Verificar Números de Ejemplar:

```
Para cada LANCE en SUBMUES:
  Obtener lista de NEJEMPLAR
  SI hay duplicados:
    REPORTAR: "Números de ejemplar duplicados en lance X"
```

---

### 4.2 Validaciones de Rangos Biológicos

**REQ-4.2.1**: Verificar Largo Total Atípico:

```
Para cada registro en SUBMUES:
  SI largo_tot > 250 (mm):
    REPORTAR: "Largo atípico > 250mm para revisión"
    Marcar como necesita revisión
```

**REQ-4.2.2**: Verificar Relación Largo Total vs Estandar:

```
SI largo_sta > largo_tot:
  REPORTAR: "Largo estándar no puede ser mayor que largo total"
```

**REQ-4.2.3**: Verificar Peso/Vida:

```
SI peso_tot = 0 Y largo_tot > 0:
  ADVERTENCIA: "Ejemplar sin peso registrado"

SI peso_tot > 0 Y largo_tot = 0:
  ADVERTENCIA: "Peso registrado sin largo"
```

---

### 4.3 Validaciones de Tallas

**REQ-4.3.1**: Verificar Rango de Tallas en MUESTRAS:

```
SI prim_talla <= 0:
  REPORTAR: "Primera talla inválida"

SI ult_talla <= prim_talla:
  REPORTAR: "Última talla debe ser mayor que primera talla"

SI intervalo <= 0 O intervalo > 10:
  REPORTAR: "Intervalo de tallas inválido"
```

**REQ-4.3.2**: Verificar Distribución de Tallas:

```
suma_tallas = TALLA_1 + ... + TALLA_90
SI suma_tallas > 0 Y peso_mues = 0:
  ADVERTENCIA: "Hay tallas pero no hay peso de muestra"
```

---

## 5. Validaciones Detectadas como Faltantes

### 5.1 Validación de Duplicados de Lance

**FALTANTE**: El sistema actual no verifica si hay lances duplicados en CAPTURA.

**REQUERIMIENTO**:

```
SELECT LANCE, COUNT(*) as cnt
FROM CAPTURA
GROUP BY LANCE
HAVING cnt > 1

REPORTAR: "Lance X aparece N veces en CAPTURA (debería ser único)"
```

---

### 5.2 Validación de Secuencia de Lances

**FALTANTE**: No se verifica si los números de lance son secuenciales o tienen saltos.

**REQUERIMIENTO**:

```
Obtener lista de LANCES ordenados
Verificar que no haya saltos > 1 entre lances consecutivos
REPORTAR: "Salto de lance entre X e Y"
```

---

### 5.3 Validación de Coherencia Temporal

**FALTANTE**: No se verifica que las horas de inicio/fin sean lógicas.

**REQUERIMIENTO**:

```
SI hora_inic > hora_final Y misma fecha:
  ADVERTENCIA: "Hora inicio mayor que hora fin"

SI hora_final - hora_inic > 24:
  ADVERTENCIA: "Duración de lance inusualmente larga (> 24h)"
```

---

### 5.4 Validación de Coordenadas Geográficas

**FALTANTE**: No se verifica si las coordenadas representan un área de pesca válida.

**REQUERIMIENTO**:

```
// Verificar que el área no sea 0,0
SI lat_inic = 0 Y long_inic = 0:
  REPORTAR: "Coordenadas en origen (0,0)"

// Verificar cambio de posición
dist = calcular_distancia(lat_inic, long_inic, lat_final, long_final)
SI dist > 100:  // km
  ADVERTENCIA: "Gran cambio de posición en un lance"

// Verificar rumbo consistencia
SI dist > 0 Y (lat_inic = lat_final Y long_inic = long_final):
  ADVERTENCIA: "Lance sin movimiento pero con duración > 0"
```

---

### 5.5 Validación de Profundidad

**FALTANTE**: No se verifica si las profundidades son físicamente posibles.

**REQUERIMIENTO**:

```
SI prof_inic <= 0 O prof_inic > 2000:
  REPORTAR: "Profundidad inicial fuera de rango"

SI prof_final <= 0 O prof_final > 2000:
  REPORTAR: "Profundidad final fuera de rango"

SI prof_final > prof_inic * 2:
  ADVERTENCIA: "Cambio de profundidad inusualmente grande"
```

---

### 5.6 Validación de Consistencia de Marea

**FALTANTE**: No se verifica que todos los registros pertenezcan a la misma marea.

**REQUERIMIENTO**:

```
Obtener lista de MAREA en CAPTURA
SI hay más de 1 MAREA diferente:
  REPORTAR: "ERROR: Datos de múltiples mareas en un archivo"

Verificar que marea coincida con nombre del archivo:
  SI archivo = C15225.DBF Y marea <> 152:
    REPORTAR: "Marea no corresponde al nombre del archivo"
```

---

### 5.7 Validación de Parámetros Alométricos

**FALTANTE**: LG.DBF contiene parámetros pero no se verifica su validez.

**REQUERIMIENTO**:

```
Para cada registro en LG:
  suma_frecuencias = TALLA_1 + ... + TALLA_70
  SI suma_frecuencias = 0:
    REPORTAR: "Parámetros alométricos sin datos en lance X"

  SI CODIGO = 0:
    REPORTAR: "Código de especie inválido en LG"
```

---

### 5.8 Validación de Ausencia de Datos

**FALTANTE**: No se reportan lances sin ninguna especie registrada.

**REQUERIMIENTO**:

```
Para cada registro en CAPTURA:
  suma_especies = KG_1 + ... + KG_25
  SI suma_especies = 0:
    REPORTAR: "Lance X sin registro de especies"
```

---

### 5.9 Validación de Tracks Satelitales

**NUEVA FUNCIONALIDAD**: Se incorporará un archivo con los tracks (registro satelital de posiciones) del buque.

**REQ-5.9.1**: Archivo de Tracks:

- Formato: DBF con campos de posición y tiempo
- Estructura esperada: BARCO, FECHA, HORA, LAT, LONG
- Los registros no son continuos; se toman en lapsos de 15 minutos a 1 hora en promedio

**REQ-5.9.2**: Verificación de Existencia de Tracks:

```
SI no existe archivo de TRACK para el BARCO de la marea:
  ADVERTENCIA: "No se encontraron registros de track para este buque en el período de la marea"

SI archivo de TRACK existe pero no tiene registros en el período de la marea:
  ADVERTENCIA: "Archivo de track no contiene datos para el período de la marea"
```

**REQ-5.9.3**: Validación de Posición vs Track:

Para cada lance en CAPTURA:

```
  hora_lance = HORA_INIC  (o HORA_FINAL para posición final)
  fecha_lance = FECHA

  // Buscar track más cercano en tiempo
  track_cercano = BUSCAR_TRACK_MAS_CERCANO(fecha_lance, hora_lance)

  SI track_cercano existe:
    diferencia_tiempo = ABS(hora_lance - track_cercano.hora)  // en minutos

    // Tolerancia dinámica basada en la diferencia temporal
    tolerancia_tiempo = MAX(30, diferencia_tiempo * 0.5)  // minutos, mínimo 30

    dist = CALCULAR_DISTANCIA(lat_inic, long_inic, track_cercano.lat, track_cercano.long)

    // Velocidad promedio del buque (nudos)
    velocidad_promedio = VEL_ARRAS  // de CAPTURA, o calcular desde tracks

    // Distancia máxima esperada según tiempo transcurrido
    distancia_maxima = (diferencia_tiempo / 60) * velocidad_promedio * 1.852  // km

    tolerancia_distancia = MAX(5, distancia_maxima * 0.3)  // km, mínimo 5 km

    SI dist > tolerancia_distancia:
      ADVERTENCIA: "Posición de lance inconsistente con track (distancia: X km, tolerancia: Y km)"
  SINO:
    ADVERTENCIA: "No se encontró track cercano para lance X"
```

**REQ-5.9.4**: Parámetros de Tolerancia:

- Tolerancia mínima de tiempo: 30 minutos
- Tolerancia mínima de distancia: 5 km
- Factor de tolerancia dinámica: 30% sobre distancia calculada por velocidad
- La tolerancia aumenta proporcionalmente con la diferencia temporal al track más cercano

---

## 6. Validaciones de Formato y Codificación

### 6.1 Validación de Campo BARCO

**REQ-6.1.1**: Verificar que BARCO no esté vacío:

```
SI EMPTY(barco) O BARCO = "":
  REPORTAR: "Campo BARCO vacío en registro X"
```

**REQ-6.1.2**: Verificar consistencia de BARCO:

```
Obtener lista de BARCO
SI hay más de 1 valor diferente:
  REPORTAR: "Múltiples barcos en datos"
```

---

### 6.2 Validación de Campo SEXO

**REQ-6.2.1**: Verificar valores válidos:

```
SI sexo NOT IN (1, 2, 3):
  REPORTAR: "Valor de sexo inválido (debe ser 1, 2 o 3)"
```

---

### 6.3 Validación de Campo ESTADIO

**REQ-6.3.1**: Verificar valores válidos:

```
SI estadio NOT IN (1, 2, 3, 4, 5, 6):
  REPORTAR: "Estadio de madurez inválido"
```

---

## 7. Validaciones para ML (Muestras de Langostino)

### 7.1 Estructura Esperada

ML es opcional y complementa M con datos adicionales de langostino.

### 7.2 Validaciones Específicas

**REQ-7.2.1**: Si existe ML, verificar compatibilidad con M:

```
SI ML existe:
  SI MUESTRAS no existe:
    REPORTAR: "ML requiere M para complementarse"

  Para cada registro en ML:
    BUSCAR M con mismo LANCE
    SI no encontrado:
      REPORTAR: "Lance X en ML sin correspondencia en M"
```

---

## 8. Validaciones de Integridad Referencial

### 8.1 Catálogo de Especies

**REQ-8.1.1**: Mantener integridad de catálogo:

```
Para cada especie en datos:
  SI especie no existe en especie1 Y no existe en especievie:
    REPORTAR: "Especie no encontrada en catálogo"

Verificar que especie1 y especievie sean excluyentes:
  SI especie existe en ambas:
    REPORTAR: "Especie duplicada en catálogos"
```

---

### 8.2 Índices y Relaciones

**REQ-8.2.1**: Verificar índices esperados:

| Tabla | Campo | Tipo Index |
|-------|-------|------------|
| CAPTURA | LANCE | Único |
| MUESTRAS | LANCE | No único |
| SUBMUES | LANCE | No único |
| PRODUCC | FECHA | No único |

---

## 9. Requerimientos de Reporte

### 9.1 Tipos de Reporte

**REQ-9.1.1**: Generar reportes en formatos:

- Consola (para debugging)
- JSON (para integración)
- CSV (para análisis)
- PDF (para archivado)

**REQ-9.1.2**: Niveles de Severidad:

| Nivel | Color | Significado |
|-------|-------|-------------|
| ERROR | Rojo | Dato incorrecto que debe corregirse |
| WARNING | Amarillo | Dato atípico que requiere revisión |
| INFO | Azul | Información útil para contexto |
| DEBUG | Gris | Detalles técnicos |

---

### 9.2 Estructura de Reporte

**REQ-9.2.1**: Cada validación debe reportar:

```json
{
  "id": "VAL-001",
  "tipo": "ERROR|WARNING|INFO",
  "tabla": "CAPTURA|MUESTRAS|...",
  "campo": "nombre_campo",
  "registro": numero_de_registro,
  "lance": numero_de_lance,
  "mensaje": "Descripción legible",
  "valor_actual": valor,
  "valor_esperado": valor,
  "accion_sugerida": "cómo corregir"
}
```

---

## 10. Requerimientos de Corrección

### 10.1 Modos de Corrección

**REQ-10.1.1**: La aplicación debe soportar:

| Modo | Descripción |
|-------|-------------|
| VALIDATE_ONLY | Solo reportar, no modificar |
| AUTO_CORRECT | Corrección automática con confirmación |
| INTERACTIVE | Corrección con intervención del usuario |
| EXPORT_CORRIGIDO | Generar archivos corregidos para reimportación |

**REQ-10.1.2**: Para cada corrección automática:

- Generar backup antes de modificar
- Registrar qué se cambió y cuándo
- Permitir rollback

---

## 10B. Proceso de Migración DBF → Base de Datos Normalizada

### 10B.1 Flujo de Importación

```
┌─────────────────────────────────────────────────────────────┐
│  1. USUARIO INGRESA DATOS DE MÁREA                         │
│     - Número y año de marea                                │
│     - Nombre del observador                                │
│     - Buque (seleccionado de tabla buques)                 │
│     - Etapas: fecha inicio/fin de cada etapa              │
│     - Ubicación de archivos DBF                             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  2. VALIDACIÓN CRÍTICA (Pre-importación)                  │
│     - Verificar archivos DBF existen                       │
│     - Validar estructura de cada DBF                        │
│     - Validar datos contra los parámetros de marea         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  3. SI HAY ERRORES GRAVES → REPORTAR Y BLOQUEAR            │
│     Usuario debe corregir archivos DBF por fuera           │
│     y volver a intentar                                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│  4. SI VALIDACIÓN PASA → IMPORTAR Y VINCULAR              │
│     - Crear registro en mareas                              │
│     - Crear registros en marea_etapas                      │
│     - Vincular lances a etapas por fecha                   │
│     - Importar resto de datos con FK correctas             │
└─────────────────────────────────────────────────────────────┘
```

### 10B.2 Datos Requeridos del Usuario

**REQ-10B.2.1**: El usuario debe proporcionar:

| Dato | Tipo | Requerido | Descripción |
|------|------|-----------|-------------|
| NumeroMarea | INTEGER | Sí | Número de marea INIDEP |
| AnioMarea | INTEGER | Sí | Año de la marea |
| BuqueID | UUID | Sí | Seleccionado de tabla buques |
| NombreObservador | TEXT | Sí | Nombre del observador a bordo |
| Etapas | ARRAY | Sí | Al menos una etapa |

**REQ-10B.2.2**: Cada etapa debe tener:

| Dato | Tipo | Descripción |
|------|------|-------------|
| FechaZarpada | DATE | Fecha de inicio de la etapa |
| FechaArribo | DATE | Fecha de fin de la etapa (puede ser NULL si está en curso) |
| EspecieObjetivo | UUID | Especie principal (opcional) |

### 10B.3 Validaciones Críticas Pre-importación

**REQ-10B.3.1**: Validación de Marea y Buque en CAPTURA:

```
Obtener lista de (MAREA, BARCO) únicos en CAPTURA
SI cantidad > 1:
  REPORTAR: "ERROR GRAVE: Múltiples valores de MAREA/BARCO en CAPTURA"

SI MAREA del archivo no coincide con NumeroMarea proporcionado:
  REPORTAR: "ERROR GRAVE: MAREA del archivo no corresponde a la marea indicada"

SI BARCO del archivo no coincide con BuqueID proporcionado:
  REPORTAR: "ERROR GRAVE: BARCO del archivo no corresponde al buque indicado"
```

**REQ-10B.3.2**: Validación de Fechas de Lances vs Etapas:

```
Para cada LANCE en CAPTURA:
  fecha_lance = FECHA

  Buscar etapa donde FechaZarpada <= fecha_lance <= FechaArribo

  SI no se encuentra etapa:
    REPORTAR: "ERROR GRAVE: Lance X con fecha Y no pertenece a ninguna etapa"
```

**REQ-10B.3.3**: Validación de Duplicados de Lance:

```
Para cada LANCE en CAPTURA:
  SI existe más de un registro con mismo LANCE:
    REPORTAR: "ERROR GRAVE: Lance duplicado (nro X aparece N veces)"
```

**REQ-10B.3.4**: Validación de Integridad MUESTRAS → CAPTURA:

```
Para cada registro en MUESTRAS:
  SI LANCE no existe en CAPTURA:
    REPORTAR: "ERROR GRAVE: Muestra referencing lance inexistente"
```

**REQ-10B.3.5**: Validación de Integridad SUBMUES → MUESTRAS:

```
Para cada registro en SUBMUES:
  SI LANCE no existe en MUESTRAS:
    REPORTAR: "ERROR GRAVE: Submuestra referencing muestra inexistente"
```

**REQ-10B.3.6**: Validación de Producción vs Fechas de Etapas:

```
Para cada registro en PRODUCC:
  fecha_prod = FECHA

  Buscar etapa donde FechaZarpada <= fecha_prod <= FechaArribo

  SI no se encuentra etapa:
    REPORTAR: "ERROR GRAVE: Producción de fecha X no pertenece a ninguna etapa"
```

**REQ-10B.3.7**: Validación de Formato de DESCARTE:

```
ASUNCIÓN: Todos los registros de descarte están cargados de la misma manera
(todos en kilos o todos en porcentaje).

Para cada LANCE en CAPTURA:
  // Calcular indicador: ratio DESCARTE / CAPT_TOTAL
  SI CAPT_TOTAL > 0:
    ratio = DESCARTE / CAPT_TOTAL
  SINO:
    ratio = NULL

  // Clasificar lance según ratio
  SI ratio > 1.0:
    MARCAR lance como "posible_porcentaje"
  SI ratio <= 1.0 Y ratio > 0:
    MARCAR lance como "posible_kilos"
  SI ratio = 0 O ratio = NULL:
    MARCAR lance como "indeterminado"

// Verificar consistencia global
conteo_porcentaje = COUNT(lances donde "posible_porcentaje")
conteo_kilos = COUNT(lances donde "posible_kilos")
conteo_indeterminado = COUNT(lances donde "indeterminado")
total_lances = COUNT(lances)

// Si hay mezcla de tipos → ERROR
SI conteo_porcentaje > 0 Y conteo_kilos > 0:
  REPORTAR: "ERROR GRAVE EG-011: Datos mixtos de descarte (kilos y porcentaje)"
  REPORTAR: "  - {} lances parecen estar en porcentaje" (conteo_porcentaje)
  REPORTAR: "  - {} lances parecen estar en kilos" (conteo_kilos)
  BLOQUEAR importación
```

**REQ-10B.3.8**: Conversión de DESCARTE (si todos son porcentaje):

```
PRECONDICIÓN: EG-011 no se activó (todos los lances son consistentes)
PRECONDICIÓN: CAPT_TOTAL ha sido validado y corregido (REQ-3.5.1)

SI conteo_porcentaje > 0 Y conteo_kilos = 0:
  // Todos los lances están en porcentaje
  MOSTRAR al usuario:
    "Se detectó que TODOS los registros de DESCARTE están en porcentaje."
    "¿Desea convertir a kilos usando CAPT_TOTAL de cada lance?"

  SI usuario confirma:
    PARA cada LANCE marcado "posible_porcentaje":
      DESCARTE = (DESCARTE * CAPT_TOTAL) / 100
      // También convertir DESCAR_1..25
      PARA cada i de 1 a 25:
        SI KG_i > 0:
          DESCAR_i = (DESCAR_i * KG_i) / 100
      REPORTAR: "INFO: Lance {} - DESCARTE convertido de {:.1f}% a {:.2f} kg"
    MOSTRAR: "Conversión completada. {} lances procesados." (conteo_porcentaje)

  SI usuario cancela:
    BLOQUEAR importación
    REPORTAR: "Usuario canceló conversión. Verifique los datos manualmente."
```

### 10B.4 Clasificación de Errores

#### ERRORES GRAVES (Bloquean importación)

| Código | Descripción | Causa Raíz |
|--------|-------------|-------------|
| EG-001 | Múltiples BARCO/MAREA en archivo | Datos de múltiples mareas en un archivo |
| EG-002 | BARCO no coincide con buque seleccionado | Archivo incorrecto o dato mal registrado |
| EG-003 | MAREA no coincide con marea indicada | Archivo de marea equivocada |
| EG-004 | Lance con fecha fuera de rango de etapas | Etapa mal definida o dato mal registrado |
| EG-005 | Lance duplicado en CAPTURA | Error de entrada de datos |
| EG-006 | Muestra referencing lance inexistente | Datos inconsistentes entre tablas |
| EG-007 | Submuestra referencing muestra inexistente | Datos inconsistentes entre tablas |
| EG-008 | Producción con fecha fuera de rango de etapas | Etapa mal definida o dato mal registrado |
| EG-009 | Falta archivo DBF obligatorio | Archivo no encontrado |
| EG-010 | Estructura de DBF inválida | Archivo corrupto o formato incorrecto |
| EG-011 | Datos mixtos de descarte | Algunos en kilos, otros en porcentaje |

#### ERRORES NO GRAVES (Advertencias, permiten importación)

| Código | Descripción | Impacto |
|--------|-------------|---------|
| EN-001 | Largo total > 250mm en submuestra | Dato atípico, verificar |
| EN-002 | Factor incorrecto para producto ENTERO | Error de cálculo potencial |
| EN-003 | Especie no existe en catálogo | FK marcada como NULL |
| EN-004 | CAPT_TOTAL inconsistente con suma de KG | Diferencia en totals |
| EN-005 | Coordenadas en origen (0,0) | Dato faltante |
| EN-006 | Hora inicio > hora fin | Error de registro |
| EN-007 | Descarte > captura total | Error de dato |
| EN-008 | Área < 3500 | Coordenada inválida |

**Nota**: El descarte en porcentaje (sin mezcla) se convierte automáticamente tras confirmación del usuario. Solo EG-011 (mixed) bloquea.

### 10B.5 Proceso de Vinculación

**REQ-10B.5.1**: Vinculación de Lances a Etapas:

```
1. Para cada LANCE en CAPTURA (ordenado por FECHA, HORA_INIC):
   fecha_lance = FECHA
   hora_lance = HORA_INIC

2. Buscar etapa:
   SELECT * FROM marea_etapas
   WHERE marea_id = :marea_id
     AND fecha_zarpada <= :fecha_lance
     AND (fecha_arribo IS NULL OR fecha_arribo >= :fecha_lance)
   ORDER BY fecha_zarpada

3. Si se encuentra etapa:
   INSERT INTO lances (...columns..., marea_etapa_id)
   VALUES (...values..., :etapa_id)

4. Si NO se encuentra etapa:
   REPORTAR: ERROR GRAVE EG-004
```

**REQ-10B.5.2**: Vinculación de Muestras a Lances:

```
1. Para cada MUESTRA en M*.DBF:
   lance_nro = LANCE

2. Buscar lance vinculado:
   SELECT id FROM lances
   WHERE marea_etapa_id = :etapa_id
     AND nro_lance = :lance_nro

3. Si se encuentra:
   INSERT INTO muestras (...columns..., lance_id)
   VALUES (...values..., :lance_id)

4. Si NO se encuentra:
   REPORTAR: ERROR GRAVE EG-006
```

**REQ-10B.5.3**: Vinculación de Submuestras a Muestras:

```
1. Para cada SUBMUES en S*.DBF:
   lance_nro = LANCE

2. Buscar muestra:
   SELECT m.id FROM muestras m
   JOIN lances l ON m.lance_id = l.id
   WHERE l.marea_etapa_id = :etapa_id
     AND l.nro_lance = :lance_nro

3. Si se encuentra:
   INSERT INTO items_submuestras (...columns..., muestra_id)
   VALUES (...values..., :muestra_id)

4. Si NO se encuentra:
   REPORTAR: ERROR GRAVE EG-007
```

### 10B.6 Reporte de Validación Pre-importación

**REQ-10B.6.1**: El sistema debe generar un reporte con:

```json
{
  "marea": {
    "numero": 152,
    "anio": 2025,
    "buque": "ARBUMASA"
  },
  "archivos": {
    "CAPTURA": { "encontrado": true, "registros": 145 },
    "MUESTRAS": { "encontrado": true, "registros": 58 },
    "SUBMUES": { "encontrado": true, "registros": 211 }
  },
  "errores_graves": [
    {
      "codigo": "EG-001",
      "tabla": "CAPTURA",
      "descripcion": "Múltiples BARCO/MAREA en archivo",
      "detalle": "Encontrados: (158, 'ARBUMASA'), (159, 'OTRO_BUQUE')",
      "registros_afectados": [10, 25, 30]
    }
  ],
  "errores_advertencia": [
    {
      "codigo": "EN-001",
      "tabla": "SUBMUES",
      "descripcion": "Largo total atípico",
      "registros_afectados": [5, 12, 88]
    }
  ],
  "validacion_passed": false,
  "mensaje": "Se encontraron errores graves. Corrija los archivos DBF e intente nuevamente."
}
```

**REQ-10B.6.2**: Si `validacion_passed = true`, el reporte incluirá además:

```json
{
  "resumen_importacion": {
    "lances": 145,
    "muestras": 58,
    "submuestras": 211,
    "items_captura": 1850,
    "produccion": 86
  },
  "etapas_creadas": [
    { "id": "uuid-1", "fecha_zarpada": "2025-03-01", "fecha_arribo": "2025-03-15", "lances_vinculados": 145 }
  ]
}
```

---

## 11. Arquitectura Propuesta

### 11.1 Módulos

```
/validacion
  /core
    - validator.js          # Motor principal de validación
    - rules.js               # Definición de reglas
    - reporter.js            # Generación de reportes
  /rules
    - estructura.js        # Validaciones de estructura
    - cruzada.js            # Validaciones cruzadas
    - biologicas.js         # Validaciones biológicas
  /correccion
    - corrector.js          # Lógica de corrección
    - backup.js              # Manejo de backups
  /parsers
    - dbf-parser.js         # Lectura de DBF
    - dbf-writer.js         # Escritura de DBF
  /output
    - json-reporter.js
    - csv-reporter.js
    - pdf-reporter.js

/migracion
  /importer
    - dbf-importer.js       # Importador principal de DBF
    - validator.js           # Validación pre-importación
    - linker.js              # Vinculación de registros
  /dto
    - marea-dto.js          # Datos de entrada de marea
    - etapa-dto.js          # Etapas de marea
  /reporters
    - migration-reporter.js  # Reporte de migración
  /db
    - sqlite-connector.js    # Conexión a SQLite
    - postgres-connector.js  # Conexión a PostgreSQL
    - migrations/            # Scripts de migración de schema
```

### 11.2 Dependencias Externas

- `dbf` - Biblioteca Node.js para lectura/escritura de DBF
- `proj4` - Para cálculos geográficos
- `pdfkit` - Para generación de PDF

---

## 12. Anexos

### 12.1 Glosario

| Término | Definición |
|---------|------------|
| Lance | Una operación de pesca individual |
| Marea | Viaje de pesca completo (puede tener múltiples lances) |
| Área | Código geográfico de 4 dígitos + decimal de cuadrante |
| Factor | Factor de conversión para producto transformado |
| Submuestra | Individuo medido individualmente |

### 12.2 Referencias

- Formato DBF: dBase III Plus
- Coordenadas: Grados decimales, negativos para latitud sur/longitud oeste
- Sistema de área: Codificación custom basada en lat/lon minutos

### 12.3 Bugs Conocidos del Sistema Actual

| Bug | Descripción | Impacto | Solución |
|-----|-------------|---------|----------|
| Y2K-100 | Suma 100 a cualquier año < 2000 | Fechas históricas se alteran | Lógica condicional por rango de años |
| Global-Barco | Aplica primer barco a todos | Pierde información de multi-barco | Verificación por archivo |
| Área-Incondicional | 21arr_mue.PRG sobrescribe siempre | Datos buenos se machacan | Solo sobrescribir si < 3500 |
| Factor-ENTERO | No valida factor=1 para ENTERO | Reportado en e_p pero no corregido automáticamente | Agregar corrección automática |

---

## 13. Criterios de Aceptación

### 13.1 Completitud

- [ ] Todas las validaciones de estructura implementadas
- [ ] Todas las validaciones cruzadas implementadas
- [ ] Todas las validaciones faltantes identificadas implementadas
- [ ] Reportes generados en todos los formatos
- [ ] Migración DBF → SQL implementada

### 13.2 Corrección

- [ ] Los bugs identificados no se replican
- [ ] Las correcciones automáticas son seguras (con backup)
- [ ] Los falsos positivos son minimizados
- [ ] Errores graves bloquean importación correctamente
- [ ] Errores no graves no bloquean importación

### 13.3 Migración

- [ ] Validación pre-importación detecta todos los errores graves
- [ ] Vinculación de lances a etapas funciona correctamente
- [ ] Vinculación de muestras a lances funciona correctamente
- [ ] Vinculación de submuestras a muestras funciona correctamente
- [ ] Reporte de errores es claro y accionable
- [ ] Importación completa cuando no hay errores graves

### 13.4 Performance

- [ ] Procesa archivos de 10,000+ registros en < 30 segundos
- [ ] Uso de memoria < 512MB

### 13.5 Usabilidad

- [ ] Mensajes de error claros y accionables
- [ ] Documentación completa de reglas
- [ ] Opciones de configuración para diferentes escenarios
