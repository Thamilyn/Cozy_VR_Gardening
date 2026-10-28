# Gronny: grabaciones de menú y accesibilidad

Estado: los 13 WAV de este lote están generados en
`Assets/Audio/menu_accessibility/`, con KittenTTS 2, revisión `v2`, voz Kiki,
preset `expressive` y temperatura `1.1`. Ya tienen referencias en el catálogo y
reproducción en el menú/tutorial. Uso y límites en `IntegracionAudiosMenu.md`.
Complementa las grabaciones originales.

## Lote de grabación

Generar 10 audios principales y, si se desea narrar el menú, 3 opcionales.
El archivo `GronnyMenuYAccesibilidad.json` contiene exactamente los mismos textos,
con identificadores únicos. Sus campos `text` son el único contenido hablado.

- Conservar la voz, el idioma inglés y la configuración de los audios originales.
- Interpretación cálida, tranquila y clara, sin acelerar las instrucciones largas.
- Exportar un WAV por texto, usando el identificador como nombre, más `.wav`.
- Mantener la misma configuración de exportación que los originales.
- No incluir títulos, identificadores ni notas técnicas en la locución.
- Pronunciar Y como la letra inglesa «why» y B como «bee».
- Mantener los originales. Los nombres nuevos evitan sobrescribirlos.
- Guardar los resultados nuevos dentro del proyecto, en
  `Assets/Audio/menu_accessibility/`, cuando estén generados.

Los 10 principales son suficientes para cubrir ambos lados en movimiento y
calendario y sustituir las dos instrucciones de identificación por color. No
requieren volver a generar bienvenida, riego, crecimiento ni ayudas existentes.

## Contrato para la implementación posterior

- Los sufijos `left` y `right` indican la mano que realiza esa acción; no significan
  necesariamente jugador diestro o zurdo. Movimiento y calendario se configuran
  por separado. Nunca reproducir las dos variantes seguidas.
- Movimiento con manos: índice doblado, toque del pulgar sobre el índice para
  activar, deslizamientos para los pasos e índice extendido para salir. No invertir
  esa secuencia al preparar la variante derecha.
- Movimiento con mandos: reproducir la variante del joystick realmente asignado.
- Calendario con mandos: variante izquierda para Y; variante derecha para B.
  El intercambio debe resolver el conflicto con el atajo actual de rellenado.
  El botón Refill water del calendario sigue siendo una alternativa disponible.
- Calendario con manos: una pinza nueva abre o cierra; no exigir mantenerla hasta
  soltar para que se produzca la apertura. La liberación prepara el gesto siguiente.
- El juego todavía no ofrece todas estas configuraciones. Los audios derechos
  deben incorporarse cuando también funcionen sus entradas y demostraciones.
- La instrucción de plantación exige una etiqueta legible Tomato en la semilla
  o su identificación contextual. La de cosecha exige un marcador inequívoco
  Ready to harvest para el fruto que se pueda recoger. Implementarlos antes de
  activar estas voces; no bastaría con cambiar solo el audio.
- Las voces nuevas tendrán tiempos distintos: ajustar los pasos de las demos a
  cada WAV. No reutilizar los tiempos antiguos suponiendo que coinciden.
- Este lote no narra cómo abrir el nuevo menú, porque su entrada aún está por
  definir. La interfaz debe permitir descubrir ese acceso sin depender de voz.

## Principales: movimiento con manos

### controls_move_hands_left.wav

With your left hand, curl your index finger and keep your thumb free. Tap your thumb against your curled index finger to activate movement. Swipe your thumb forward to step forward, or backward to step back. Straighten your index finger to exit movement mode.

### controls_move_hands_right.wav

With your right hand, curl your index finger and keep your thumb free. Tap your thumb against your curled index finger to activate movement. Swipe your thumb forward to step forward, or backward to step back. Straighten your index finger to exit movement mode.

## Principales: movimiento con mandos

### controls_move_controllers_left.wav

Use the joystick on your left controller to move. Let the joystick return to the centre to stop.

### controls_move_controllers_right.wav

Use the joystick on your right controller to move. Let the joystick return to the centre to stop.

## Principales: calendario con manos

### controls_calendar_hands_left.wav

Keep your left hand free and away from objects. Pinch your thumb and index finger together to open the calendar, then release. Repeat the gesture to close it.

### controls_calendar_hands_right.wav

Keep your right hand free and away from objects. Pinch your thumb and index finger together to open the calendar, then release. Repeat the gesture to close it.

## Principales: calendario con mandos

### controls_calendar_controllers_left.wav

Press the Y button on your left controller to open the calendar. Press it again to close it.

### controls_calendar_controllers_right.wav

Press the B button on your right controller to open the calendar. Press it again to close it.

## Principales: identificación sin depender del color

