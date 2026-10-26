# Entrega 1: base de ajustes y menú

Fecha: 26 de octubre de 2026. Estado: compilación y comprobaciones sintéticas de
Play realizadas; pendiente validar en Quest 3. Esta entrega no completa todo
`PlanMenuYAccesibilidad.md`.

## Alcance implementado

- Instalación automática del menú al cargar `Garden_Moves`, antes de los `Start`
  del jardín. No requiere ejecutar un generador ni guardar cambios en la escena.
- Inicio con Play, Controls y Sound settings, conservando el inglés del juego.
  La bienvenida y el tutorial progresivo esperan a Play y a soltar la selección.
- Durante la sesión, botón visible Menu, seleccionable con los rayos Meta de
  cualquiera de los lados. Return to garden conserva los objetos y el estado de
  esa sesión. No guarda el cultivo entre cierres.
- Paneles estables en el mundo, con la resolución de obstáculos ya usada por el
  calendario. Se recolocan al quedar fuera del campo frontal o al alejarse.
- Página de consulta de controles. La biblioteca de reproducciones independientes
  con manos/mandos corresponde a la siguiente entrega; todavía no está incorporada.
- Volumen general mediante AudioListener, volumen relativo de Gronny aplicado
  también al audio actual, silencio y restauración de valores de sonido.
- Preferencias locales bajo la única clave `CozyGarden.Preferences.v1`, con
  defaults, validación de números finitos, límites y enums. No se serializan las
  plantas, el diario, los pasos aprendidos ni los registros de ayuda.
- La estructura reserva brillo (0,7–1,3), mano principal, movimiento, calendario y
  paleta por separado. Sus selectores no se muestran y sus integraciones siguen
  pendientes; guardar esos valores todavía no cambia el rig ni la imagen.

## Revisión de entradas y renderizado

Fuentes revisadas: escena `Assets/Scenes/Garden_Moves.unity`, scripts propios,
prefab Meta `OVRComprehensiveInteractionRig`, su cadena de prefabs anidados y las
APIs de la versión ya instalada en `Library/PackageCache` (solo lectura).

| Acción | Estado observado en disco |
| --- | --- |
| Rig | La escena referencia `OVRComprehensiveInteractionRig` (GUID `0a7d2469f24041c4284c66706f84c45e`). Tiene dos grupos Comprehensive con configuraciones de lateralidad y fuentes de manos/mandos. |
| Movimiento con manos | La cadena incluye `MicroGesturesLocomotionHandInteractorGroup`, `LocomotionGate`, `StepMicrogesture` y `StepLocomotionBroadcaster`. El paso base del prefab es 0,8 m. El tutorial original enseña movimiento izquierdo. |
| Movimiento con mandos | Incluye `LocomotionControllerInteractorGroup`, `ControllerSlide`, `ControllerStep` y fuentes `ControllerAxis2D` del joystick. `MovingSetting` parte de Slide (valor 0). No basta con cambiar el texto del tutorial para intercambiar el movimiento. |
| Giro | `Locomotor` conecta dos turners de mandos y dos de locomoción. El valor base de Turning es Snap con 45 grados. Gronny bloquea los interactores de giro y teletransporte durante sus pasos iniciales. |
| Calendario | `CalendarVisibilityController`: pinza nueva de la mano izquierda o Y/LTouch; liberación de 0,1 s para rearmar. Usa el rayo procesado de Meta y evita pinzas de objetos/UI. |
| Selección de UI | RayInteractor → RayInteractable/ColliderSurface → PointableCanvas → PointableCanvasModule → Button; manos con pinza, mandos con Trigger. El menú reutiliza este circuito con inyección pública del SDK instalado. |
| Agarre | `GardenGrabSetup` configura Grabbable e interactables de manos/mandos; el mando usa Grip. No se desactiva el tracking ni el agarre que ya sostiene un objeto al abrir el menú. |
| Rellenar | `WateringCan`: Button.Two/RTouch (B), condicionado a sostenerla y a permisos de Gronny. El calendario conserva Refill water. |
| Recuperar | `CalendarPanel.RecoverCan` sigue priorizando derecha y después izquierda. Su adaptación a preferencias se hará con la entrega de lateralidad. |
| Acceso al menú | Objetivo visible Menu, con pinza o Trigger. No se añade un botón físico ni un gesto de sistema. Se evita que la pinza sobre Menu también abra el calendario. |
| Imagen | URP existente; `GardenPhaseTimeCycle` interpola un skybox privado, sol y ambiente Trilight. Esta entrega solo adapta sus esperas a la suspensión, sin implementar brillo ni cambiar materiales guardados. |

