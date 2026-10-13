# Indicador de agua sobre la planta

El panel de Gronny conserva las instrucciones y los controles. Las cantidades se muestran sobre cada tomate plantado, siguiendo la referencia visual de fondo blanco redondeado con segmentos de agua azul cian.

## Integración

`SeedsController.TryPlant` añade e inicializa `PlantWaterProgress` en la semilla de tomate después de registrar la siembra y crear su visual. No es necesario modificar la escena ni conectar componentes manualmente. El indicador usa el `PlantSave` existente y se actualiza con `CalendarSystem.Changed`, que ya comunica riego, drenaje, avance y cosecha. No registra agua por separado ni cambia los objetivos o el diario.

El llenado representa `retainedWaterMillilitres / TargetMillilitres(plant)`. Los doce segmentos admiten llenado parcial: no se redondea la cantidad a bloques completos. Al llegar al objetivo la barra está llena; si se sigue regando, las cifras continúan aumentando. Superar el límite vuelve los segmentos rojos y muestra `TOO MUCH WATER`. Después del drenaje se muestran en ámbar con `EXCESS WATER DRAINED` hasta cambiar de fase.

Encima de la barra aparece agua retenida / objetivo en ml. Debajo aparecen el estado, el último vertido (`Pour`), el agua aplicada durante la fase (`Stage`), el límite (`Limit`) y el total aplicado (`Total`). El drenaje reduce solo la cantidad retenida: los contadores de agua aplicada conservan el exceso ocurrido. Al avanzar se vacían la barra y los contadores de fase/vertido; el total permanece. En madurez se muestra la barra completa, el total y la indicación de cosecha. Al registrar la cosecha se oculta el indicador; reiniciar la escena elimina sus objetos y datos.

La posición sigue el punto de siembra y la parte superior de los renderers activos de la planta. Los frutos separados durante la cosecha no arrastran la barra. Gira horizontalmente hacia `OVRCameraRig.centerEyeAnchor`, con `Camera.main` como alternativa. El ancho de la barra es de 22 cm; `barWidthMetres`, `heightAbovePlant` y los colores son configurables en `PlantWaterProgress`. Si se desea persistir otros ajustes, puede añadirse este componente al prefab de semilla de tomate: la integración reutiliza el componente existente.

El Canvas usa espacio de mundo y no incluye raycaster ni collider. El gráfico dibuja el fondo y los segmentos con geometría UI y el material compartido de Unity, sin texturas o paquetes nuevos. El alcance es el ciclo de tomate del prototipo; rábano y lechuga mantienen su modelo anterior.

## Verificación

Se revisaron estáticamente el enlace tras la siembra, las notificaciones del calendario, la separación entre agua retenida/aplicada, los reinicios de fase, la limpieza de suscripciones, las referencias `.meta` nuevas y la ausencia de interacción en la barra.

Unity CLI 1.0.0-beta.12 no detectó ningún Editor conectado por Pipeline. No se inició Unity ni se ejecutaron recompilación, pruebas automatizadas o compilación Android. No hay todavía una captura validada del resultado en ejecución.

Comprobaciones pendientes en `Garden_Moves`:

1. Antes de sembrar, ninguna maceta vacía debe tener indicador; al sembrar debe aparecer solo sobre la maceta elegida, inicialmente vacía y con `0.0 / objetivo ml`.
2. Regar hasta aproximadamente la mitad: los segmentos y las cifras deben seguir solo el agua que alcanza el sustrato. Verter fuera de la maceta no debe incrementarlos. El panel de Gronny no debe mostrar cantidades, tampoco al intentar avanzar sin suficiente agua.
3. Llegar al objetivo: barra completa y `WATER IN RANGE`. Superar el límite: rojo, aviso de exceso y cifras sin recorte. Tras cinco segundos sin riego: ámbar, cantidad retenida en el objetivo y totales aplicados sin reducción.
4. Avanzar de fase: vaciar el llenado, actualizar el objetivo y colocar el indicador encima de la nueva planta. Durante la transición debe indicar `WATERING PAUSED`.
5. Mover y girar la maceta; caminar alrededor y cambiar la altura de la cabeza. Comprobar seguimiento, orientación, lectura de todos los textos y que no se bloquean los agarres o el chorro.
6. Llegar a madurez, recoger el fruto y reiniciar: comprobar estado de cosecha, ocultación y ausencia de indicadores duplicados al volver a sembrar.
7. Validar legibilidad estereoscópica, oclusión por regadera/hojas y rendimiento en Meta Quest 3.