### 03_plant_seed_accessible.wav

**Sustituirá:** `03_plant_seed.wav`, tras añadir la etiqueta Tomato.

Pick up the seed labelled Tomato and gently drop it onto the soil in an empty small pot.

### 12_harvest_accessible.wav

**Sustituirá:** `12_harvest.wav`, tras añadir el marcador Ready to harvest.

Our tomato is ripe! Pick the fruit marked ready to harvest and gently place it in the harvest tray.

## Opcionales: menú y muestra de voz

Estos tres audios complementan el menú; no son necesarios para que funcione.
Ninguno debe repetirse automáticamente al navegar por cada botón.

### menu_intro.wav

**Uso:** introducción breve al menú inicial. Mantener `01_welcome` para cuando
comience la partida, evitando reproducir ambas bienvenidas seguidas.

Ready to spend some time in the garden? Select Play to begin. You can also review the controls or adjust your settings before you start.

### menu_tutorial_intro.wav

**Uso:** al solicitar la explicación de la biblioteca de controles. La elección
de manos o mandos para la demo debe ser independiente de la entrada usada en el menú.

Choose hand tracking or controllers to review the controls. You can replay any demonstration and return to the garden whenever you're ready.

### settings_voice_sample.wav

**Uso:** únicamente al pulsar Probar voz; permitir detener o repetir la muestra.
Aplicar el volumen configurado sin cambiarlo desde la propia locución.

Hello again! This is how loud my voice will sound. Adjust the volume until you can hear me comfortably.

## Grabaciones originales que se conservan

- `01_welcome`, `02_tomato_intro`, `04_first_watering`, `05_water_target`,
  `06_enough_water`, `07_first_advance`, `08_sprout`, `09_young_plant`,
  `10_flowering`, `11_green_fruit` y `13_complete`.
- Las seis ayudas `help_*`.
- Los originales de movimiento y calendario: conservar como respaldo; las nuevas
  variantes sustituyen su uso al integrar la configuración de lateralidad.
- `controls_grab_hands`, `controls_grab_controllers`,
  `controls_calendar_select_hands` y `controls_calendar_select_controllers`:
  previstos para reutilizarse con demos de cualquiera de los lados. Su texto
  hablado no está transcrito en los documentos revisados: escuchar los cuatro
  antes de certificar que no nombran una mano fija. Si la nombran, necesitarán
  una versión neutral; no se ha supuesto que su contenido sea independiente.

La mención descriptiva a green tomatoes en `11_green_fruit` se puede conservar:
no es la única señal para identificar un objeto ni ordena elegirlo por su color.
La fase y el cuidado seguirán mostrándose mediante texto e iconos.

## Comprobación al recibir los WAV

1. Escuchar cada archivo y contrastarlo con su texto, especialmente izquierda,
   derecha, Y, B y la secuencia del microgesto.
2. Comprobar los 10 nombres principales y, si se grabaron, los 3 opcionales.
3. Comparar voz, ritmo y nivel percibido con los audios actuales; evitar cortes
   al inicio/final y diferencias molestas entre frases.
4. Incorporar referencias explícitas al catálogo sin renombrar recursos originales
   ni alterar sus `.meta`. Evitar cualquier importador que espere solo 27 clips.
5. Sincronizar demos, textos de interfaz y selección de variantes con las entradas
   efectivas. Verificar después en Unity y Meta Quest 3 con autorización.

## Resultado de generación y comprobación

Los 10 audios principales y los 3 opcionales se generaron con las librerías y
modelos locales existentes, sin instalaciones ni descargas. Los originales se
conservan. El adaptador de generación está en `Docs/GenerarVocesMenu.py`.

La comprobación de los 13 archivos confirmó señal finita y no silenciosa,
frecuencia de muestreo de 24 kHz, ausencia de duplicados binarios y ausencia de
muestras saturadas. Duración total: 109,56 segundos.

La transcripción local con `openai/whisper-base.en` coincidió en 12 de 13 archivos
tras la normalización del comprobador. La única diferencia del restante,
`03_plant_seed_accessible.wav`, fue labelled frente a labeled: variantes
ortográficas de la misma palabra, sin discrepancia de contenido detectada.
El informe conserva las transcripciones y la diferencia original en
`output/VocesMenuValidacion.json`.

El usuario aprobó las dos pruebas de voz anteriores guardadas en
`output/VocesMenuPrueba/`. El lote completo se generó con la misma configuración.
La comprobación automática no sustituye la escucha del lote final, el ajuste de
las demostraciones ni su validación física en Meta Quest 3. El menú, la biblioteca
de voz/demos y los marcadores accesibles están integrados; consulta
`IntegracionAudiosMenu.md`. Las entradas de lateralidad configurables siguen
pendientes; las variantes derechas solo se usan como previsualizaciones indicadas.