Este inventario verifica referencias y valores base serializados, no la
configuración efectiva tras los `Start`, gates y cambios de tracking. Quedan por
confirmar en ejecución el reparto real de locomoción entre lados, la activación
de slide/step/teleport, el giro y los botones auxiliares. Por eso no se ofrecen
intercambios de lado ni se activan las variantes de voz derechas. En especial,
calendario B y relleno B siguen sin compartir una entrada.

## Suspensión y reanudación

`GardenMenuController.GardenSuspended` cubre tanto el menú abierto como la espera
de liberación al volver. No modifica `Time.timeScale`.

- La suspensión desconecta broadcasters/conexiones de locomoción e interactores
  de giro/teletransporte y elimina la velocidad residual. Mantiene el seguimiento,
  la colisión por movimiento físico de cabeza y los rayos de UI.
- Desactiva nuevos agarres del jardín, conservando los seleccionados. Los objetos
  libres y los soltados durante el menú quedan cinemáticos, sin velocidad pendiente.
  Al volver se restauran únicamente los componentes/cuerpos modificados por el menú.
  Los bloqueos previos de Gronny mantienen su propietario.
- El calendario se oculta por una ruta que no depende de haber aprendido Close.
  No se reabre automáticamente al volver.
- El riego se detiene sin descontar agua. Después exige pasar por la posición
  vertical para rearmar el vertido, incluso si la regadera seguía agarrada.
- Se bloquean plantación, cosecha, avance, rellenado y recuperación. Una semilla
  soltada dentro del menú no registra la acción ni se planta al volver. Un fruto
  soltado dentro del menú necesita un nuevo agarre antes de contarse en la bandeja.
- El drenaje no modifica agua mientras el jardín está suspendido. Sus marcas de
  tiempo existentes siguen siendo absolutas: al volver puede resolverse un drenaje
  cuyo tiempo de espera ya haya transcurrido; no se simula cultivo durante el menú.
- Las esperas e interpolaciones del ciclo de día/noche se congelan y el avance no
  se confirma con el menú abierto.
- Gronny conserva cola, audio, pasos aprendidos y registros de asistencia. Se pausa
  la fuente, se oculta la demo y se vuelve al punto de audio al reanudar. No se
  desactiva el componente del tutorial ni se vuelve a reproducir la bienvenida.
- Antes de aceptar entradas tras una apertura y antes de reactivar el jardín se
  espera liberación estable de botones, gatillos y joysticks. En manos, las libres
  deben soltar la pinza y extender el índice para salir del microgesto. La mano que
  ya sostiene un objeto no está obligada a soltarlo. La interfaz muestra este paso.

## Comprobaciones realizadas

- Lectura de ambas guías de diseño/voz, de `AGENTS.md`, de los consumidores de
  entradas, el rig anidado y los puntos donde se modifica el estado del cultivo.
- Contrastadas en las fuentes locales del SDK las APIs públicas de inyección de
  PointableCanvas, RayInteractable, ColliderSurface, IHand e interactores.
- Revisión de flujos de inicio, bloqueo, cierre, pausa de voz/demos, reanudación,
  cambios de página y restauración de preferencias. No equivale a compilar C#.
- Añadidos `.meta` únicos a los cuatro scripts nuevos. No se modificaron escenas,
  prefabs, catálogos, materiales, archivos de audio ni sus `.meta` existentes.
- `ProjectVersion.txt` continúa en **6000.3.23f1**. No se modificaron paquetes.
- Consultados `unity --help`, `unity version --format json`, y ayuda de status,
  list, run y recompile. CLI instalado: **1.0.0-beta.12**; no se actualizó.
- `unity status --project-path "C:\Users\Trole\Cozy_VR_Gardening" --format json`
  devolvió `success: false`, `STATUS_NO_INSTANCES`, cero instancias y la advertencia
  de que no pudo comprobar todos los editores en ejecución. El manifiesto contiene
  `com.unity.pipeline` 0.8.0-exp.1, pero eso no confirma un Editor conectado.

