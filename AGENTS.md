# Instrucciones para agentes — Cozy_VR_Gardening

Estas instrucciones se aplican a todo el proyecto y a sus subdirectorios.

## Contexto del proyecto

- Cozy_VR_Gardening es un proyecto de Unity para realidad virtual.
- Utiliza Meta SDK y tiene como dispositivo objetivo Meta Quest 3.
- Respeta las versiones de Unity, los paquetes y la configuración existentes. No actualices dependencias ni sustituyas integraciones sin autorización del usuario.
- Sigue los patrones existentes de Meta SDK para las interacciones y funciones de realidad virtual. Considera el rendimiento y la comodidad del usuario en Meta Quest 3.

## Calidad del código

- Prioriza código limpio, legible, sencillo y fácil de mantener.
- Sigue las convenciones y la estructura del código existente, especialmente en los scripts de C#.
- Usa nombres descriptivos, responsabilidades claras y métodos pequeños. Evita duplicaciones, abstracciones innecesarias y cambios ajenos a la tarea.
- Añade comentarios cuando expliquen una decisión o un comportamiento que no resulte evidente; evita repetir lo que ya expresa el código.
- Preserva los archivos `.meta` de Unity y las referencias de los recursos. Evita editar escenas, prefabs o recursos serializados salvo que la tarea lo requiera.
- No edites archivos generados ni carpetas de caché como `Library`, `Logs` o `Temp` para implementar cambios de código.

## Límites de acceso

- No crees, edites, muevas ni elimines archivos fuera del directorio del proyecto `Cozy_VR_Gardening`.
- Mantén dentro del proyecto cualquier archivo auxiliar o temporal que necesites crear.
- No sobrescribas ni reviertas cambios del usuario que no formen parte de la tarea.

## Herramientas y permiso

- No ejecutes herramientas externas sin permiso explícito del usuario, salvo el uso de Unity CLI autorizado en la sección siguiente.
- Solicita permiso antes de iniciar aplicaciones, Unity, compilaciones, pruebas automatizadas, instaladores, gestores de paquetes, scripts externos o servicios externos.
- Antes de solicitar permiso, explica qué herramienta quieres ejecutar, para qué y qué efectos tendrá.
- Puedes utilizar las herramientas integradas del entorno de asistencia para leer y editar archivos dentro del proyecto, siempre que no inicien herramientas externas.
- Si una validación requiere una herramienta externa y no tienes permiso, realiza la revisión estática posible e indica claramente qué comprobaciones quedaron pendientes. No afirmes que se ejecutaron pruebas que no realizaste.

## Unity CLI

- El usuario ha instalado Unity CLI (`unity`) y ha autorizado su uso a partir de ahora. Prioriza esta herramienta para las operaciones de Unity que correspondan a la tarea.
- Puedes consultar ayuda, versiones, estado y diagnósticos mediante el CLI sin volver a pedir permiso por la herramienta. Las acciones que inicien Unity, compilaciones, pruebas, instaladores o servicios externos siguen requiriendo autorización previa; no la solicites de nuevo si ya está concedida para la tarea.
- Referencia oficial: [Unity CLI reference](https://docs.unity.com/en-us/unity-cli/unity-cli-reference). Es experimental: comprueba los comandos y opciones de la versión instalada con `unity --help` y `unity <command> --help`; consulta la versión con `unity version --format json`.
- Ejecuta los comandos desde la raíz de este proyecto. Cuando el comando lo admita, indica explícitamente `--project-path "C:\Users\Trole\Cozy_VR_Gardening"`. Respeta la versión de `ProjectSettings/ProjectVersion.txt`.
- Para controlar un Editor conectado se necesita Unity Pipeline. Comprueba la conexión con `unity status` y descubre las herramientas y sus parámetros con `unity list` o `unity command --help`. No presupongas que Pipeline está instalado ni lo instales o actualices sin autorización.
- Para validar cambios, utiliza según corresponda `unity recompile`, `unity test` o `unity build`, tras comprobar su ayuda y contar con la autorización necesaria. Guarda informes y resultados dentro del proyecto.
- Prefiere `--format json` para interpretar resultados. Revisa el código de salida, los errores y las advertencias antes de afirmar que una operación tuvo éxito.
- La disponibilidad del CLI no autoriza actualizar Unity, paquetes, módulos o el propio CLI, cerrar el Editor descartando trabajo, ni modificar archivos fuera del proyecto.

## Entrega de cambios

- Haz cambios concretos y limitados al objetivo solicitado.
- Resume qué cambió, por qué y cómo se verificó.
- Señala cualquier limitación relevante, especialmente si falta validar el comportamiento en Unity o en Meta Quest 3.
