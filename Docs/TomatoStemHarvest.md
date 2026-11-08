# Cortar el tallo y recoger el tomate con Gronny

## Flujo implementado

Se reutilizan `GardenPruner`, `PrunerHandGrab`, el modelo existente de Pruner y los agarres de Meta. No se trocea el fruto ni se incorpora otra herramienta.

1. La madurez se consulta en el calendario de la semilla: tomate plantado, `soilCovered = true`, `tomatoPhase = 5`, `stage = Mature`, sin cosecha registrada ni transición de crecimiento en curso. El color rojo por sí solo no habilita el corte. `SeedItem` sigue creando `TomatoFruit` únicamente al mostrar la etapa madura y conserva su referencia aunque se separe del modelo.
2. Mientras está unido, el cuerpo permanece cinemático, sin gravedad, y se desactivan `Grabbable`, `GrabInteractable` y `HandGrabInteractable` del fruto. Los bloqueos del tutorial respetan este estado. No se cambia la interacción de las semillas ni el paso previo de cubrirlas con tierra.
3. Pruner debe estar sostenido. La revisión de comodidad usa apertura hasta 0,30 y cierre deliberado desde 0,75, mapeados a las hojas completamente abiertas/cerradas. Se corta cuando la animación ha cerrado al menos un 90 %. El tallo admite un margen de 0,025 m del segmento de las hojas; durante el mismo cierre se conserva una alineación válida hasta 0,18 s y como máximo 1 cm adicional de deriva. No se aceptan puntos de corte dentro de la esfera del fruto. Solo se elige un tomate por cierre; mantener las hojas cerradas, acercarlas ya cerradas, rozar el fruto o acercar una mano no corta. Pausa, cambio de mano y pérdida de seguimiento siguen exigiendo reapertura. Un aro señala la unión y pasa de ámbar a verde cuando las hojas están alineadas. Detalles: `Docs/PrunerCutComfort.md`.
4. El punto de unión se obtiene de la geometría del tallo del modelo existente `RootswithTomatoes.036`: se proyectan sus vértices sobre el eje fruto/tallo y se centra el extremo. Buscar simplemente el vértice más lejano al fruto seleccionaba una hoja lateral del cáliz y se ha corregido. Si falta geometría legible del tallo o la separación de materiales, se informa del error y no se utiliza el centro del fruto como sustituto. El FBX actual `TomatPhasefinal.fbx` ya tiene Read/Write habilitado.
5. Un corte válido marca `IsStemCut`, separa la jerarquía conservando la escala, reinicializa el transformador de Meta y activa gravedad sin añadir un impulso. Habilita los agarres existentes. No marca `IsPickedUp` ni registra cosecha.
6. Un evento `Select` de Meta sobre el fruto separado, maduro y con el jardín activo marca `IsPickedUp`. `Hover`, contacto o liberación no lo hacen. Aquí termina el paso de cortar y recoger, y Gronny felicita al jugador.
7. Se conserva la entrega posterior en la bandeja para cerrar la sesión de jardinería y registrar `CalendarSystem.RecordHarvest`. Requiere corte, recogida previa y fruto liberado dentro del volumen. Una caída directa desde la planta a la bandeja sin recogerlo no registra cosecha. Tampoco se acepta otra semilla como origen. El almacenamiento y el estado `harvested` impiden registros y celebraciones repetidos.

## Manos, mandos y recuperación

- Mandos: sostener Pruner con Grip; abrir soltando Trigger y cerrar apretándolo junto al tallo.
- Manos: tomar Pruner con la palma, abrir los cuatro dedos principales sin soltar la herramienta y cerrar la mano para cortar. Se conserva la suelta existente con mano abierta y palma hacia la mesa.
- Después, recoger el tomate separado mediante pinch o Grip, usando el sistema existente de Meta.
- Se conservan los plazos de recuperación de 8 segundos después del corte y 2 segundos después de soltar un fruto ya recogido. El contador se suspende mientras se sostiene o está el menú abierto.
- La recuperación devuelve el fruto cerca de su posición original, desplazado horizontalmente 9,75 cm hacia fuera de la planta. Espera cinemático y sin gravedad para quedar accesible; el siguiente agarre y liberación restauran su física mediante Meta. No se vuelve a unir a la planta, no se restablecen los indicadores de corte/recogida ni se repiten diálogos. Conserva su escala independiente aunque se haya movido la maceta.

## Audio de la cosecha

La revisión de archivos y transcripciones encontró `12_harvest.wav` y `12_harvest_accessible.wav`, pero sus instrucciones piden recoger directamente y entregar en la bandeja. No describen el corte. Se conservan los archivos y sus referencias anteriores; el tutorial de madurez utiliza los tres IDs nuevos siguientes. `13_complete` se conserva para el cierre posterior a la entrega.