La sección anterior describe la revisión inicial, antes de la autorización para
compilar y comprobar Play. La validación posterior figura a continuación.

## Validación autorizada en Unity

El usuario autorizó compilar y comprobar el menú en Play. El Editor ya estaba
abierto; inicialmente no respondía Pipeline y el aislamiento ocultaba el proceso.
La consulta fuera del aislamiento detectó el Editor **6000.3.23f1**, PID 34600.
Al activar su ventana importó los scripts y Pipeline quedó disponible en el puerto
7800. Se mantuvo esa instancia y no se cerró el Editor ni se guardó la escena.

- `unity recompile --project-path "C:\Users\Trole\Cozy_VR_Gardening" --timeout 120
  --format json`: compilación completada, sin errores ni advertencias de C#.
  Resultado final: `output/MenuCompileFinal.json`.
- Ejecutado `Garden/Verify menu in Play` mediante Unity CLI/Pipeline. El comprobador
  está en `Assets/Editor/GardenMenuVerification.cs`. Usa la escena abierta en Play,
  una pose temporal de cámara, muestras sintéticas para la decisión de liberación
  y eventos PointerEvent para el agarre; no pretende simular el tracking físico.
- Las **29 comprobaciones** cubren: suspensión inicial; bienvenida posterior a
  Play; ausencia de progreso por abrir el menú; dependencias Meta de UI; consumo
  del rayo; texto dentro de sus rectángulos; preferencias y restauración independiente
  de sonido; bloqueo hasta liberación estable; pausa/reanudación de la misma voz;
  conservación del aprendizaje y asistencia; regadera agarrada sin consumo de agua
  durante el menú; bloqueo de rellenado; rearme del vertido mediante posición vertical;
  suspensión de iluminación/día y confirmación única de la fase al volver.
- La preferencia se recarga dentro del mismo proceso desde PlayerPrefs y se
  restaura la clave original en `finally`. Esto no certifica todavía un reinicio
  real de aplicación en Quest. Para las pruebas de cultivo se desactiva Gronny
  después de comprobar su pausa: se aísla la suspensión del menú de la enseñanza.
- Informe: `output/MenuPlayVerification.txt`. Capturas revisadas: `output/MenuValidatedHome.png`,
  `output/MenuValidatedControls.png` y `output/MenuValidatedSound.png`. Se ajustó el
  orden de dibujo y se hizo opaco el fondo para que otros canvases del jardín no
  superpongan instrucciones sobre el menú.
- El comprobador termina su sesión de Play; el Editor queda abierto en edición.
  `Garden_Moves.unity` conserva la fecha de modificación previa del 21 de octubre.
  No se actualizaron paquetes ni se regeneraron voces.

Limitaciones del entorno: Meta/OpenXR informa `XR_ERROR_FORM_FACTOR_UNAVAILABLE`
porque no hay visor XR disponible. La primera consulta inmediatamente después de
entrar en Play agotó el tiempo de espera de Pipeline; se repitió tras completar el
arranque. La advertencia previa de URP sobre `Unity.PathTracing.Runtime` se conserva.
Estos mensajes se registran y no se presentan como una ejecución sin errores de XR.
La compilación de C# y las comprobaciones sintéticas no dependen de resolverlos.

## Validación física siguiente

1. Compilación y comprobaciones sintéticas realizadas según la sección anterior.
   Verificar ahora las entradas físicas en Quest 3; la autorización anterior no
   implica publicar, instalar software nuevo ni hacer una compilación Android.
2. En Play/Quest 3: Play retrasa la bienvenida, apertura en cada paso de Gronny,
   selección con ambos lados, manos↔mandos y tracking perdido/recuperado.
3. Abrir sosteniendo la regadera inclinada: agua constante; volver manteniéndola
   inclinada: sin vertido hasta ponerla vertical. Probar soltar herramientas y
   semilla/fruto durante el menú sin registrar cultivo o aprendizaje accidental.
4. Abrir durante un ciclo de fase: iluminación y día detenidos, luego una sola
   confirmación. Volver tras una demo hablada sin voces superpuestas ni aprendizaje
   causado por la consulta del menú.
