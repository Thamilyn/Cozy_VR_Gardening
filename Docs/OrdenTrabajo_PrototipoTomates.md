# Orden de trabajo: cerrar el prototipo jugable de cultivo de tomates

**Fecha de referencia:** 1 de octubre de 2026  
**Fecha objetivo de prototipo:** 4 de octubre de 2026  
**Proyecto:** Cozy VR Garden, Unity para Meta Quest 3

## Encargo para el agente

Trabaja en el proyecto existente para que una persona que no conoce el proyecto pueda completar un ciclo breve de cultivo de tomates en `Garden_Moves`: coger una semilla, plantarla, regar, avanzar el tiempo, observar el crecimiento, cosechar un tomate y reiniciar la experiencia.

Antes de editar, inspecciona las instrucciones `AGENTS.md`, la escena, los scripts y los documentos actuales. Sigue los patrones ya usados con Meta XR SDK. Conserva las versiones, paquetes, referencias, GUID y archivos `.meta` existentes. No sustituyas integraciones ni añadas dependencias.

## Prioridad inmediata

1. **Desbloquear la compilación del prototipo.** Comprueba `ProjectSettings/EditorBuildSettings.asset`: actualmente la lista apunta a `Assets/Scenes/SampleScene.unity`. Configura la escena jugable existente `Assets/Scenes/Garden_Moves.unity` como escena de entrada para la compilación del prototipo. Conserva otras escenas solo si la configuración del proyecto las necesita.
2. **Completar y conectar el ciclo del tomate.** Revisa primero lo ya implementado en `SeedsController`, `SeedItem`, `CalendarSystem`, `PlantWaterReceiver` y `WateringCan`. Reutiliza esa lógica y evita duplicarla. El recorrido debe ser:
   - La semilla se puede agarrar y plantar en una maceta válida.
   - La regadera aporta agua a la planta y el usuario recibe feedback visible.
   - El calendario solo permite avanzar una fase cuando se cumplen las condiciones de riego definidas para esa fase; si no, el usuario recibe una pista clara.
   - La tomatera muestra sus fases de crecimiento y llega a una fase madura con al menos un tomate cosechable.
   - El tomate se puede agarrar y colocar en una cesta, caja o bandeja presente en la escena (o en un destino sencillo creado con objetos existentes).
   - Al completar la cosecha, el guía comunica el éxito y ofrece una forma clara de reiniciar para la siguiente persona.
3. **Convertir al guía en instructor de cultivo sostenible.** El guía debe explicar brevemente cada acción, confirmar acciones correctas, orientar al usuario cuando se atasca y enseñar a usar el agua con cuidado. Debe advertir cuánta agua se aplicó en el riego actual, cuánto suma durante el día/fase, cuál es la referencia para la fase y si conviene regar más, esperar o detenerse. Mantén las instrucciones cortas y sincronizadas para no interrumpir una interacción. Prioriza audio si ya existe un sistema y contenido de voz aprovechable; si no, usa texto o señales visuales que encajen con el proyecto y deja anotado el audio pendiente. No introduzcas un sistema de diálogo complejo.
4. **Añadir medición y objetivos de riego por fase.** El sistema debe registrar el volumen que llega a la maceta, mostrarlo en unidades legibles (ml o L) y compararlo con un objetivo configurable por fase. Revisa los requisitos actuales antes de cambiar la lógica: `SeedsController` actualmente indica que el tomate crece según la edad independientemente del agua, mientras que sus umbrales de agua se usan para rábano y lechuga. El ciclo del tomate debe considerar el agua antes de avanzar cada fase y proporcionar feedback si falta o sobra.

### Base hortícola para el modelo de agua

Usa estas referencias para diseñar los objetivos, pero no presentes una cifra de litros por planta como regla universal: el consumo cambia con el tamaño de la planta, la fase, la temperatura, el sustrato, el drenaje y el recipiente.

- Durante germinación y establecimiento, el sustrato debe mantenerse uniformemente húmedo; estos periodos son sensibles a la falta o exceso de agua. Las plántulas necesitan agua a menor profundidad. Evita recomendar suelo encharcado.
- Durante crecimiento, floración, cuajado y desarrollo del fruto, mantén humedad regular y suficiente. Para tomates en suelo de jardín, una referencia general es aproximadamente 25 mm por semana; otras guías dan un rango de 25–50 mm por semana según condiciones. La demanda crece con la planta y el calor. La irregularidad puede causar caída de flores, rajado y pudrición apical.
- Cuando el fruto alcanza tamaño maduro y empieza a madurar, algunas guías permiten reducir ligeramente el agua. No enseñes a secar la planta ni a someterla a estrés hídrico.
- Prioriza riego dirigido al suelo/raíces y evita mojar follaje sin necesidad; recomienda esperar si la cantidad objetivo ya se alcanzó y el sustrato sigue húmedo.
- Para convertir una profundidad de riego de jardín a referencia de volumen de la maceta, usa `litros = milímetros × área superior de sustrato en m²`. No traslades 25–50 mm/semana directamente como mililitros por planta sin considerar el área y la duración de los días virtuales.
- Las fuentes consultadas no fijan una dosis universal exacta separada por cada fase para una maceta de este proyecto. Por eso, el agente debe calcular y documentar valores jugables a partir del área real de la maceta y del calendario virtual, dejarlos configurables en el Inspector y etiquetarlos como una **simplificación educativa del prototipo**, no como prescripción hortícola universal. Mostrar al jugador la referencia de fase, el agua aplicada y el estado (insuficiente, suficiente/en rango o exceso); justificar cada umbral con las referencias y supuestos.

