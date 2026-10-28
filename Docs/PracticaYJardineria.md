# Práctica separada y jardinería sustentable

**Propuesta revertida.** Se retiró la escena separada y se restauró el tutorial
en `Garden_Moves`. Véase `ReversionTutorial.md`. Este documento y sus resultados
describen únicamente la implementación anterior que el usuario pidió revertir.

## Flujo

`Garden_Practice` es la primera escena habilitada en Build Settings. Es una copia
independiente del entorno configurado de `Garden_Moves`, con su mesa, macetas,
Gronny y el rig Meta existente. No se cambiaron versiones ni paquetes.

Después de Play y de soltar la selección, la práctica enseña movimiento, agarre,
plantación, riego, recarga y calendario. El agua se repone gratuitamente al
seleccionar Refill water; ya no se rellena automáticamente una regadera vacía.
No existe agotamiento ni Game over en esta escena. Al alcanzar el primer
objetivo de riego hay que practicar una recarga antes de avanzar el calendario.

Al terminar el primer cambio de día se muestra el brote y se reproduce
`08_sprout`. Después aparece Practice complete con el botón Enter garden.
Hay que soltar los objetos y la selección antes de cargar la siguiente escena.

`Garden_Moves` empieza con una semilla nueva, día cero, historial y medidas de
agua nuevos. No hereda la planta, el agua ni el desperdicio de práctica. Conserva
las preferencias de sonido. No presenta otro botón Play al llegar desde la
práctica ni vuelve a imponer los pasos de aprendizaje. Las demostraciones siguen
disponibles en el menú. Abrir Garden_Moves directamente en el Editor permite
probar solo la actividad principal.

## Reserva desde el inicio

La práctica conserva siempre agua libre. El jardín dispone de un único
presupuesto finito desde el inicio, incluyendo el agua de la primera regadera.
El presupuesto incluye los cinco objetivos de riego y un margen del 50 %. Antes
de plantar, se usa el área de la mayor maceta elegible para no penalizar la
elección de maceta. Tras plantar, los objetivos siguen usando su área real.
No se reinicia la reserva al aparecer el brote. Los reintentos, el balance y la
pausa por agotamiento conservan las reglas de `AhorroAgua.md`.

## Brote

La sustitución visual tenía desplazadas las primeras fases. Los siete modelos
ahora se usan en orden: maceta vacía, semilla, brote, planta joven, floración,
fruto verde y fruto maduro. El brote aparece en el mismo evento que confirma el
primer cambio de día. Su voz ya no espera las introducciones de reserva y reglas
que anteriormente se insertaban en ese punto.

## Voces

Se sustituyeron en sus rutas existentes `01_welcome`, `02_tomato_intro` y
`help_refill`, conservando `.meta` y referencias. Los otros 54 audios se
mantienen. El guion nuevo está en `GronnyPracticeAndGardenDialogues.json` y las
copias anteriores se registran en `WaterSavingAudioImport.json`.

Al entrar al jardín, Gronny dice:

> Hello! I'm Gronny. Welcome to our sustainable garden. Our goal is to grow a
> tomato while saving water, so our water reserve is limited. Come over to the
> table, and let's begin.

`02_tomato_intro` se usa como bienvenida específica de práctica. `help_refill`
sirve para ambas escenas sin atribuir a la práctica una reserva finita. La voz
Kiki y el generador local existente se conservan. El reconocimiento coincide en
dos grabaciones y en todo el saludo salvo el nombre propio, que interpreta como
Granny; se conserva la presentación de Gronny y queda pendiente escuchar su
pronunciación. Informe: `output/PracticeSeparationAudioVerification.json`.

## Comprobaciones

La secuencia visual se comprueba con Garden > Verify tomato pot visual
replacement. El recorrido entre escenas se comprueba con Garden > Verify
practice to gardening in Play, abriendo Garden_Practice. El menú y suministro
principal conservan Garden > Verify menu in Play, abriendo Garden_Moves.
Las muestras de aprendizaje y liberación son sintéticas; las escenas, recargas,
plantación, transición de día y carga de la siguiente escena son reales.

Resultados de esta revisión:

- Compilación sin errores ni advertencias: `output/PracticeSeparationCompile.json`.
- Catálogo de 57 audios correcto: `output/PracticeSeparationCatalogue.json`.
- Siete apariencias de maceta en orden, con alineación y un solo modelo activo:
  `Docs/Validation/TomatoPotVisualVerification.txt`.
- 24 comprobaciones correctas de Play: `output/PracticeToGardenVerification.txt`.
  Incluyen brote visual en el cambio de día, anuncio inmediato, recarga gratuita,
  fin de práctica, liberación antes de entrar, carga real de Garden_Moves,
  reinicio del cultivo y medidas, presupuesto finito desde el inicio y saludo
  sustentable seguido de la explicación de recarga.
- Captura revisada: `output/PracticeComplete.png`.

La consulta posterior del estado del Editor fue rechazada porque el revisor
automático alcanzó su límite de uso; esa consulta no se ejecutó. El informe de
24 comprobaciones ya se había generado correctamente. No se pudo consultar el
estado final en vivo ni repetir la batería general de menú/suministro tras estos
cambios. Las 87 comprobaciones documentadas previamente son de la versión que
combinaba práctica y jardín y no se atribuyen a esta revisión.

Pendiente en Quest 3: controles físicos, pronunciación del nombre, claridad de
las instrucciones, comodidad del cambio de escena y rendimiento. No se generó
ni instaló un APK.
