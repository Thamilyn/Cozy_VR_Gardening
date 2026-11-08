# Cubrir la semilla con la pala

## Flujo implementado

En `Garden_Moves`, el orden es caminar hasta la mesa, agarrar la semilla, soltarla en la maceta, cubrirla con tierra, regar y usar el calendario. Se conserva el criterio existente de finalización de la explicación de desplazamiento; no se añade una medición nueva de la caminata.

Se reutilizan `GardenShovel`, `GardenSoilSource`, los agarres de manos/mandos de `GardenGrabSetup` y las apariencias de `TomatoPotStages`. La maceta empieza con una capa de tierra visible y la conserva al plantar. La cobertura de la semilla sigue siendo una acción posterior necesaria antes de regar; se distingue del suelo base. El campo antiguo `startsWithSoil` del YAML no pertenece al script actual y no controla este flujo.

1. Soltar correctamente la semilla registra la plantación con `CalendarSystem.PlantSave.soilCovered = false` y activa el paso 4/7 del tutorial. La semilla permanece visible.
2. Con la pala sostenida, hacer una pasada corta sobre la abertura del SoilPot muestra una carga de tierra. Basta un recorrido de 1,5 cm en la zona de recogida; no hay que hundir la hoja en el recipiente ni pulsar otro botón. Una pala vacía no deposita tierra.
3. Llevar la hoja cargada **nivelada** sobre la abertura de la maceta y después inclinarla al menos 55 grados durante 0,35 segundos. El punto de la hoja debe permanecer dentro de la huella de la superficie, entre 2 y 25 cm por encima de ella. No hace falta tocar ni penetrar la maceta, sacudir la pala ni pulsar otro botón.
4. El gesto solo se arma sobre una maceta activa con una semilla registrada y pendiente de cubrir. Entrar ya inclinado, pasar por el lateral, tocar la maceta, usar una pala vacía o dejar caer la pala no completa el paso. Salir de la zona, cambiar de maceta, soltar/cancelar el agarre, desactivar la pala o abrir el menú reinicia el gesto; conserva la carga.
5. Una acción válida consume la carga una sola vez, oculta la semilla bajo la tierra base y registra `soilCovered = true`. La tierra permanece visible antes de plantar, mientras la semilla está descubierta y en las siguientes fases. No se cambian las colisiones ni las referencias de los modelos.
6. El tutorial permite regar después de cubrir. `SeedsController` también rechaza agua y bloquea crecimiento/avance sin cobertura cuando el tutorial está desactivado. El indicador de agua aparece después de cubrir. Los objetivos, drenaje, práctica, calendario, fases y cosecha mantienen sus reglas actuales.

La cobertura pertenece a la sesión, igual que los datos existentes del calendario. Reiniciar/reintentar recarga la escena y vuelve a exigir plantar y cubrir. No se ha añadido la mecánica de cortar el tomate.

## Inspector y grabaciones pendientes

No se encontraron clips ni entradas de catálogo para estas instrucciones en los audios del proyecto o sus guiones/catalogación existentes. **Las tres voces nuevas siguen pendientes de grabación.** Los textos y campos preparados no son grabaciones terminadas.

En `Assets/Resources/GronnyAudio/Tutorial.asset`, desplegar `Cues`, localizar cada `id` y asignar la grabación correspondiente al campo **Clip**. Mantener `Mode = Unknown`, `Demo = None`, `Priority = 10`, `Cooldown = 0`; la prioridad conserva el reconocimiento de cobertura por delante de las instrucciones normales de riego, sin interrumpir una frase en curso. Se puede guardar cada WAV importado en `Assets/Audio/dialogues_cozy_gardening/` con el nombre de su ID.

| ID / nombre sugerido de WAV | Texto en inglés para grabar |
| --- | --- |
| `soil_cover_seed` | Great job! Now use your shovel to cover the seed with soil. Hold the loaded blade level over the pot, then gently tilt it to pour. |
| `soil_scoop_first` | Great job! Scoop up some soil with your shovel. Hold the loaded blade level over the pot, then gently tilt it to cover the seed. |
| `soil_seed_covered` | Perfect! Your seed is all covered up. |

Interpretación: el Gronny cercano, tranquilo y alentador de las tomas existentes, con una pausa natural tras “Great job!” y “Perfect!”. La segunda frase sustituye a la primera si todavía hace falta cargar la pala; no se encadenan ambas instrucciones. La indicación de nivelar e inclinar corresponde al gesto real implementado.

