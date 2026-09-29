# Recoger objetos con las manos

`GardenGrabSetup` prepara los objetos sueltos de `Garden_Test`, `Garden_Moves` y
`Garden_Moves - Copy` al entrar en Play. Cada objeto tiene `Grabbable`,
`GrabInteractable` para mandos y `HandGrabInteractable` para manos, con agarre por
pinza o palma. El rig Meta existente proporciona los interactores de ambas manos.

Incluye las herramientas, regaderas, botella, cestas, recipiente de arroz,
macetas, semillas y plantas sueltas presentes en las escenas. Las copias y los
objetos que aparecen durante la partida también se preparan. El suelo, mesas,
arquitectura y rig permanecen como elementos del escenario.

Los modelos hijos de una maceta se mueven junto con ella. Una semilla sembrada
desactiva ambos tipos de agarre y permanece unida a su maceta al crecer.
Se conservan los triggers de siembra y se añaden colisiones físicas donde faltan.
Los MeshCollider se hacen convexos en ejecución para permitir el movimiento con
Rigidbody y conservar las referencias de los interactores existentes.

Para añadir otro tipo de objeto, asigne su nombre a `grabbableObjectNames` del
prefab `Assets/Resources/GardenGrabSetup.prefab`, o añada un `Grabbable` al objeto.

## Regadera Watering

En ambas escenas Garden_Moves, la regadera tiene gravedad y recupera la física
al soltarla. Su cuerpo usa una caja con la base alineada con el modelo; durante
Awake se añaden colliders separados para el tubo y la cabeza de la boquilla.
Esto evita el casco convexo que rellenaba los huecos del modelo completo.

`WateringCan` ubica `Water Spout` en la cara delantera de WateringCup.fbx, justo
fuera de la boquilla, respetando la escala y orientación de importación. El
LineRenderer y la detección del riego utilizan ese mismo punto. Una referencia
`spout` asignada manualmente en el Inspector sigue teniendo prioridad.

La pinza de la regadera usa `DefaultPinchRule` con `AllReleased`: abrir uno de
los dedos mientras otro mantiene la pinza no fuerza la suelta. `Slippiness` es
cero. La suelta sigue produciéndose al liberar la pinza completa; la pérdida de
seguimiento de manos puede interrumpir la interacción igualmente.

Salga de Play y vuelva a entrar para cargar los colliders y la física nuevos.
Suelte la regadera en el aire y sobre una mesa; luego agárrela e inclínela para
comprobar que el agua sale de la boquilla y alcanza la maceta.

Mientras se sostiene, sus colliders pasan a la capa `GardenHeldObject`. Las
consultas de movimiento, suelo y penetración de paredes del rig Meta excluyen
esta capa: la regadera no puede convertirse en el suelo del jugador al acercarla
al cuerpo, lo que provocaba saltos de cámara. Las colisiones con mesas, suelo y
macetas siguen activas. Al liberar la última mano, cancelar el agarre o desactivar
el componente, se restauran las capas originales de cada collider.

## Comprobación en Quest

1. Abra `Garden_Moves`, entre en Play y active el seguimiento de manos.
2. Recoja y suelte, con cada mano, Shovel, Rake, Pruner, Watering, bottle-oil,
   Basket_S, RiceBin, Sprout, Plant_Pot y las tres macetas PotSmall.
3. Compruebe que la tierra y el modelo de cada maceta la acompañan al moverla.
4. Recoja las semillas por pinza y suéltelas en las macetas. Mueva una maceta
   sembrada y avance el crecimiento: la planta debe continuar anclada.
5. Suelte una herramienta sobre una mesa y en el suelo: debe apoyarse y poder
   recogerse de nuevo. Vuelva a probar con el botón Grip de los mandos.
