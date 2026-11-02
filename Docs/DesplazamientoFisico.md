# Guía por voz: gardening table y semilla de tomate

En Garden_Moves, después de Play y de la bienvenida, Gronny dice:

> Walk towards the gardening table. Stay inside your clear play area. When you
> get there, look for the seed labelled Tomato seed. I'll show you how to pick it up.

Al terminar la frase, enlaza con las instrucciones existentes para agarrar la
semilla con manos o mandos. El rótulo de la semilla pasa de Tomato a Tomato seed.
El movimiento físico sigue a cargo del seguimiento del visor que ya estaba
activado. La guía no mide pasos, distancias ni llegada a la mesa.

El jugador empieza en (0, 0, -3.6), aproximadamente a 1,1 m del borde frontal
de la mesa. Antes empezaba en z = -7.88, a unos 5,3 m. Se conserva la orientación
hacia la mesa y se comprueba el espacio para el jugador y el menú de inicio.

La grabación usa la voz Kiki local, en inglés como las voces existentes.
El guion está en Docs/GronnyRoomScaleDialogues.json y el WAV en
Assets/Audio/roomscale/roomscale_walk.wav. El catálogo y el panel muestran la
misma acción. Se conservan los audios y controles anteriores.

Controls > Walk in your room permite escuchar, detener y repetir la indicación.
Las demostraciones opcionales de manos y joystick siguen accesibles desde ahí.
Revisar la voz en el menú no avanza el tutorial. Pausar conserva el punto de
la narración para continuar al volver.

Las comprobaciones se guardan en output/RoomScaleAudioVerification.json,
output/WelcomeMovementPlay.txt, output/MenuPlayVerification.txt,
output/RoomScaleCompile.json y output/RoomScaleCatalogue.json.
Unity 6000.3.23f1: compilación sin errores ni advertencias de C#, catálogo válido,
14 comprobaciones de la secuencia de voz y rótulo correctas, y 99 del menú
correctas antes del ajuste de posición inicial. Tras acercar al jugador, la
compilación volvió a pasar y se verificaron el inicio sin obstáculos, el espacio
del menú y la secuencia de voz/pausa. La prueba completa se detuvo después, en
`Synthetic held can pours before menu suspension`. El diagnóstico posterior
confirmó `held=True`, `tilted=True`, agua disponible, menú cerrado y permiso de
verter, pero `hmd=True` y `tracking=False`. El agarre sintético no proporcionó
una fuente de seguimiento válida con XR activo; la protección de WateringCan
suspendió correctamente el vertido y exigió volver a la posición vertical.
Esa parte requiere una ejecución de escritorio sin XR activo o comprobar un
agarre real con el visor. Informes del ajuste:
output/GardenCloserSpawnValidation.json y output/GardenCloserSpawnCompile.json.
La transcripción automática del audio solo difiere en la grafía
equivalente labelled / labeled. La grabación dura 9,16 s y no presenta muestras
recortadas.
La escucha y el uso físico en Quest 3 requieren una comprobación en el visor.