**Las tres grabaciones ya están generadas y asignadas.** Se utilizó el generador local existente KittenTTS v2, voz Kiki, con autorización del usuario. Son WAV mono de 24 kHz: 12,04 s, 3,24 s y 3,08 s respectivamente. La explicación inicial incluye el gesto de abrir y cerrar las tijeras. Véase `Docs/GronnyHarvestVoice.md` para los informes de señal, transcripción e integración.

| ID / nombre del WAV | Texto hablado en inglés | Momento |
| --- | --- | --- |
| `harvest_cut_stem` | Look! Your tomato is ripe! Pick up the pruning shears. Open the blades around the stem connecting your tomato to the plant. Then close your hand, or squeeze the trigger, to snip the stem. | Madurez real, antes del corte. |
| `harvest_stem_cut` | Nicely done! Now you can pick up your tomato. | Primer corte válido del tallo. |
| `harvest_picked` | Well done! You have harvested your tomato! | Primer agarre del tomate separado. |

La herramienta real es **Pruner**, unas *pruning shears*. El panel usa el nombre del objeto y la grabación su nombre natural en inglés. Mantener la interpretación cálida, tranquila y alentadora de Gronny, con pausas naturales tras “Look!”, “Nicely done!” y “Well done!”.

Se usa la misma instancia de `GronnyVoicePlayer`, su fuente, deduplicación, relevancia, pausa y separación entre frases. La instrucción pendiente de cortar se descarta si ya se cortó. Los reconocimientos de corte y recogida se conservan en orden incluso si se recoge y entrega muy deprisa; no interrumpen la frase en curso. Los IDs nuevos tienen igual prioridad 10 y modo común a manos/mandos, por lo que cambiar de entrada no vuelve a anunciarlos.

Sin clips, se emite un aviso por ID y sesión; el panel muestra la acción real y no se bloquea el avance esperando una grabación. No se necesita modificar `GronnyVoicePlayer`.

## Inspector y referencias

No es necesario editar la escena ni añadir manualmente `TomatoFruit` al modelo. Las referencias existentes se conservan.

Los pasos 1 y 2 ya están realizados para las voces generadas; solo harían falta para sustituirlas por otras grabaciones.

1. Importar las tres grabaciones en `Assets/Audio/dialogues_cozy_gardening/`, preferiblemente con los nombres de ID anteriores.
2. En `Assets/Resources/GronnyAudio/Tutorial.asset`, las tres entradas de **Cues** ya tienen **Clip**. Conservar `Mode = Unknown`, `Priority = 10`, `Volume = 1` y `Cooldown = 0`. La instrucción `harvest_cut_stem` usa `Demo = CutStem` con cuatro pasos; `harvest_stem_cut` usa `Demo = PickTomato` con tres pasos; `harvest_picked` conserva `Demo = None`. `Assisted` está activado solo para la instrucción de corte. Las transcripciones y los textos breves ya están preparados. La referencia **Pruning Shears Visual** ya apunta al prefab existente de Pruner. Detalles: `Docs/GronnyHarvestAnimations.md`.
3. `GronnyTutorial.Catalogue` debe apuntar a ese catálogo; si está vacío, el componente ya lo carga desde `Resources/GronnyAudio/Tutorial`. No añadir otra fuente de voz.
4. En el prefab existente `Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/Pruner.prefab`, conservar **GardenPruner** (`Closing Angle = 22`, `Animation Duration = 0.12`, `Cutting Radius = 0.025`, `Squeeze To Cut = 0.75`, `Squeeze To Open = 0.3`, `Alignment Grace Seconds = 0.18`) y **PrunerHandGrab**. Los ajustes ya están aplicados. `BottomPart` y `TopPart` se resuelven por sus nombres. `Release Table` es opcional: su resolución existente reconoce la mesa actual. No hay un nuevo evento de contacto que conectar al fruto.
5. Conservar las apariencias de **TomatoPotStages**, su superficie/tierra y el modelo maduro con tallo. El punto de corte se calcula al inicializar el fruto, sin una referencia nueva de Inspector. No convertir el fruto verde en un objeto recogible.
6. Conservar **TomatoHarvestBasket.Collection Volume**, el volumen de bandeja existente y su referencia al jardinero (o su búsqueda automática). Los agarres y Rigidbody del fruto se preparan en ejecución mediante `GardenGrabSetup`.

Las grabaciones de tierra que ya estaban pendientes siguen fuera de esta entrega; no se sustituyen ni se presentan como completadas.

## Revisión y validación

Realizado en esta modificación: lectura de AGENTS.md, revisión estática del crecimiento/calendario, cobertura de tierra, modelos/materiales, corte y gestos de Pruner, agarres Meta, recuperación, bandeja, tutorial, cola de voz, catálogo y archivos de audio. Se contrastaron las llamadas relevantes con el código del Meta SDK instalado. Se revisaron referencias serializadas de Pruner y del modelo final, además de las nuevas entradas del catálogo.

