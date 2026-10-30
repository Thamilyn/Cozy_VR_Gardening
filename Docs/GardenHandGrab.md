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

## Recuperación automática de la jarra

`WateringCan` deja la jarra donde el jugador la apoya sobre la mesa. Después de
haberla agarrado, solo inicia la recuperación si está suelta y su centro de masa
queda al menos 45 cm por debajo del centro de masa en la pose de retorno. Si
permanece por debajo durante dos segundos de juego activo, vuelve al origen y
queda inmóvil hasta recogerla; entonces recupera sus restricciones físicas
originales. Volver a agarrarla o subirla por encima del límite cancela la espera.
El temporizador se detiene mientras el jardín está pausado.

`Recovery Delay Seconds` ajusta la espera, `Minimum Recovery Drop` define la
caída mínima y un `Recovery Point` independiente permite elegir otra pose de
retorno accesible a la altura de la mesa. La detección usa la altura del centro
de masa, no una colisión con el suelo ni la distancia horizontal al origen. Se
comprueba continuamente para detectar una jarra que se desliza del borde después
de haber descansado sobre la mesa. Las semillas conservan su retorno anterior.
La recuperación conserva el agua de la jarra y la reserva de la sesión. No
vierte agua mientras espera un nuevo agarre. El calendario ya no ofrece
recuperación manual. El retorno automático usa la capa `GardenHeldObject`
mientras la jarra espera, para que no se convierta en un apoyo del jugador.

Revisión estática realizada; quedan pendientes la compilación y la comprobación
en Play y Quest 3 de estos casos:

1. Apoyarla en distintos puntos de la mesa, de pie y de lado, y esperar más de
   dos segundos: permanece allí sin retornar.
2. Dejarla caer de la mesa: al superar la caída mínima, empieza la espera de dos
   segundos y después vuelve conservando su agua. Recogerla antes cancela el retorno.
3. Recogerla después del retorno, regar y soltarla de nuevo: recupera la física,
   el riego responde al agarre y la inclinación; solo retorna si vuelve a caer.
4. Pausar durante la espera o soltarla con el menú abierto: el tiempo solo
   avanza al volver al jardín.
5. Abrir el calendario: solo aparecen los controles de avance, relleno y
   reinicio; la recuperación automática sigue funcionando sin cambiar el agua.
6. Transferirla entre manos: no se programa el retorno mientras alguna la sostiene.
7. Dejarla en el borde y permitir que se deslice después: se detecta la caída
   aunque hayan pasado más de dos segundos desde la suelta.

## Recuperación de semillas sueltas

`SeedItem` intenta plantar al soltar la semilla, como antes. Si no se planta,
espera dos segundos de juego activo y la devuelve a su posición y orientación
iniciales, anulando la velocidad lineal y angular. Allí permanece inmóvil hasta
un nuevo agarre real de Meta, que restaura sus restricciones físicas originales.
Volver a agarrarla antes del retorno cancela la recuperación. Una semilla
plantada no se recupera ni se separa de su maceta.

El Inspector permite ajustar `Recovery Delay Seconds` y asignar un
`Recovery Point` independiente de la semilla, colocado en una zona accesible.
Sin referencia se utiliza la pose al comenzar la partida. El retorno reutiliza
la misma semilla y su identificador; no crea copias ni reinicia el tutorial.
El temporizador se detiene durante el menú. Una suelta durante la pausa solo
programa la recuperación al volver al jardín y no intenta plantar.

Revisión estática realizada; quedan pendientes la compilación y la comprobación
en Play y Quest 3 de estos casos:

1. Soltar fuera de la maceta y dejar que ruede: vuelve a los dos segundos y
   permanece disponible para ambas manos y los mandos.
2. Recogerla antes del retorno: continúa en la mano sin saltar al origen.
3. Recogerla después del retorno y soltarla otra vez: recupera la física y
   vuelve a ejecutar el mismo ciclo de recuperación.
4. Soltar sobre tierra válida: se planta y permanece unida a la maceta después
   de esperar más de dos segundos.
5. Pausar durante la espera o soltarla con el menú abierto: no retorna durante
   la pausa ni se planta al reanudar; después vuelve a su punto de recuperación.

## Comprobación en Quest

1. Abra `Garden_Moves`, entre en Play y active el seguimiento de manos.
2. Recoja y suelte, con cada mano, Shovel, Rake, Pruner, Watering, bottle-oil,
   Basket_S, RiceBin, Sprout, Plant_Pot y las tres macetas PotSmall.
3. Compruebe que la tierra y el modelo de cada maceta la acompañan al moverla.
4. Recoja las semillas por pinza y suéltelas en las macetas. Mueva una maceta
   sembrada y avance el crecimiento: la planta debe continuar anclada.
5. Suelte una herramienta sobre una mesa y en el suelo: debe apoyarse y poder
   recogerse de nuevo. Vuelva a probar con el botón Grip de los mandos.

## Tomate del prototipo

En Garden_Moves se prepara un solo fruto de la fase madura con TomatoFruit y los agarres Meta de pinza, palma y mando. El modelo cosechable es RootswithTomatoes.036, conservando la malla original, sus dos materiales y el tallo. TomatoFruitMesh lo coloca bajo una raíz física centrada en el fruto. No sustituye el modelo por una esfera ni modifica los triángulos de la planta. El prefab provisional sigue usando Tomato_PLACEHOLDER_0.

