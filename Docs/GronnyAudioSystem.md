# Audio y demostraciones de Gronny

## Integración

`TomatoPrototypeGuide` inicia `GronnyTutorial` en `Garden_Moves`. El catálogo se carga desde `Assets/Resources/GronnyAudio/Tutorial.asset`. Ya contiene referencias a los archivos originales: no se han movido audios, sustituido paquetes ni editado escenas o prefabs. `GronnyAudioSetup` crea el catálogo una sola vez y conserva las ediciones posteriores del Inspector.

La voz (`GronnyVoicePlayer`) gestiona una única fuente de audio y una cola con condiciones de pertinencia. `GronnyTutorial` observa el estado real de `SeedsController`, las acciones del calendario y `FirstPersonLocomotor`; no modifica objetivos de agua, fases, agarres ni locomoción. `GardenControlDemonstration` crea objetos independientes con modelos de Meta y desactiva sus comportamientos de seguimiento, colliders y física.

Referencia revisada: conversación **Crear diálogos para el profesor**, especialmente las últimas correcciones del movimiento con índice doblado y las cuatro instrucciones separadas de calendario. Los diálogos generales y la introducción al tomate conservan sus identificadores separados.

## Audio → evento → entrada → recurso visual

Los identificadores de la tabla son los nombres completos de los WAV sin extensión. Los 19 diálogos están en `Assets/Audio/dialogues_cozy_gardening`; los 8 controles, en `Assets/Audio/controls_`. El inventario recursivo con rutas, duración y SHA-256 está en [GronnyAudioInventory.md](GronnyAudioInventory.md).

Audio | Evento y condición de pertinencia | Entrada | Visual
---|---|---|---
`01_welcome` | Inicio del guía en Garden_Moves, una vez por sesión | Ambas | Sin demostración
`02_tomato_intro` | Inicio, mientras aún no existe un tomate plantado | Ambas | Sin demostración
`03_plant_seed` | Inicio de plantación; se cancela al plantar | Ambas | Los controles de agarre siguientes tienen su propia demo
`04_first_watering` | Primer tomate plantado; fase 0 y agua insuficiente | Ambas | Mano fantasma o Touch Plus, con símbolo de regadera y giro propio
`05_water_target` | Tras primera explicación; fase 0 y agua insuficiente | Ambas | Panel de agua existente
`06_enough_water` | Primer objetivo alcanzado sin exceso; fase 0 | Ambas | Regadera vuelve a posición vertical; prioridad 50
`07_first_advance` | Fase 0 regada, sin exceso y tras detener el vertido | Ambas | Controles de apertura y selección por separado
`08_sprout` | Fase 1 del tomate | Ambas | Brote existente de SeedItem
`09_young_plant` | Fase 2 | Ambas | Planta joven existente
`10_flowering` | Fase 3 | Ambas | Floración existente
`11_green_fruit` | Fase 4 | Ambas | Frutos verdes existentes
`12_harvest` | Fase 5, antes de cosechar | Ambas | Fruto maduro y bandeja existentes
`13_complete` | CalendarSystem.RecordHarvest marca harvested | Ambas | Celebración existente del profesor; las demos terminan
`help_planting` | Liberación de semilla de tomate rechazada, intento de avanzar sin plantar o inactividad | Ambas | Tierra y guía existentes
`help_low_water` | Intento de avanzar con agua insuficiente o inactividad | Ambas | Panel existente
`help_excess_water` | Cruce real del límite de agua o intento de avanzar con exceso | Ambas | Regadera vertical; prioridad 100
`help_drained` | El exceso retenido desaparece, conservándose el exceso aplicado en el registro | Ambas | Panel y charco existentes
`help_calendar` | Inactividad con agua suficiente y vertido detenido | Ambas | Calendario existente
`help_refill` | Regadera sostenida vacía mientras la planta necesita agua | Ambas | Calendario existente y botón Refill water
`controls_move_hands` | Primera plantación pendiente y movimiento aún no aprendido | Manos | Ghost-HandLeft, ThumbUp, HandMidFist, IndexPoint; toque y deslizamiento propios
`controls_move_controllers` | Mismo contexto | Mandos | MetaQuestTouchPlus_Left y clip de joystick stickN
`controls_grab_hands` | Primera plantación pendiente; aún no se ha observado agarre y liberación | Manos | Ghost-HandRight, HandPinch y pose abierta, desplazamiento propio
`controls_grab_controllers` | Mismo contexto | Mandos | MetaQuestTouchPlus_Right y clip grip
`controls_calendar_hands` | Primer cuidado listo para avanzar o regadera vacía; calendario cerrado | Manos | Ghost-HandLeft: alejar, pinza y apertura
`controls_calendar_controllers` | Mismo contexto | Mandos | MetaQuestTouchPlus_Left y clip button02 (Y)
`controls_calendar_select_hands` | Calendario abierto durante el primer cuidado | Manos | Ghost-HandRight, IndexPoint y HandPinch; rayo y botón de ejemplo propios
`controls_calendar_select_controllers` | Mismo contexto | Mandos | MetaQuestTouchPlus_Right y clip trigger; rayo y botón de ejemplo propios