Se adaptó la comprobación opcional existente `Garden/Verify ripe tomato harvest` (`TomatoRuntimeVerification`) para preparar el corte con Pruner antes del agarre. Incluye bloqueos de manos/mandos, rechazo de falta de herramienta, proximidad sin cierre, hojas ya cerradas, estado verde, ausencia de tierra, cierre parcial y corte repetido; también distingue corte, recogida y entrega y revisa la recuperación sin reconexión. Inyecta muestras de cierre en el método existente y eventos Meta sintéticos; no representa sensores físicos. **No se ha ejecutado.** Los scripts e informes históricos de `Docs/Validation` no son resultados de este cambio.

No se ha iniciado Unity, recompilado, ejecutado Play, pruebas automatizadas ni una compilación APK: requieren permiso previo según AGENTS.md. No se cambiaron versiones, paquetes, escenas, prefabs ni archivos `.meta`.

Pendiente en Unity, una vez autorizada la validación:

- Compilar y recorrer las lecciones anteriores, incluida añadir tierra, hasta madurez. Revisar bloqueo de agarres en el fruto unido, desprendimiento/escala, recogida y entrega única.
- Ejecutar la comprobación focalizada adaptada en una sesión preparada para jugar; comprobar también el flujo real del tutorial, que esa comprobación aislada no sustituye.
- Intentar cortar fruto verde, tocar el fruto con mano/hojas, mover hojas cerradas, cortar lejos del tallo y mantener el cierre. Solo el cierre válido sobre el tallo maduro debe avanzar.
- Cortar sin recoger, dejar caer en la bandeja sin agarre, recoger y soltar fuera, esperar recuperación, mover la maceta, repetir agarres y entregar: sin corte adicional, reinicio de progreso ni recompensa repetida.
- Pausar, cambiar mano/mando, perder seguimiento y reiniciar la sesión: reapertura requerida y estado limpio al reiniciar.
- Con los WAV asignados, probar voces largas y acciones rápidas, entrega inmediata y pausa durante voz: un solo reconocimiento de cada acción, orden correcto y sin solapamientos. Sin WAV, comprobar panel y avisos limitados.

Pendiente en **Meta Quest 3**: alineación visual del punto del tallo con las hojas, tolerancia de 1,5 cm, alcance cómodo, selección real con ambas manos/mandos, apertura de mano sin soltar Pruner, colisiones/caída, recuperación con tiempos reales, lectura del panel, voces y rendimiento. La revisión estática no valida esas sensaciones ni la física en el dispositivo.

## Corrección después de la grabación del 8 de noviembre

Tras autorización explícita para VLC y validación en Unity, se extrajeron fotogramas del vídeo del usuario y se inspeccionó el modelo real en Play pausado. La madurez y el componente de cosecha estaban disponibles. El punto elegido por distancia radial estaba en una hoja del cáliz, a un lado y por debajo del fruto; las vistas aisladas con un marcador confirmaron el fallo. Las pruebas anteriores alineaban la herramienta con ese mismo punto calculado, por lo que no detectaban el error de geometría.

`TryGetAttachmentPoint` ahora usa el eje desde el centro del fruto hacia el conjunto verde y promedia el extremo del tallo proyectado sobre él. El marcador se verificó en el centro del extremo visible del tallo, desde dos vistas. No se ampliaron la tolerancia ni las zonas válidas al fruto. No requiere nuevas asignaciones de Inspector.

La comprobación existente añade una condición independiente sobre los límites medidos del extremo real de `RootswithTomatoes.036`, para que alinear las tijeras con un punto incorrecto ya no produzca un falso positivo.

Se recompiló con Unity 6000.3.23f1: cero errores y cero advertencias del compilador. El CLI emite por separado un aviso sobre nombres antiguos de Pipeline; no se actualizó el paquete. La prueba `StemCutRegression.cs` pasó 14 comprobaciones, además de los rechazos focalizados del ayudante `VerifyStemCut`: geometría real, fruto unido, corte único, escala, gravedad, recuperación sin reconexión, caída directa sin recogida, Hover, Select, entrega al soltar y contabilización única. Las muestras de cierre y los eventos Meta son sintéticos; no equivalen a probar seguimiento físico.

Informes de esta corrección: `Docs/Validation/StemCutCompilation.json`, `StemCutRegression.txt`, `StemCutRegressionResult.json` y `StemPointAfter.json`. Las imágenes `StemCutVideo/Before-StemTarget-*.png` muestran el fallo y `StemCutVideo/StemTarget-*.png` el extremo corregido. No se generó APK ni se probaron voces o sensores físicos de Quest 3.