La voz utiliza `GronnyVoicePlayer`: una sola fuente, solicitudes con relevancia, deduplicación y finalización de la frase actual antes de la siguiente. Al cubrir se descartan instrucciones pendientes de plantación/cobertura y se encola el reconocimiento antes del riego. Las instrucciones que ya no corresponden se retiran de la cola. Los clips nulos se omiten y generan un aviso por ID y sesión del tutorial; la mecánica y el panel siguen funcionando sin simular una voz ni bloquear al jugador esperando un audio inexistente.

Los componentes de pala y SoilPot ya están asignados en los prefabs existentes; no se necesitan componentes nuevos ni cambios en la escena. En `Assets/OurAss/GrabablePrfab/Shovel.prefab`, componente **GardenShovel**, el punto de recogida y de carga se ha corregido a `Local Scoop Point = (0, 0.019, 0.195)`. La malla confirma que la hoja ancha está en Z positivo; el punto anterior `(0, 0.05, -0.025)` quedaba sobre el mango, 22 cm detrás. Se conserva `Soil Load Size = (0.075, 0.035, 0.09)`. Los demás valores predeterminados son:

- `Local Blade Up = (0, 1, 0)`: normal de la cara que transporta tierra. Verificar visualmente que apunta fuera de la hoja del modelo.
- `Pour Tilt Degrees = 55`: inclinación necesaria desde la cara nivelada.
- `Pour Hold Seconds = 0.35`: tiempo de inclinación continua sobre la abertura.

Comprobar en el Inspector que la maceta conserva su `GardenSoil`/superficie de plantación y el `TomatoPotStages` ya existente, y que `Ground Renderer` / `Seed Renderer` y las apariencias conservan sus referencias. No es necesario asignar un visual de cobertura nuevo. Las referencias existentes se han conservado.

## Revisión realizada y validación pendiente

Revisión estática del flujo de suelta de semillas, recogida y depósito de tierra, estado de sesión, bloqueo de agua/avance, reemplazo de apariencias, indicador y cola de voz. Se revisaron las referencias serializadas existentes de la pala, SoilPot, semilla, maceta y catálogo; se conservaron las escenas, prefabs, archivos `.meta`, Unity 6000.3.23f1 y las dependencias actuales.

Se ampliaron las comprobaciones opcionales de `GardenCalendarVerification.Verify` para cubrir rechazo en maceta sin semilla, agua/crecimiento bloqueados antes de cubrir y finalización única. Las verificaciones existentes de apariencias y runtime preparan la cobertura antes de revisar crecimiento/cosecha. La comprobación focalizada VerifyScoopRuntime usa eventos Meta sintéticos sobre la pala real.

Tras la autorización del usuario se recompiló en Unity 6000.3.23f1, se ejecutaron las comprobaciones focalizadas del arranque Meta y del prefab SoilPot, y se probó en Play el flujo de recoger, cubrir y empezar el riego con eventos Meta sintéticos sobre los objetos reales de Garden_Moves. Todos esos checks pasaron. No se generó APK ni se probó el dispositivo Quest 3. Los resultados actuales están en Docs/Validation/ScoopSurfaceVerification.txt y Docs/Validation/ScoopRuntimeVerification.txt; los demás informes antiguos no son resultados de esta modificación.

Pendiente, tras autorizar la validación en Unity y después con manos y mandos en Quest 3:

1. Verificar en Quest que la tierra base se ve al inicio y al plantar, la semilla desaparece al cubrir y la tierra permanece visible en todas las fases. La compilación y el flujo hasta el primer riego ya se comprobaron en Unity con eventos sintéticos.
2. Confirmar la alineación de carga y normal de la hoja, alcance cómodo desde la mesa, lectura del panel y margen de altura de vertido. Probar sin levantar el brazo por encima del hombro ni hacer movimientos bruscos.
3. Rechazar pala vacía, maceta vacía, lateral/fondo, entrada ya inclinada, contacto fugaz y depósito repetido. Con la pala nivelada, acercarse sobre la abertura, inclinar y mantener brevemente: una sola cobertura y consumo de carga.
4. Interrumpir el gesto saliendo de la zona, soltando la pala o pausando. Reanudar requiere volver a nivelar sobre la maceta. Confirmar la recuperación existente de una pala caída y probar pérdida/recuperación de tracking.
5. Intentar agua/avance antes de cubrir, también sin tutorial. Después cubrir y recorrer riego, drenaje, calendario y crecimiento hasta las fases posteriores.
6. Con las grabaciones asignadas, probar la instrucción con pala vacía y ya cargada, realización rápida del paso, pausa durante voz, cambio entre manos/mandos y reconocimiento de cobertura seguido de riego: sin solapamientos ni repeticiones. Repetir sin clips: avisos limitados, panel correcto y flujo utilizable.
7. Reiniciar/reintentar: estado limpio, semilla visible al plantar y cobertura requerida otra vez. Revisar en Android Quest 3 el shader existente que oculta/muestra la tierra.