Los avisos y fases se observan cada 0,15 s. Durante el salto temporal se limpia la cola. Cada instrucción se cancela también si su acción ya ocurrió o cambió la fase. Las referencias a la planta se consultan de nuevo, por lo que una petición pendiente no conserva un estado de agua antiguo.

## Entrada y aprendizaje

Se usa `OVRInput.GetActiveController()` y se estabiliza un cambio durante 0,35 s. Sin entrada conocida se espera para explicar controles. Al cambiar a manos o mandos se descartan las instrucciones del modo anterior y se oculta cualquier demostración incompatible. Se solicita solo la variante que corresponde al modo activo; las instrucciones ya iniciadas no se repiten automáticamente para el mismo modo.

Movimiento se aprende al recibir un evento de traslación de `FirstPersonLocomotor`; no se usa el movimiento físico de la cabeza. Agarre se aprende tras observar selección y liberación de semilla o regadera. Apertura se aprende al abrir el calendario y selección al activar un botón de avanzar, rellenar, recuperar o cerrar. Estas marcas son de sesión y por modo de entrada. Las explicaciones completas de controles quedan limitadas a la primera plantación/cuidado.

Los primeros diálogos enseñan acciones completas. Desde planta joven aumenta el tiempo antes de ofrecer ayuda (55 s frente a 30 s), y no se vuelven a explicar controles automáticamente. Los avisos de exceso siguen disponibles. Cada audio marcado `assisted` crea una entrada `Assisted: <id>` en el journal existente **cuando comienza su reproducción**, nunca cuando solo se encola. Una ayuda interrumpida por prioridad no registra dos veces el mismo inicio al reanudarse. Los mensajes de autonomía de fases 2–4 no marcan por sí solos asistencia.

La cosecha limpia las instrucciones, oculta demos, libera las suscripciones y deja sonar únicamente el cierre. Desactivar el guía desactiva el tutorial; salir o reiniciar la escena libera también todos los objetos temporales. No se guarda aprendizaje entre sesiones.

## Recursos de Meta y secuencias propias

Se reutilizan `Packages/com.meta.xr.sdk.interaction/Runtime/Prefabs/HandGrab/Ghost-HandLeft.prefab` y `Ghost-HandRight.prefab`, con los clips de `Runtime/Animations/Hands`: `HandPinch_l_/r_`, `HandMidFist_l_/r_`, `IndexPoint_l_/r_` y `ThumbUp_l_/r_`. Los modelos de mando y los clips de sus botones proceden de `Packages/com.meta.xr.sdk.core/Meshes/MetaQuestTouchPlus/MetaQuestTouchPlus_Left.fbx` y `MetaQuestTouchPlus_Right.fbx`.

Las poses de manos son muestras estáticas: el proyecto interpola sus huesos y añade el desplazamiento de la mano, el toque/deslizamiento del pulgar, la inclinación y enderezado de una regadera esquemática, y el rayo/botón de ejemplo. Esa secuenciación es propia y reside en los scripts y el catálogo dentro de Assets. Los clips Touch Plus mueven los controles del modelo. No se confunde una pose con una secuencia completa.

También se inspeccionaron `HandGhost`, las variantes OpenXR y `MicrogestureSwipe`/`MicrogestureTeleport` de los ejemplos de Meta. Las variantes OpenXR tienen otra jerarquía y los clips de ejemplo no representan todos los pasos del guion corregido; se eligieron los fantasmas OVR con bindings verificados. `HandMidFist_r_` incluye cinco rutas de marcadores de depuración nombrados con `l_`; no son huesos deformantes y la demostración ignora esas rutas inexistentes. No se modificó el paquete.

Las demos conservan los materiales originales de manos fantasma y mandos de Meta y la escala de importación de sus modelos. Se colocan en el mundo al iniciar el audio, aproximadamente a 0,85 m delante y 0,34 m al lado de la vista, ligeramente por debajo. No siguen cada giro de la cabeza. Modelos y rayo usan Ignore Raycast y carecen de colliders activos. Se ocultan al terminar/cancelar la voz, completar la acción, cambiar de fase, cambiar de entrada o abandonar la actividad.

