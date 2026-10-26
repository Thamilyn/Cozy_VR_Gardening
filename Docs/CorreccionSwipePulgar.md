# Deslizamiento visible del pulgar

Corrección del 26 de octubre de 2026 solicitada al detectar que faltaba la
demostración de «swipe the thumb» para avanzar.

Revisada tras el feedback del usuario: la primera versión abría demasiado el
pulgar y su flecha confundía. Esa presentación se ha retirado.

## Cambio

La demo anterior alternaba pequeñas rotaciones de una articulación del pulgar,
interpoladas durante `blendSeconds` después de cambiar de paso. Se han añadido
clips dedicados de las articulaciones del pulgar y una interpolación que recorre
todo el intervalo de la instrucción de deslizamiento.

- Los clips `ThumbSwipeLeft.anim` y `ThumbSwipeRight.anim` conservan sus GUID. Ahora
  se generan a partir de la geometría y las poses del modelo Meta existente: la
  yema recorre un tramo corto del lateral del índice doblado. El cálculo de la
  pose se hace al crear el recurso en el Editor, no durante el juego.
- Solo se animan articulaciones del pulgar. El índice mantiene la misma pose y
  el recorrido se invierte al mostrar el paso atrás; no se mueve toda la mano.
- El paso adelante se muestra entre 5,8 y 7,0 s; el paso atrás entre 8,3 y 9,5 s.
  Se conservan los tiempos del catálogo y el WAV actual de 13,28 s. La futura
  sustitución de ese WAV requiere volver a sincronizar los pasos.
- Se han eliminado tanto la flecha como la esfera de la punta. La demostración
  muestra la mano y su instrucción breve, con pausa al final del deslizamiento.
- La mano activa de esta demostración sigue siendo la izquierda. Preparar un
  clip derecho no habilita por sí mismo movimiento ni instrucciones derechas.
- `GardenThumbSwipeSetup` permite reconstruir los clips y conectar el gesto
  sin reemplazar voces ni sobrescribir los tiempos editados del catálogo.

No se modificaron entradas del rig, voces, escenas, prefabs Meta ni paquetes.

## Verificación

Compilación mediante Unity CLI y Unity 6000.3.23f1: sin errores ni advertencias de
C#. Resultado en `output/ThumbSwipeRevisionCompileFinal.json`.

El comprobador `GardenThumbSwipeVerification` ejecuta una sesión temporal de Play
y revisa bindings, recorrido corto de la yema durante el intervalo, índice
inmóvil, recorrido inverso, repetición sin acumulación, pausa y ausencia de flechas.
Muestrea también la distancia entre la yema y el eje del índice para comprobar que
permanezca junto a su superficie. Comprueba la deformación de la malla de la mano
y que la demostración no cambie el inicio de sesión ni el diario del cultivo.
El recorrido anterior de aproximadamente 6,9 cm se ha reducido a unos 2 cm.
El resultado de estas verificaciones técnicas no equivale a aprobación de claridad
por parte del usuario: la primera versión ya superaba verificaciones y resultaba confusa.

La captura de fases utiliza una pose temporal de cámara a altura de ojos y una
malla horneada por muestra para evitar que Unity reutilice el skinning de un mismo
frame en las cuatro imágenes. No cambia el renderizado del juego.

- Informe: `output/ThumbSwipeVerification.txt`.
- Inicio, deslizamiento, final y regreso: `output/ThumbSwipeFrames.png`.
- Vista previa animada, si el codificador local de Unity está disponible:
  `output/ThumbSwipePreview.mp4` (sin audio).
- Para repetir: menú de Unity `Garden/Gronny/Verify visible thumb swipe in Play`.

La sesión termina fuera de Play y no guarda la escena. Sigue pendiente comprobar
en Quest 3 la claridad a escala real y que una persona pueda imitar el gesto con
éxito. Las pruebas sintéticas no certifican el reconocimiento físico del gesto.