## Alcance educativo del prototipo

El documento de requisitos del proyecto pide una experiencia guiada que progrese hacia la aplicación independiente de lo aprendido. Mantén el ciclo guiado de tomates como objetivo principal. Si puede añadirse sin poner en riesgo el prototipo del 4 de octubre, incluye al final una tarea independiente muy sencilla: por ejemplo, que el usuario identifique que la planta necesita agua y la riegue sin una instrucción paso a paso. Debe haber feedback correcto/incorrecto. No amplíes el trabajo a simulación de clima, enfermedades, biodiversidad interactiva o múltiples cultivos.

## Orden de trabajo y comprobación

1. Haz un inventario breve de lo que ya funciona y de los huecos concretos. Revisa también `Docs/GardenGrowth.md`, `Docs/GardenHandGrab.md` y `Docs/CalendarHandInput.md`.
2. Corrige primero la escena de entrada de compilación y las referencias que bloqueen `Garden_Moves`.
3. Implementa las piezas ausentes del recorrido completo, con cambios pequeños y siguiendo los componentes existentes.
4. Actualiza la documentación del prototipo para que refleje el comportamiento final y una secuencia manual reproducible.
5. Termina con un resumen de archivos y cambios, decisiones tomadas, limitaciones y una lista de comprobación pendiente para Quest 3.

## Criterios de aceptación

- La compilación del proyecto abre `Garden_Moves` como escena jugable.
- Una persona puede completar el ciclo semilla → plantación → riego → avance de tiempo → tomatera madura → cosecha → éxito → reinicio.
- Avanzar el calendario sin cumplir el riego no hace crecer la planta y produce una indicación comprensible.
- El crecimiento y el feedback de riego son visibles; la cosecha solo está disponible cuando el tomate está maduro.
- El tomate no avanza de fase solo por edad: cada transición respeta los objetivos de agua configurados para la fase.
- El usuario ve el volumen aplicado en el riego actual y el acumulado relevante (día/fase), junto con la referencia de esa fase.
- La guía explica cómo regar el suelo de forma sostenible y da una indicación clara cuando falta agua, se alcanzó el objetivo o se aplicó demasiada.
- La guía da instrucciones breves y confirma el resultado de las acciones principales.
- El reinicio devuelve la experiencia a un estado inicial conocido y permite repetir la prueba sin borrar manualmente datos o archivos.
- La interacción sigue siendo cómoda para principiantes en Quest 3 y funciona con los métodos de interacción ya presentes en el proyecto.
- La documentación indica los pasos de validación manual y cualquier comprobación que no se haya podido realizar.

## Límites y validación

- No edites escenas, prefabs o assets serializados salvo que haga falta para completar este encargo; mantén esos cambios acotados y conserva referencias y `.meta`.
- No edites `Library`, `Logs`, `Temp` ni otros archivos generados.
- No actualices Unity, paquetes ni configuraciones de Meta SDK.
- No inventes una cifra agronómica exacta por fase: expresa los supuestos del modelo y separa las referencias publicadas de los umbrales simplificados elegidos para esta maceta y este calendario virtual.
- Las instrucciones raíz del proyecto requieren permiso antes de iniciar Unity, compilar, ejecutar pruebas automatizadas, instalar herramientas, usar gestores de paquetes o iniciar servicios externos. Sin ese permiso, no ejecutes esas acciones: realiza revisión estática y entrega pasos exactos para validarlas manualmente.
- No afirmes que el prototipo está probado en Unity o Quest 3 si no se realizó esa validación.
- No sobrescribas cambios de usuario que no pertenezcan a este encargo.

## Referencias de contexto

- Requisitos conceptuales: `Assets/Docs/Conceptual design and requirements specification.md` y el PDF proporcionado por el usuario, `C:/Users/Trole/Documents/Conceptual design and requirements specification.pdf`.
- Guías técnicas existentes: `Docs/GardenGrowth.md`, `Docs/GardenHandGrab.md` y `Docs/CalendarHandInput.md`.
- Referencias hortícolas para las recomendaciones de agua:
  - [Utah State University Extension — Water Recommendations for Vegetables](https://extension.usu.edu/yardandgarden/research/water-recommendations-for-vegetables): tomate, 1–2 pulgadas/semana como rango general; advierte que depende de fase, tamaño, suelo y temperatura.
  - [Utah State University Extension — Irrigation: Tomato, Pepper, Eggplant](https://extension.usu.edu/vegetableguide/tomato-pepper-eggplant/irrigation): riego regular y uniforme; aumentar con el tamaño/calor; ligera reducción al madurar el fruto; sensibilidad a irregularidad.
  - [Oregon State University Extension — Educator's Guide to Vegetable Gardening](https://extension.oregonstate.edu/catalog/em-9032-educators-guide-vegetable-gardening): periodos críticos de germinación, establecimiento tras trasplante, floración y desarrollo del fruto; plántulas a menor profundidad, plantas maduras con riego más profundo y menos frecuente; riego a nivel del suelo para eficiencia.
  - [University of Minnesota Extension — Growing tomatoes in home gardens](https://extension.umn.edu/garden-and-home/yard-and-garden/gardening-in-minnesota/growing-tomatoes): referencia de alrededor de 1 pulgada/semana en suelo de jardín; humedad uniforme y riego profundo.
- Calendario de prototipo compartido: [Planificar prototipo de cultivo](https://chatgpt.com/s/cx_6abed29cae8081918abdaeae7aa4824a). El plan reserva el 2–3 de octubre para pruebas con 2–3 personas y el 4 de octubre para congelar contenido, probar en el visor y guardar una compilación de respaldo y un vídeo corto.
