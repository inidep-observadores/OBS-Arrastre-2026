# Changelog

Todas las novedades y cambios notables de este proyecto serán documentados en este archivo.

## [Unreleased]

### Fixes
- **exportación**: Se corrigió el uso de tipos de coma flotante (`double`) por `long` al codificar las tallas para evitar que `DotNetDBF` modifique los datos con notación científica o pérdida de ceros.
- **exportación**: Se corrigió la lógica del archivo DBF "L" para que las tallas se coloquen exactamente en la columna correspondiente a su valor (`TALLA_38` para la talla 38) en lugar de hacerlo secuencialmente.
- **exportación**: Se añadieron verificaciones al generador DBF para impedir la creación de archivos vacíos (C, P, M, MD, S, L, X) si la marea carece de registros.
- **tests**: Se actualizaron las pruebas unitarias (`DbfExporterServiceTests`) añadiendo registros de producción simulados para mantener el funcionamiento y comprobar la nueva regla de archivos vacíos.
