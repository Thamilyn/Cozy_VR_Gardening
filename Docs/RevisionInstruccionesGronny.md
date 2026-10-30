# Revisión de instrucciones de Gronny — 29 de octubre de 2026

## Hallazgos

- El catálogo contiene 58 voces: 45 de la lección y 13 de menú/accesibilidad.
- El saludo es la grabación más larga: 20,48 segundos y 58 palabras. Las vistas
  previas de movimiento con manos llegan a 44 palabras. El menú mostraba el
  guion entero, aunque las demostraciones ya tenían sus propios rótulos.
- El panel de Gronny elegía el texto por el estado del juego, sin consultar qué
  instrucción estaba reproduciendo la voz. Por ejemplo, mostraba el paso de
  movimiento durante el saludo.
- Durante la voz, las manos usaban un reloj que separaba las poses al menos
  2,5 segundos. La demostración de movimiento podía durar 20 segundos frente
  a una voz de 10,16 segundos. Ese ritmo es útil para practicar después, pero
  retrasa las poses respecto al audio cuando se usa durante la grabación.
- La demostración de calendario con manos sustituía todos los rótulos por una
  frase fija de tres líneas, aunque la pose cambiase.
- La alineación automática también detectó marcas de poses adelantadas: la
  pinza para recoger la semilla empezaba a 1,76 s, frente a la palabra Pinch
  a 2,28 s; el toque de activación del movimiento empezaba a 2,45 s, frente a
  Touch a 5,26 s. Se ajustaron las 23 pistas de demostración a la voz vigente.
- Gronny sí indica plantar la semilla: `03_plant_seed` dura 7,76 segundos.
  Su guion pide mover la semilla identificada como Tomato sobre la tierra de
  una maceta pequeña vacía y soltarla para plantarla. El rótulo flotante
  `Pick me and plant me / on the plant pot` repetía esa instrucción.

## Cambios

- Se retiró el rótulo flotante de plantación y su código auxiliar. La etiqueta
  que identifica la semilla como Tomato y la instrucción de voz se conservan.
- Cada voz tiene un título y un detalle breves, de hasta 15 palabras en total.
  Se guardan en `instruction` e `instructionDetail` del catálogo. `transcript`
  conserva el guion completo de la grabación.
- El panel consulta la voz activa antes de mostrar el paso pendiente. Los
  textos caducados se retiran al completar una acción, cambiar de fase o de
  entrada. Los avisos del estado del agua, el agotamiento, el menú y la
  transición temporal siguen teniendo prioridad sobre la instrucción hablada.
- Las vistas previas del menú muestran el resumen breve de la voz elegida.
- Durante la reproducción, poses y rótulos usan `AudioSource.time`. Al terminar
  la voz, las instrucciones que esperan una acción repiten la práctica lenta.
  El calendario vuelve a mostrar el rótulo correspondiente a cada pose.
- Los pasos extra de retroceder y salir del movimiento, ausentes de la voz
  breve, tienen `practiceOnly`: se muestran durante la práctica posterior.
  La vista previa completa de cada mano conserva sus pasos hablados.
- Los importadores rellenan los resúmenes al crear el catálogo; respetan los
  resúmenes ya editados en el Inspector. Al clonar otra voz no heredan por error
  el resumen de la plantilla. Los valores iniciales están en
  `Docs/GronnyInstructionCaptions.json`.

## Validación realizada

Se revisaron estáticamente los 58 enlaces a WAV, sus cabeceras y duraciones,
guiones, resúmenes y tiempos de demostración. Todos los guiones coinciden con
los JSON vigentes. Los hashes coinciden con el registro de importación aprobado
o con el inventario existente. Los tiempos están ordenados y dentro del clip.

Los 120 archivos de audio y metadatos bajo `Assets/Audio` conservan sus hashes.
El catálogo conserva todas las referencias, guiones, prioridades, ajustes y
poses. Los cambios serializados son los resúmenes, los tiempos de demostración
y tres marcas de pasos exclusivos de práctica. Auditorías:
`output/GronnyInstructionReview/StaticAudit.json` (inicial) y
`output/GronnyInstructionReview/FinalStaticAudit.json` (tras ajustar los tiempos).

Con autorización del usuario se validó en Unity `6000.3.23f1`, mediante Unity
CLI `1.0.0-beta.12` y el Pipeline `0.8.0-exp.1` ya instalado. La conexión inicial
falló durante la carga del Editor; después quedó disponible. No se actualizaron
Unity ni sus paquetes.