Los 33 modelos de fruto de TomatPhasefinal.prefab tienen un diámetro máximo de 6,5 cm, medido sobre la superficie con material Tomato y excluyendo el tallo. El ajuste está guardado en el prefab y se aplica de nuevo después de alinear la planta con la maceta en Play. Al reducir cada modelo se conserva el extremo de unión de su tallo. Se dejaron las medidas en Docs/Validation/TomatoRipeModelSizes.json y un respaldo previo en .utmp/TomatPhasefinal.BeforeSizeFix.prefab.

El fruto cosechable se ajusta a 6,5 cm y tiene un contorno amarillo de 1,5 mm solo alrededor del tomate. El contorno usa una copia de la malla que dibuja únicamente sus triángulos Tomato, sin collider, sombras ni posprocesado, con soporte estéreo en URP. Para leer estos triángulos y encontrar el extremo del tallo se mantiene Read/Write en TomatPhasefinal.fbx. La copia se libera al destruir su objeto. El contorno permanece visible en la mata y se apaga al recogerlo. Select de Meta separa el modelo completo antes de iniciar la transformación de agarre, conservando su tamaño mundial. Los demás frutos permanecen visuales.

Soltar fuera de la bandeja permite que el tomate caiga y se pueda volver a recoger. La cosecha exige soltarlo dentro de COSECHA; entonces permanece visible e inmóvil, con gravedad, colliders y agarres de manos y mandos desactivados. StoreIn conserva su tamaño al hacerlo hijo de la caja y lo apoya en el fondo real: TomatoHarvestBasket lanza un rayo hacia abajo por el centro del volumen de cosecha, consultando únicamente los colliders sólidos de la caja. Se excluyen triggers y tomates ya entregados. RestOn alinea el vértice más bajo del modelo con esa superficie, dejando 0,5 mm de separación para evitar intersecciones. No se usa el origen de la caja ni se conserva una altura de suelta que podría dejarlo flotando. GardenGrabSetup respeta estos estados y vuelve a instalarse al reiniciar la escena. La ruta alternativa de seis prefabs de SeedItem usa índices de 0 a 5, incluido el fruto maduro.

El ajuste para apoyar el modelo en el fondo tiene revisión estática y una comprobación preparada en Garden > Verify ripe tomato harvest que compara el modelo con la superficie del collider Base. Su compilación y ejecución están pendientes: Unity CLI no encontró un Editor conectado al realizar este cambio. Los informes de Play anteriores verifican visibilidad y desactivación, pero no acreditan este nuevo apoyo en el fondo.

Validación de la entrega visible del 21 de octubre de 2026: recompilación sin errores ni advertencias y prueba en Play aprobada. Tras una suelta estacionaria simulada, el modelo seguía activo, con su renderer habilitado, dentro del volumen actual de cosecha y con diámetro de 0,065 m. Se verificaron Rigidbody inmóvil, gravedad desactivada, colliders desactivados y ambos tipos de agarre Meta desactivados. El informe está en Docs/Validation/TomatoHarvestVerification.txt; la comprobación física en Quest 3 sigue pendiente.

Corrección del 21 de octubre de 2026: se reprodujo un salto de 0,065 m a 0,319417 m al mover el fruto agarrado. GrabFreeTransformer había guardado restricciones de escala local mientras el tomate dependía de la planta; después de SetParent(null, true), el siguiente Move restauraba esa escala local antigua. TomatoFruit reinicializa ese transformer tras separarse y antes de BeginTransform, conservando la escala mundial correcta. También se restauró el cálculo proporcional del normalizador en lugar de una escala local fija de 0,2.

La regresión ahora mide el diámetro en siete estados: unido, seleccionado, movido en la mano, soltado fuera de la bandeja, recogido otra vez, movido a la bandeja y almacenado. Los siete dieron 0,065000 m; la recompilación terminó sin errores ni advertencias. La reproducción anterior está en Docs/Validation/TomatoGrabScaleBeforeFix.txt y el resultado corregido en Docs/Validation/TomatoHarvestVerification.txt. La prueba entrega el fruto en el centro del volumen de cosecha configurado actualmente. Estas medidas usan eventos simulados de Meta; las manos físicas en Quest 3 siguen pendientes.

Validación del 19 de octubre de 2026, con autorización: Unity CLI recompiló sin errores ni advertencias (Docs/Validation/TomatoHarvestCompilation.json). Garden > Verify ripe tomato harvest pasó con el modelo RootswithTomatoes.036, tamaño de todos los frutos maduros, collider exclusivo, componentes Meta, contorno, separación inmediata, suelta fuera de la bandeja, segunda recogida y entrega (Docs/Validation/TomatoHarvestVerification.txt). El prefab guardado contiene 33 frutos, con diámetro máximo de 0,065 m y desviación máxima de la unión del tallo menor de 0,001 mm respecto al modelo original. Esta prueba prepara directamente la fase madura y omite las lecciones del tutorial solo durante Play; no acredita el ciclo completo de crecimiento. Se revisaron renders aislados del modelo con tallo y contorno (Docs/Validation/TomatoHarvestOutline.png) y de la planta completa (Docs/Validation/TomatoRipePlant.png).

El verificador general Garden > Verify tomato runtime and restart se detuvo antes de la siembra al exigir GardenGrabSetup activo; la escena conserva ese helper desactivado. No se cambió su configuración para pasar la prueba. El Editor volvió a edición con los cambios sin guardar presentes; se conservó una copia previa en .utmp/TomatoHarvest_BeforeValidation.unity. El entorno de escritorio informa ErrorFormFactorUnavailable al inicializar OpenXR sin visor. Sigue pendiente probar pinza y palma reales con ambas manos, mandos, apariencia del contorno y rendimiento en Quest 3, además del ciclo completo y el reinicio.
