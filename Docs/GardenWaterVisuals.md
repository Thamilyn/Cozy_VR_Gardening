# Efectos visuales de agua

Se usan los paquetes URP existentes. No se requieren texturas, Shader Graph ni paquetes nuevos.

## Chorro de la regadera

`Assets/Resources/GardenWater/WaterStream.shader` añade transparencia, bordes suaves y reflejos estilizados que se desplazan desde la salida de la regadera hacia el impacto.

`WateringCan` carga automáticamente `GardenWater/WaterStream.mat` cuando el LineRenderer no tiene un material asignado. Una referencia explícita en `streamMaterial` tiene prioridad. Se conserva un material ya asignado al LineRenderer si no se indica una sustitución.

La anchura y el color siguen siendo configurables en WateringCan. El material permite ajustar Flow speed, Highlights per metre y Highlight. Las UV del chorro se repiten por distancia para evitar que la velocidad visual dependa de su longitud.

La detección del impacto, el volumen aplicado y las condiciones para empezar o detener el riego mantienen su lógica existente.

## Charco por exceso

`Assets/Resources/GardenWater/WaterPuddle.shader` representa agua acumulada con borde circular suave, ondas lentas y un brillo dependiente del ángulo de vista.

Al recibir el estado de exceso desde SeedsController, GardenSoil crea una superficie de dos triángulos sobre el suelo y carga `GardenWater/WaterPuddle.mat`. La superficie es hija de la tierra, sigue la maceta y no tiene collider. Su tamaño se calcula desde los límites del mesh de tierra; este efecto está pensado para las superficies planas de las macetas actuales.

La tierra conserva el color húmedo. El color Waterlogged color se aplica al agua acumulada. El offset de 1,5 mm se convierte a unidades locales porque Ground tiene una escala vertical muy pequeña.

El charco aparece al superar el límite de la fase. El modelo existente drena tras cinco segundos sin aportes; cuando comunica que ha drenado, el charco se desvanece en 0,8 segundos. El aviso del panel conserva el exceso ocurrido hasta cambiar de fase. Al avanzar o reiniciar se limpia el estado visual de exceso.

Los materiales están en Resources y referencian sus shaders para incluirlos como recursos de la aplicación. Se comparten los materiales; los parámetros del charco se aplican por renderer con MaterialPropertyBlock. Ambos shaders tienen una sola pasada transparente y usan las macros de instancing y estéreo de Unity. Son efectos estilizados, sin refracción ni reflejos de pantalla.

## Validación pendiente en Unity y Quest 3

Se revisaron las referencias y la lógica de forma estática. No se ejecutaron Unity, compilación de shaders, pruebas automatizadas ni una compilación Android.

Comprobar en Garden_Moves:

1. Que ambos shaders importan sin errores ni materiales rosas.
2. Que ambas regaderas muestran el chorro y que termina en el impacto detectado.
3. Que el chorro desaparece al enderezar o soltar la regadera, vaciarla o avanzar el tiempo.
4. Que alcanzar exactamente el límite sigue en rango y que superarlo muestra charco y aviso rojo.
5. Que al dejar de regar se produce el drenaje existente, el charco se desvanece y el aviso pasa a ámbar.
6. Que mover la maceta mantiene el charco dentro de la tierra, sin parpadeo de superficies ni nuevas colisiones.
7. Que cambiar de fase y reiniciar limpia el exceso.
8. En Quest 3, comprobar ambos ojos, la visibilidad sobre la tierra y el coste de transparencia con el perfilador habitual del proyecto.
