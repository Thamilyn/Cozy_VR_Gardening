# Práctica: introducción hasta el primer riego, sin crecimiento

**Práctica separada retirada a petición del usuario.** Se vuelve a una sola
escena, Garden_Moves, con tutorial y cultivo continuos. Las voces conservan
su tono original. Este documento describe la variante anterior; véase
`ReversionTutorial.md` para el flujo vigente.

`Assets/Scenes/Garden_Practice.unity` copia la escena configurada de Garden_Moves
y tiene identidad propia. Se reutilizan rig Meta, mesa, macetas, guía, menú,
controles de manos/mandos y voces existentes. Garden_Moves conserva su flujo y
sigue siendo la primera escena habilitada en Build Settings; práctica se añade
como otra escena disponible. No hay carga automática entre ambas escenas.

## Recorrido de práctica

1. Menú inicial y botón Play.
2. Gronny se presenta y explica la jardinería sustentable: cultivar un tomate
   ahorrando agua, por lo que el jardín usa una reserva limitada.
3. Se aclara que el agua de práctica se repone libremente.
4. Demostraciones pausadas de movimiento, agarre, plantación y riego.
5. Al alcanzar el objetivo y enderezar la regadera, se indica Practice complete:
   seed watered. El riego se detiene y se conserva la maceta con tierra.

La práctica tiene cuatro pasos, sin lección de avance de fase. CalendarSystem
rechaza AdvancePhase en esta escena incluso después de cubrir el agua. No hay
cambio de día, brote, anuncio de brote, cosecha ni paso a reserva limitada en
práctica. Las demostraciones opcionales del menú siguen disponibles.

## Voz y aislamiento

La grabación ya generada para el objetivo sustentable se copia a
`Assets/Resources/GronnyAudio/PracticeWelcome.wav`. Su hash y procedencia están
en `output/PracticeFirstWateringAssets.json`. No se generaron voces adicionales
ni se sustituyeron los WAV aprobados del jardín principal.

GronnyTutorial crea una copia del catálogo exclusivamente durante la ejecución
de práctica y cambia su bienvenida por esta grabación. El catálogo guardado y el
saludo de Garden_Moves conservan sus referencias y textos. El reconocimiento
anterior coincide salvo el nombre propio Gronny/Granny; su pronunciación sigue
pendiente de escucha. Se mantienen los intervalos visuales de 2,5 s y la voz de
manos al 85 % de velocidad.

## Validación

Se revisaron estáticamente los guardas de escena, el bloqueo del reloj, las
condiciones de finalización, el aislamiento del catálogo y la integridad de
los audios. `Garden/Verify watered practice without growth` permite comprobar
la planta real después de terminar el riego y enderezar la regadera, durante Play
en Garden_Practice. Es una comprobación manualmente invocada: no se ha ejecutado.

La compilación y la prueba de este recorrido en Unity y Quest 3 están pendientes
por el límite anterior del revisor automático. Las 24 comprobaciones del antiguo
recorrido con crecimiento y cambio de escena no validan esta versión.