5. Cambiar volumen/silencio, reabrir y reiniciar la aplicación para confirmar
   persistencia. Restaurar sonido sin tocar la sesión de cultivo.
6. Revisar colocación fija de paneles, contraste, tamaño de botones y coste
   de las búsquedas de interacción durante el menú en Quest 3, sentado y de pie.

## Menú de lanzamiento y pausa — 26 de octubre

Esta revisión sustituye la recolocación automática del menú según la mirada,
distancia o altura de los ojos. El flujo acordado tiene dos momentos:

1. Antes de Play, el menú de lanzamiento aparece frente al punto por donde entra
   el jugador: 1,3 m hacia delante y centro a 1,45 m sobre el origen del suelo.
   Ofrece Play, controles y sonido. El jardín y la bienvenida esperan; el botón
   de pausa junto a Gronny aún está oculto. La posición se toma una sola vez del
   rig inicial, sin seguir la cabeza ni mover al jugador.
2. Al primer Play, se oculta el panel de lanzamiento y se traslada la estación
   junto a Gronny. Allí queda el botón Menu, sobre «Guía de cultivo y agua».
   Al seleccionarlo se abre «Garden paused», con Return to garden, controles y
   ajustes de sonido. Volver reanuda la misma sesión, conserva la posición junto
   a Gronny y no vuelve a ejecutar el traslado.

Las indicaciones de la guía distinguen ambos estados: antes de empezar señalan
Play en la entrada; durante una pausa señalan los controles/sonido y Return to
garden. La bienvenida no se reproduce de nuevo al volver de una pausa.

Para la estación de pausa, el controlador toma el centro del borde superior del
RectTransform de la guía y su orientación. Deja **8 cm** de espacio libre entre
ese borde y la parte inferior tanto del panel abierto como del botón Menu.

El panel abierto se extiende hacia arriba, sin tapar las instrucciones; el botón
cerrado queda justo encima de la guía. En la escena actual los centros son
`(1.15, 1.964, -0.834)` y `(1.15, 1.6865, -0.834)`, respectivamente. Girar, caminar,
cambiar de página o reabrir no modifica sus posiciones ni orientación. Los canvases
conservan la cámara para la selección Meta, sin usarla para su colocación. Se
eliminó GardenPresentationPlacement de ambos paneles para que tampoco el sistema
de evitación de obstáculos pueda desplazarlos. La estación permanece fuera de la
jerarquía del rig y de la guía. Si una escena no tiene guía, conserva la posición
de lanzamiento respecto al origen inicial como alternativa.

- Las validaciones anteriores de cada estación quedan en los archivos
  `output/MenuFixed*` y `output/MenuAboveGuide*`. El comprobador actual verifica
  el lanzamiento cerca del origen, botón de pausa oculto antes de Play, traslado
  único sobre la guía, interfaz de pausa con Return to garden, páginas de ayuda y
  sonido durante la pausa, y posiciones fijas antes y después de la transición.
  Se mantienen los controles de pausa, agua, voz y fase.
- Compilación del flujo: `output/MenuLaunchPauseCompile.json`, sin errores ni
  advertencias de C#. Informe de Play: `output/MenuPlayVerification.txt`,
  **46 comprobaciones correctas** en la versión final del flujo y textos.
- Capturas: `output/MenuLaunchHome.png` (lanzamiento en la entrada),
  `output/MenuAboveGuideClosed.png` (acceso a pausa junto a Gronny),
  `output/MenuPauseHome.png` (pausa con Return to garden).
- El Editor quedó fuera de Play y Garden_Moves sin cambios pendientes:
  `output/MenuLaunchPauseEditorAfter.json` y `output/MenuLaunchPauseScenesAfter.json`.

La comprobación usa poses y entradas sintéticas. Sigue pendiente la comodidad y
selección física sentado/de pie en Quest 3; el entorno continúa sin visor XR
disponible y registra ErrorFormFactorUnavailable.

## Entregas pendientes

Biblioteca independiente de voz/demos, muestra de voz, ambiente/efectos, brillo de
la imagen con vista previa, lateralidad efectiva y solución B/calendario/relleno,
marcadores Tomato/Ready to harvest, paletas y señales inclusivas. Los 13 WAV nuevos
siguen intactos y sin referencias nuevas: no se activan voces que prometan entradas
o identificadores aún no implementados.
