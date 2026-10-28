# Indicador de agua sobre la planta

El panel de Gronny conserva las instrucciones y los controles. Sobre cada tomate plantado solo aparecen una barra de agua y un mensaje breve, sin cantidades ni estadísticas. La barra conserva el fondo blanco redondeado con segmentos de agua azul cian.

## Integración

`SeedsController.TryPlant` añade e inicializa `PlantWaterProgress` en la semilla de tomate después de registrar la siembra y crear su visual. No es necesario modificar la escena ni conectar componentes manualmente. El indicador usa el `PlantSave` existente y se actualiza con `CalendarSystem.Changed`, que ya comunica riego, drenaje, avance y cosecha. No registra agua por separado ni cambia los objetivos o el diario.

El llenado representa `retainedWaterMillilitres / TargetMillilitres(plant)`. Los doce segmentos admiten llenado parcial: no se redondea la cantidad a bloques completos. Al llegar al objetivo la barra está llena y aparece `Enough water`. Superar el límite vuelve los segmentos rojos y muestra `Stop watering`. Después del drenaje se muestran en ámbar hasta cambiar de fase y el mensaje vuelve a ser `Enough water`.

Debajo de la barra aparece un único mensaje: `Water the soil`, `Enough water`, `Stop watering`, `Watering paused` o `Ready to harvest`, según el estado. Si falta la configuración del objetivo aparece `Water target unavailable`; si se agota el agua del intento, `Try again`. Se mantienen el idioma inglés del juego y los datos internos de riego, drenaje y consumo, pero el indicador ya no muestra mililitros, límites, totales, reserva ni desperdicio. Al avanzar se vacía la barra. En madurez se muestra completa con la indicación de cosecha. Al registrar la cosecha muestra `Harvest complete` si hay un suministro de agua activo; de lo contrario se oculta. Reiniciar la escena elimina sus objetos y datos.

La posición sigue el punto de siembra y la parte superior de los renderers activos de la planta. Los frutos separados durante la cosecha no arrastran la barra. Gira horizontalmente hacia `OVRCameraRig.centerEyeAnchor`, con `Camera.main` como alternativa. El ancho de la barra es de 22 cm; `barWidthMetres`, `heightAbovePlant` y los colores son configurables en `PlantWaterProgress`. Si se desea persistir otros ajustes, puede añadirse este componente al prefab de semilla de tomate: la integración reutiliza el componente existente.

El Canvas usa espacio de mundo y no incluye raycaster ni collider. El gráfico dibuja el fondo y los segmentos con geometría UI y el material compartido de Unity, sin texturas o paquetes nuevos. El alcance es el ciclo de tomate del prototipo; rábano y lechuga mantienen su modelo anterior.

La creación de la barra y su etiqueta se comparte mediante `GardenWaterIndicator`
con el tanque y las regaderas. Los tres indicadores usan las mismas proporciones,
tipografía, sombra y colores predeterminados; la planta conserva sus ajustes de
color serializados. El tanque muestra solo su reserva y cada regadera su contenido
real. Véase `AhorroAgua.md` para su integración y las comprobaciones pendientes.

## Verificación

La simplificación se revisó estáticamente: el indicador crea una sola etiqueta, conserva el cálculo y los colores de la barra y no modifica los datos de riego ni los recursos serializados. No se ejecutaron recompilación ni pruebas en Unity para este cambio.

No hay todavía una captura validada del resultado simplificado en ejecución.

Comprobaciones pendientes en `Garden_Moves`:

1. Antes de sembrar, ninguna maceta vacía debe tener indicador; al sembrar debe aparecer solo sobre la maceta elegida, inicialmente vacía y con `Water the soil`.
2. Regar hasta aproximadamente la mitad: los segmentos deben seguir solo el agua que alcanza el sustrato. Verter fuera de la maceta no debe incrementarlos. El indicador debe mostrar siempre una sola etiqueta, sin cifras ni estadísticas de reserva.
3. Llegar al objetivo: barra completa y `Enough water`. Superar el límite: rojo y `Stop watering`. Tras cinco segundos sin riego: ámbar y `Enough water`, manteniendo los totales aplicados en los datos internos.
4. Avanzar de fase: vaciar el llenado y colocar el indicador encima de la nueva planta. Durante la transición debe indicar `Watering paused`.
5. Mover y girar la maceta; caminar alrededor y cambiar la altura de la cabeza. Comprobar seguimiento, orientación, lectura de todos los textos y que no se bloquean los agarres o el chorro.
6. Llegar a madurez, recoger el fruto y reiniciar: comprobar `Ready to harvest`, `Harvest complete` con suministro activo (u ocultación sin suministro) y ausencia de indicadores duplicados al volver a sembrar. Agotar el suministro antes de completar la planta debe mostrar `Try again`.
7. Validar legibilidad estereoscópica, oclusión por regadera/hojas y rendimiento en Meta Quest 3.
