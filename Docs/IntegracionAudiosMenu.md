# Integración de voces de menú y accesibilidad

Fecha: 27 de octubre de 2026.

Actualización posterior: se sustituyeron las voces de la lección por las
grabaciones suministradas, incluida ahora la bienvenida corta, y se integró el
suministro limitado de agua. El catálogo actual tiene 57 cues y los IDs de la
lección ya no redirigen a las versiones anteriores de accesibilidad/izquierda.
El comportamiento actual, los manifiestos y las comprobaciones de Play se
describen en `AhorroAgua.md`; el resto de este documento recoge la entrega previa.

## Uso en el juego

- El menú inicial reproduce `menu_intro` una vez. Pulsar Play o cambiar de página
  interrumpe esa introducción; la bienvenida del tutorial comienza después de Play.
- Controls permite elegir Hands o Controllers y revisar movimiento, agarre,
  calendario o selección de botones. Cada demostración tiene voz, Replay y Stop.
  Back detiene tanto la voz como la demostración y regresa a la biblioteca.
- La primera visita a Controls reproduce `menu_tutorial_intro`. Elegir una acción
  interrumpe esa explicación y comienza la demostración solicitada.
- La elección Hands/Controllers en la biblioteca es independiente del dispositivo
  utilizado para seleccionar los botones. No modifica los controles del jardín.
- Sound settings incluye Test voice y Stop voice. La muestra usa
  `settings_voice_sample` y respeta el volumen general, el de Gronny y silencio.
  Cambiar el volumen mientras se escucha la muestra no la reinicia ni la detiene.
- Al pausar se conserva la voz del tutorial; solo se reproduce la voz solicitada
  en el menú. Al volver al jardín, la voz anterior continúa donde quedó.
- Consultar la biblioteca no completa objetivos, planta, riega ni registra una
  nueva asistencia en el journal del tutorial original.

## Audios conectados

El catálogo `Assets/Resources/GronnyAudio/Tutorial.asset` conserva los 27 cues
originales y añade los 13 nuevos. Los WAV y sus `.meta` no se sustituyen ni mueven.

`FindTutorialCue` resuelve las instrucciones de movimiento y calendario originales
a las grabaciones nuevas de la izquierda, que coincide con las entradas actuales.
Las instrucciones de plantación y cosecha usan las versiones `*_accessible`.
Los identificadores originales se conservan para las llamadas y el journal.

Las semillas de tomate muestran Tomato hasta su plantación. El fruto recogible
muestra Ready to harvest hasta que se agarra. Los rótulos no tienen colliders ni
rayos interactivos; se ocultan durante el menú y se destruyen con sus objetos.

La biblioteca incluye las grabaciones derechas y sus modelos/clips correspondientes
como previsualizaciones. La pantalla indica expresamente que movimiento y
calendario en el jardín siguen usando la izquierda. No se aplica una preferencia
guardada a la voz sin cambiar también la entrada real. La lateralidad efectiva y
el conflicto entre B/calendario/rellenado siguen siendo una entrega independiente.

Se conserva la animación de deslizamiento del pulgar creada anteriormente. Los
tiempos de pasos de los nuevos controles se escalan a la duración real de cada WAV;
la sincronización fina y la orientación de ambas manos deben revisarse en Quest 3.

## Mantenimiento

Garden > Gronny > Connect menu and accessibility audio permite añadir cues que
falten a partir de `Docs/GronnyMenuYAccesibilidad.json`, sin reemplazar los cues
existentes ni sus ajustes del Inspector. La creación inicial del catálogo también
incluye estos audios. No se necesita ejecutar un script de generación de voz.

## Validación

- Unity CLI informó compilación al día, sin errores ni advertencias de C#:
  `output/MenuAudioCompile.json`.
- La comprobación del catálogo pasó: 40 referencias, cobertura de los WAV,
  identificadores únicos, tiempos válidos, recursos Meta y destinos/animaciones
  de mandos derechos: `output/MenuAudioCatalogue.json` y
  `Docs/Validation/GronnyCatalogue.txt`.
- Comprobación de Play de menú y audio: 57 comprobaciones correctas en
  `output/MenuPlayVerification.txt`. Incluyen introducción del menú, repetición y
  parada de voz/demos, previsualización derecha con su modelo, muestra de voz,
  conservación de la posición de la narración y ausencia de progreso/asistencia
  al revisar controles, además de las regresiones de pausa y riego existentes.
- Editor fuera de Play al terminar: `output/MenuAudioEditorAfter.json`.
- La escucha final dentro del visor, selección con manos/mandos reales, comodidad,
  legibilidad y rendimiento siguen pendientes de validación física en Quest 3.
