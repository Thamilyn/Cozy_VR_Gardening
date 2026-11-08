# Ajuste de comodidad del corte — 8 de noviembre de 2026

Se revisó la grabación `Grabación de pantalla 2026-11-08 044226.mp4`. El fruto termina separado y recogido, pero antes se observan varios intentos de colocación de las hojas junto a la planta. El detector anterior exigía 1,5 cm de precisión y 90 % de cierre, y evaluaba la posición en un único instante al completar ese cierre. El vídeo no proporciona las muestras de seguimiento necesarias para atribuir cada fallo a un umbral concreto.

## Cambios

- Pruner admite **2,5 cm** alrededor de la unión real. El prefab existente conserva su GUID y se actualiza únicamente ese valor serializado.
- Se abre con una presión/cierre de dedos de hasta **0,30** y se cierra visualmente con un gesto de **0,75**, evitando exigir el cierre máximo de los cuatro dedos. El corte sigue esperando al 90 % de cierre de la animación de hojas, con su suavizado existente de 0,12 s. Un gesto parcial de 0,60 no corta.
- Se recuerda una alineación válida observada **durante el cierre**, hasta **180 ms**, admitiendo como máximo **1 cm adicional** de deriva. La memoria no permite acercar hojas ya cerradas ni continuar tras cambiar de mano, pausar o perder seguimiento. Abrir reinicia la memoria del gesto.
- Se comprueba que el punto de las hojas más cercano al tallo quede fuera de la esfera física del fruto, con 2 mm de margen. La tolerancia mayor no convierte el cuerpo del tomate en una zona cortable.
- Un aro fino marca la unión en **ámbar** y se vuelve **verde** cuando las hojas están alineadas; la etiqueta cambia a **Close Pruner**. El marcador permanece visible aunque las hojas de la planta oculten el tallo. Es una guía visual, sin colliders, agarres ni consecuencias de juego. Se oculta al pausar y al cortar; una alineación no confirmada caduca en 120 ms.

El aro usa el shader URP `Resources/GardenHarvest/StemCutGuide`, con soporte de instancias y salida estéreo. Tiene 24 puntos precalculados y no crea arrays o materiales por fotograma. La búsqueda de frutos se hace al abrir y, si se mantienen abiertas las tijeras, como máximo cada 0,5 s. Apuntar/cerrar recorre la lista ya descubierta; no escanea la escena cada fotograma.

Se mantienen madurez real, cobertura de tierra, agarres de Meta, separación física, recogida, bandeja, voces y animaciones de UI. No se añade movimiento asistido de la mano ni de la cámara, ni haptics nuevos.

## Configuración

Los valores ya están aplicados en el prefab de Pruner y en los valores iniciales de GardenPruner. El Inspector permite ajustar **Cutting Radius**, **Squeeze To Cut**, **Squeeze To Open** y **Alignment Grace Seconds**. No hay referencias nuevas que asignar; el shader del aro se carga desde Resources. Sus archivos `.meta` nuevos los genera Unity al importar.

## Validación

- Recompilación con Unity 6000.3.23f1: cero errores y cero advertencias del compilador; se conserva el aviso independiente del CLI sobre nombres antiguos de Pipeline.
- **24 comprobaciones aprobadas** en `EasierStemCutRegression.cs`: valores heredados del prefab, shader, selección Meta de la herramienta, feedback verde/ámbar, falta de herramienta, estado verde, contacto con el fruto, hojas ya cerradas, gesto parcial, corte con margen de 22 mm y presión 0,75, escala, separación sin recogida/recompensa, deriva de 32 mm durante el cierre, caducidad a 200 ms, desplazamiento excesivo, pausa, pérdida de seguimiento, cambio de mano y ausencia de cosecha sin recoger/entregar.
- **17 comprobaciones de voz repetidas**: corte mediante Pruner, Select de Meta, entrega rápida, reconocimientos en orden, fuente única y ausencia de duplicados. Las muestras de gesto y seguimiento fueron sintéticas. Los plazos de caducidad se comprobaron modificando el tiempo registrado, no con sensores reales.
- Se renderizaron y revisaron `Docs/Validation/EasierStemCutVideo/AlignedGreen.png` y `TargetAmber.png` sobre los modelos reales. Las capturas incluyen elementos de la fixture temporal del menú; no son una validación del diseño completo de la escena.
- Informes: `Docs/Validation/EasierStemCutCompilation.json`, `EasierStemCutRegression.txt`, `EasierStemCutRegressionResult.json` y `EasierStemCutVoiceResult.json`.

La escena inicialmente abierta era `Garden_Moves_Test 2`, sin el sistema completo de cultivo usado por la fixture. La primera preparación no llegó a comprobar el corte. Unity sufrió un cierre nativo al salir de ese Play; se guardó `EasierStemCutEditorCrash.txt`. Se recuperó el Editor con la misma versión y las pruebas aprobadas se ejecutaron después en `Garden_Moves`, conservando la configuración original de escenas para restaurarla al terminar. No se guardaron modificaciones temporales de las pruebas.

Pendiente en Meta Quest 3: confirmar que estos márgenes resultan más cómodos con las manos y mandos físicos, legibilidad del aro entre las hojas, tiempos reales, ausencia de falsos positivos y coste de renderizado. Las pruebas sintéticas no demuestran una mejora subjetiva ni la estabilidad general del Editor. No se generó APK, instalaron paquetes ni actualizaron dependencias.
