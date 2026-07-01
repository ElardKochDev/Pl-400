# P1.docx → separado en módulos (PL-400)

Documento origen: `../P1.docx` (recopilación de contenido de Microsoft Learn, PL-400 –
Power Platform Developer). Se dividió en **27 módulos**, un `.docx` por módulo, agrupados en
**6 partes** con cantidad de módulos equilibrada (5/5/4/3/5/5). Cada archivo conserva **imágenes
y formato** del original e incluye al inicio el título del módulo y su parte.

El original `P1.docx` **no se modificó**.

## Estructura

### Parte 1 — Canvas apps: datos y Power Fx
- `p1_md1.docx` — Técnicas imperativas y variables
- `p1_md2.docx` — Patch y formularios
- `p1_md3.docx` — Columnas de opción (choices) con Power Fx
- `p1_md4.docx` — Datos relacionales
- `p1_md5.docx` — Expresiones de Power Fx

### Parte 2 — Canvas apps: rendimiento y fundamentos de desarrollo
- `p2_md1.docx` — Delegación y rendimiento
- `p2_md2.docx` — Monitor y Application Insights
- `p2_md3.docx` — Introducción a Power Platform para desarrolladores
- `p2_md4.docx` — Herramientas de desarrollo (CLI, soluciones, Package Deployer)
- `p2_md5.docx` — Extender la plataforma (Custom API, configurar vs. código)

### Parte 3 — Extensibilidad de interfaz: client scripting y PCF
- `p3_md1.docx` — Client scripting básico
- `p3_md2.docx` — Client scripting avanzado
- `p3_md3.docx` — Fundamentos de PCF (Power Apps component framework)
- `p3_md4.docx` — Crear un componente de código

### Parte 4 — Extensibilidad de servidor: Dataverse, plug-ins y Web API
- `p4_md1.docx` — Desarrollar con Microsoft Dataverse
- `p4_md2.docx` — Plug-ins
- `p4_md3.docx` — Web API de Dataverse

### Parte 5 — Integración con Azure y conectores
- `p5_md1.docx` — Dataverse y Azure (Service Bus, Azure Functions)
- `p5_md2.docx` — Azure Functions: fundamentos
- `p5_md3.docx` — Azure Functions: desarrollo
- `p5_md4.docx` — Conectores personalizados: portal de creación
- `p5_md5.docx` — Conectores personalizados: creación (VS / APIM / OpenAPI)

### Parte 6 — Conectores avanzados y ALM
- `p6_md1.docx` — Autenticación de conectores
- `p6_md2.docx` — Directivas (policies) de conectores
- `p6_md3.docx` — Extensiones OpenAPI
- `p6_md4.docx` — Gestión de soluciones
- `p6_md5.docx` — Arquitectura de soluciones y ALM

## Correcciones aplicadas
1. **Marcadores de corte eliminados.** Se quitaron del contenido las etiquetas `P1…P9` y
   `MD1…MD9` que habías insertado como guía de separación.
2. **Bloque duplicado eliminado.** En el material de "Herramientas de desarrollo" se repetía dos
   veces, idéntico, el bloque *"Deploy apps with Package Deployer" + "Exercise - Install and use
   developer tools"*. Se conservó una sola copia (queda en `p2_md4.docx`).
3. **Renumeración limpia.** La numeración original de marcadores era irregular (P1 saltaba
   MD3→MD4→MD6→MD9; faltaba P8). Los módulos se renumeraron de forma secuencial dentro de cada
   parte.
4. **Consolidación de partes.** Se pasó de las ~8 partes marcadas a 6 partes temáticas con
   cantidad de módulos equilibrada.

## Verificación
- Los 27 archivos son OOXML válidos (XML bien formado, partes requeridas presentes).
- **Imágenes:** 612 imágenes distribuidas; cada referencia resuelve a su archivo de imagen (0
  referencias rotas). Las 7 imágenes restantes respecto al origen (619) pertenecían al bloque
  duplicado descartado.
- Se abrieron archivos de muestra en Word: abren sin errores y muestran las imágenes.

> Nota: los títulos internos van sin acentos por robustez de codificación; el contenido original
> de Microsoft Learn (en inglés) se mantiene intacto.