- Compilación final: cero errores y cero advertencias (`FinalCompile.json`).
- Voz y textos: 35 comprobaciones correctas (`VoiceAndTextPlay.txt`). Incluyen
  el texto del panel real, el reloj de audio, los pasos exclusivos de práctica,
  caducidad, interrupciones, reanudación y ausencia del rótulo flotante.
- Menú, preferencias, pausa y suministro: 95 comprobaciones correctas con los
  tiempos definitivos (`MenuPlay.txt`).
- Movimiento visible del pulgar: 34 comprobaciones correctas (`ThumbSwipePlay.txt`).
  La captura `output/ThumbSwipeFrames.png` se revisó visualmente.
- Saludo y progresión del aprendizaje de movimiento: 15 comprobaciones
  correctas (`WelcomeMovementPlay.txt`). Total: 179 comprobaciones de Play.
- Recreación de los 58 resúmenes y sus tiempos desde el JSON, en una copia
  temporal del catálogo: correcta (`CaptionImportVerification.json`).

Los nombres de informe anteriores son relativos a `output/GronnyInstructionReview`.
Las pruebas usan AudioSource y componentes reales, con entradas sintéticas.
No constituyen una prueba física de manos, mandos ni audio del visor.
El Editor quedó fuera de Play, con `Garden_Moves` abierta y sin cambios
pendientes en la escena (`EditorAfter.json`).
La consola registró `MetaXRFeature: ErrorFormFactorUnavailable xrGetSystem`
al iniciar cada sesión de Play: el sistema XR del visor no estaba disponible
para estas pruebas. Los seis errores capturados corresponden a ese mismo
mensaje; no son errores de compilación del tutorial (`ConsoleErrors.json`).

Whisper local completó los 58 audios (`WhisperAudioQA.json`, salida 0). No detectó
muestras saturadas: pico máximo 0,99002 y fracción de clipping 0. Cincuenta
transcripciones coinciden con el guion normalizado. Ocho presentan diferencias:
una es únicamente la grafía británica/estadounidense labelled/labeled; cinco
son Advance/Advanced; una interpreta Gronny como Granny y otra and practise
como in practice. Las siete ambigüedades de reconocimiento necesitan escucha
humana antes de atribuirlas a una pronunciación incorrecta. No se cambiaron los
guiones para acomodar errores posibles del reconocedor.

Se obtuvieron marcas automáticas por palabra para las 23 voces con demostración
y seis voces adicionales con discrepancias (`WordTimings.json`). Los ajustes
de poses están registrados en `DemonstrationTimingChanges.json` y como valores
iniciales en `Docs/GronnyInstructionCaptions.json`. Las marcas de Whisper son
aproximadas y no equivalen a una alineación fonética comprobada por una persona.

Pendientes: escucha humana de las ambigüedades y validación de claridad,
comodidad y sincronización en Meta Quest 3. Los resúmenes son indicaciones
breves, no subtítulos literales. No se regeneró ni recortó ninguna grabación.

## Inventario de la revisión