## Configuración

Abre `Assets/Resources/GronnyAudio/Tutorial.asset` en el Inspector:

- `voiceVolume`, `pitch` y `gapSeconds`: volumen global, velocidad y pausa entre voces. Cada cue tiene volumen adicional, prioridad y cooldown.
- `idleHelpSeconds` y `independentHelpSeconds`: demora de ayuda inicial y en fases de autonomía.
- `viewOffset`, `visualScale`, `handEulerAngles`, `controllerEulerAngles` y `blendSeconds`: posición, orientación, tamaño y transición de la demo.
- Cada cue contiene `steps`: segundos **dentro del WAV**, pose, desplazamiento, rotación, giro complementario del pulgar, texto y presión del botón. Los tiempos iniciales son configurables y requieren ajustar la sincronización al escucharlos en Quest. `controllerTarget` identifica el botón o joystick del modelo que se resalta al pulsarlo.
- `assisted`: determina si un audio registra asistencia. El registro no cambia los valores de agua ni el avance de fases.

El audio utiliza una fuente dedicada 2D sin Doppler para mantener una voz comprensible al girar la cabeza. El material de demostración referencia explícitamente URP Unlit para incluir su shader en el reproductor. Los recursos de Meta permanecen referenciados desde el catálogo, sin copias ni modificaciones de sus `.meta`.

## Discrepancias y límites

Se encontraron los 27 WAV previstos, sin nombres faltantes ni variantes adicionales ni contenido binario duplicado (SHA-256). El inventario conserva sus nombres reales. La estructura actual no contiene un registro de dominio persistente: se usa el journal de sesión para indicar asistencia.

En `CalendarVisibilityController`, la apertura actual ocurre al iniciar la pinza tras una liberación estable de 0,1 s; el guion indica pinzar y liberar. La demo muestra el gesto completo, pero no altera el instante real de apertura ni la protección frente a objetos cercanos. Seleccionar botones sigue el PointableCanvas de Meta. La regadera admite también el botón B para rellenar si está sostenida, además del botón Refill water del calendario; la ayuda enseña este último.

La lógica instalada de microgestos confirma índice doblado para entrar, toque del pulgar para activar y deslizamientos para dar pasos; índice extendido es el gesto de salida (`ExitMicroGesture`, OpenIndex). No se usa el guion antiguo de índice extendido para entrar. El archivo WAV está identificado por su nombre; la comprobación física incluye escuchar que la grabación diga el guion corregido.

## Validación

Las comprobaciones automatizadas se ejecutan mediante Unity CLI y el Editor conectado por Pipeline, con informes en `Docs/Validation`. El menú `Garden/Gronny/Verify audio and tomato events in Play` permite repetir la comprobación; modifica únicamente el estado temporal de Play y vuelve a Edit sin guardar la escena.

La comprobación del catálogo pasó: 27 referencias, inventario recursivo, IDs únicos, duración, tiempos ordenados, bindings esqueléticos de las manos y modelos/clips Touch Plus. La comprobación de Play pasó: prioridad, instrucciones obsoletas, cancelación por entrada, objetos sin interacción, registro de asistencia y eventos reales de plantación, exceso, drenaje, fases y cosecha. Para probar eventos de fases invoca la transición directamente: no valida el ciclo visual de día/noche ni simula gestos de un dispositivo. La compilación mediante `unity recompile` terminó con `compilationFailed=false` y cero errores o advertencias. Los resultados están en `GronnyCatalogue.txt` y `GronnyRuntime.txt`; las capturas `Gronny-*.png` permiten revisar las demostraciones.

Durante Play, Meta/OpenXR informó `ErrorFormFactorUnavailable xrGetSystem`, con advertencias de Quest Link y funciones XR no disponibles. Las comprobaciones de audio y eventos completaron PASS, pero este entorno no validó entrada o reproducción estereoscópica de un visor. El Editor quedó fuera de Play, sin errores de compilación y con la escena sin cambios pendientes. `git diff --check` pasó; las advertencias de conversión LF/CRLF son del formato de Git.

Pendiente físicamente en Meta Quest 3: escuchar los 27 audios, ajustar tiempos de pasos, confirmar gesto de activación/salida y joystick, probar pinza/Grip y liberación, protección de apertura junto a objetos, selección por rayos, cambio real de manos a mandos durante la voz, comodidad/escala/oclusiones de las demos, accesibilidad del audio y rendimiento. No se ha generado ni instalado un APK.