## Corrección de recogida en SoilPot

La selección automática anterior aceptaba cualquier material cuyo nombre empezara por `Soil`. El FBX existente contiene `SoilPot` (recipiente) antes de `Soil.001` (tierra), por lo que podía utilizar el material y los límites del recipiente para detectar la recogida y mostrar la carga. Ahora reconoce `Soil` y sus sufijos habituales, excluyendo `SoilPot`. La resolución automática ya no guarda el índice encontrado en el campo de configuración del Inspector.

También se ajustó la zona de recogida a la abertura rectangular visible. La elipse anterior excluía las esquinas aunque se viera tierra bajo la hoja. La versión actual añade un radio de hoja de 5 cm y un margen superior de 14 cm, manteniendo el margen inferior de 8 cm, la condición de sostener la pala y el bloqueo del tutorial. El gesto de vertido y la exigencia de una semilla plantada se conservan.

No requiere nuevas asignaciones en el Inspector. Los nuevos ajustes de GardenShovel son Scoop Radius = 0.05 m y Minimum Scoop Travel = 0.015 m. En GardenSoilSource, Scoop Height Above Soil = 0.14 m y Scoop Depth Below Soil = 0.08 m. Se comprueba también el recorrido entre fotogramas. La carga se coloca por encima de la cara de la hoja para que se vea. Soltar, pausar o desactivar reinicia el gesto; se acumula solo el recorrido reciente de hasta 0,8 segundos y se ignoran saltos de más de 25 cm en una muestra.

La conexión CLI se pudo establecer posteriormente. La inspección de la partida y del registro del Editor identificó la causa de la pala inactiva: GardenShovel.Awake llamaba a GardenGrabSetup.ConfigureGrabbable, que consultaba SelectingPointsCount antes de que PointableElement.Awake creara la lista de Meta. La excepción abortaba el arranque de la pala. Se trata la lista todavía nula como estado inicial sin agarre. No se modificó Meta SDK ni su orden de ejecución. La pala redescubre fuentes y macetas al activarse y durante el uso, sin depender únicamente de la caché de Start.

También se reparó, en el mismo archivo .meta, el GUID de 33 caracteres del shader HiddenSoil: Unity lo ignoraba por inválido. Su GUID actual tiene 32 caracteres; no había referencias serializadas al identificador inválido. El shader ya se importa y se resuelve por su ruta Resources.

La carga utiliza Soil Texture Region = (0.02, 0.02, 0.58, 0.96) para muestrear solo la parte marrón de la textura Soil existente, que incluye una zona negra a la derecha. Se aplica mediante MaterialPropertyBlock y no modifica la textura ni el material compartido del recipiente.

Se renderizaron y revisaron las vistas previas Docs/Validation/PotSoilBed.png (maceta al inicio) y Docs/Validation/ShovelSoilPlacement.png (carga real sobre la hoja sin mancha negra).

Validación ejecutada:

- Garden/Verify grab setup before Meta Awake: reproduce la lista Meta todavía nula y comprueba los agarres de manos y mandos.
- Garden/Verify SoilPot scoop surface: material real, centro, esquinas, rechazo de laterales/fondo, escala y pose de escena, alcance sobre el borde, radio de hoja y trayectoria entre fotogramas.
- Docs/Validation/VerifyScoopRuntime.cs, mediante Pipeline eval_file en Play: 21 comprobaciones con eventos Hover, Select, Move y Unselect de Meta sobre la pala real. Una pasada de 2 cm a 12 cm por encima de la tierra carga inmediatamente la pala; una pala sin sostener o un gesto alejado no la carga. Comprueba la tierra base al inicio y después de plantar, el anclaje sobre la parte ancha de la malla metálica, material y posición de la carga, reactivación, vertido temporizado, consumo, cobertura única, semilla oculta, paso 5/7 y aceptación del primer riego. La preparación de la fixture omite únicamente las lecciones previas de desplazamiento y agarre.
- Recompilación final: cero errores y cero advertencias de compilación. El CLI informa de que Pipeline usa nombres antiguos de comandos; no se actualizó el paquete.

Durante la validación hubo cierres nativos de Unity. Las comprobaciones anteriores terminaron correctamente antes del último intento de salir de Play. El usuario ha pedido detener la validación automática y realizará las siguientes pruebas manualmente; no se afirma que el Editor haya quedado estable tras el cierre. La inspección se realizó en el Editor conectado; FFmpeg no estaba disponible y no se extrajeron fotogramas del vídeo adjunto. Queda pendiente comprobar en Meta Quest 3 la sensación con manos/mandos, pérdida de tracking y las grabaciones de voz aún sin asignar.