ID | Audio (s) | Palabras habladas | Indicación visual
---|---:|---:|---
01_welcome | 20.48 | 58 | Welcome! I'm Gronny. Hands visible. Let's grow a tomato and save water.
02_tomato_intro | 6.48 | 15 | Let's grow a tomato. First, practise moving.
03_plant_seed | 7.76 | 27 | Plant the Tomato seed. Release it onto the soil in an empty small pot.
04_first_watering | 5.68 | 16 | Tilt the can over the soil. Aim near the roots. Turn upright to stop.
05_water_target | 8.28 | 24 | Watch the plant's water bar. Stop pouring at the target.
06_enough_water | 4.20 | 11 | Enough water. Stop pouring. Hold the can upright.
07_first_advance | 5.68 | 14 | Your plant is ready to grow. Calendar: select Advance phase.
08_sprout | 6.12 | 16 | A little sprout! Water the soil. Stop at the target.
09_young_plant | 6.76 | 20 | Your tomato is growing. Water to this stage's target.
10_flowering | 6.92 | 20 | Your plant has flowers! Watch the water bar. Save water for later.
11_green_fruit | 7.20 | 18 | A green tomato! Use the water target to decide when to stop.
12_harvest | 7.00 | 21 | Your tomato is ripe! Put the largest red tomato in the harvest tray.
13_complete | 8.68 | 25 | You grew your first tomato! Thanks for gardening and saving water.
help_planting | 4.68 | 15 | Release the seed over the soil. Aim for the centre of an empty small pot.
help_low_water | 5.72 | 18 | Your plant needs more water. Pour gently. Stop at the target.
help_excess_water | 5.56 | 18 | Too much water. Stop pouring. Hold the can upright. Let the extra water drain.
help_drained | 7.44 | 19 | The extra water has drained. Next time, stop at the target.
help_calendar | 5.16 | 12 | Your plant has enough water. Calendar: select Advance phase.
help_refill | 6.44 | 18 | Your watering can is empty. Calendar: Refill water uses the reserve.
controls_move_hands | 10.16 | 28 | Practise moving. Curl your index. Tap with your thumb, then swipe.
controls_grab_hands | 7.40 | 25 | Pick up the Tomato seed. Pinch to grab. Open your fingers to release.
controls_calendar_hands | 7.00 | 19 | Open the calendar. Left hand away from objects. Pinch, then release.
controls_calendar_select_hands | 5.12 | 15 | Select Advance phase. Point at the button, then pinch.
controls_move_controllers | 4.84 | 14 | Practise moving. Push the left joystick to walk.
controls_grab_controllers | 6.36 | 21 | Pick up the Tomato seed. Hold Grip to grab. Release Grip to let go.
controls_calendar_controllers | 3.52 | 10 | Open the calendar. Press Y on the left controller.
controls_calendar_select_controllers | 3.84 | 11 | Select Advance phase. Aim at the button, then press Trigger.
controls_move_hands_left | 15.28 | 44 | Move with your left hand. Curl your index. Tap with your thumb, then swipe.
controls_move_hands_right | 16.20 | 44 | Move with your right hand. Curl your index. Tap with your thumb, then swipe.
controls_move_controllers_left | 6.40 | 18 | Move with the left joystick. Release the joystick to stop.
controls_move_controllers_right | 5.92 | 18 | Move with the right joystick. Release the joystick to stop.
controls_calendar_hands_left | 10.24 | 28 | Calendar: left hand. Away from objects: pinch, then release.
controls_calendar_hands_right | 9.52 | 28 | Calendar: right hand. Away from objects: pinch, then release.
controls_calendar_controllers_left | 5.64 | 18 | Calendar: left controller. Press Y to open. Press again to close.
controls_calendar_controllers_right | 5.20 | 18 | Calendar: right controller. Press B to open. Press again to close.
03_plant_seed_accessible | 5.40 | 18 | Plant the seed labelled Tomato. Release it onto the soil in an empty small pot.
12_harvest_accessible | 6.08 | 19 | Your tomato is ripe! Put the fruit marked ready in the harvest tray.
menu_intro | 8.80 | 25 | Ready to garden? Select Play. Controls and sound settings are also available.
menu_tutorial_intro | 8.44 | 22 | Review the controls. Choose Hands or Controllers, then a demonstration.
settings_voice_sample | 6.44 | 19 | Set Gronny's volume. Adjust until you can hear comfortably.
controls_watering_hands | 7.68 | 26 | Pick up the watering can. Move close. Close your fingers and keep holding.
controls_watering_controllers | 6.52 | 21 | Pick up the watering can. Move close. Hold Grip and keep holding.
water_practice_intro | 6.24 | 17 | Practise watering first. Water is unlimited during practice.
water_reserve_intro | 6.32 | 19 | Your water supply is now limited. Use only what the tomato needs.
water_reserve_explanation | 7.92 | 22 | Refilling uses the garden's reserve. Each refill leaves less water for later.
water_reserve_rules | 6.16 | 20 | Make the water last. If the can and reserve run out early, try again.
water_stopped_success | 6.20 | 19 | You stopped at the target! The remaining water is saved for later.
water_ready_next_phase | 5.72 | 14 | Watering is done. Calendar: select Advance phase.
help_missed_pot | 5.96 | 18 | The water is missing the soil. Hold upright. Move over the pot, then pour.
help_stop_watering | 5.84 | 15 | Enough water. Stop pouring. Hold the can upright to save water.
controls_refill_select_hands | 5.04 | 15 | Select Refill water. Point at the button, then pinch.
controls_refill_select_controllers | 4.28 | 11 | Select Refill water. Aim at the button, then press Trigger.
help_reserve_low | 5.12 | 15 | The water reserve is low. Pour carefully. Stop at the target.
help_reserve_empty | 6.08 | 20 | The reserve is empty. Use the water in your can carefully.
water_supply_exhausted | 5.40 | 19 | Not enough water to finish. Try again. Practise stopping at the target.
water_retry | 3.60 | 10 | Select Try again. Start with a fresh water supply.
water_no_waste_complete | 5.68 | 17 | You grew a tomato without waste! You gave the plant just what it needed.
movement_success | 3.16 | 9 | You've got it! Come over to the gardening table.
