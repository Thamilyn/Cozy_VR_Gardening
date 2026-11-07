# Tijeras de poda

## Cambios

- `Pruner.prefab` incorpora `GardenPruner` en el objeto exterior. Las instancias existentes heredan el comportamiento sin modificar las escenas ni el FBX.
- Al entrar en Play, se crean un Rigidbody y las interacciones de agarre de Meta, con una envolvente convexa para cada pieza móvil.
- Las tijeras se pueden tomar desde el menú inicial y durante las primeras lecciones de Gronny. El bloqueo de los menús de pausa posteriores continúa aplicándose.
- Mandos: mantener Grip para sujetar; soltar el gatillo índice para abrir y apretarlo para cerrar. Solo se consulta el mando que sostiene las tijeras.
- Manos: tomar con un agarre de palma. Cerrar el puño cierra las hojas; abrir los cuatro dedos principales las abre y conserva el agarre.
- Para soltar con manos: abrir la mano con la palma orientada hacia abajo sobre una mesa. `PrunerHandGrab` exige una orientación a un máximo de 45 grados de la vertical, una superficie de mesa a 45 cm como máximo y 0,15 segundos de gesto estable. Estos valores se pueden ajustar en el prefab.
- El componente usa las direcciones de palma izquierda/derecha de Meta y un override de su condición de liberación por selección; no sustituye el seguimiento ni el movimiento de Meta. La pérdida de datos válidos no se interpreta como un gesto de soltar; Meta conserva sus cancelaciones normales.
- Las dos piezas giran desde sus pivotes importados, con transición proporcional de 0,12 segundos y recorrido máximo de 22 grados por pieza.
- Un cierre completo cerca del tallo desprende un único tomate maduro. Mantener las hojas cerradas no repite el corte; hay que abrirlas de nuevo.
- El tomate conserva su escala y cae con gravedad. Puede recogerse y colocarse en la bandeja existente, que registra la cosecha. Tras un corte, vuelve a la planta si no se recoge en ocho segundos; después de sujetarlo vuelve a usar el plazo de recuperación existente.
- Las hojas pueden animarse mientras el menú está visible, sin cortar. Se bloquea el corte durante el menú y cuando no hay agarre o seguimiento válido. Tras pausar, cambiar de mano o perder seguimiento, hay que abrir antes del siguiente corte.

## Revisión estática realizada

- Unity del proyecto: 6000.3.23f1. Meta XR SDK: 205.0.0. Sin cambios de versiones ni dependencias.
- Inspección del FBX binario: `BottomPart` y `TopPart` comparten el origen de bisagra; las hojas se extienden hacia Z positivo y los mangos hacia Z negativo. La rotación corresponde al eje Y.
- Contraste de las API utilizadas con el código de Meta SDK instalado: agarres seleccionados, entrada analógica del gatillo, reglas de agarre y articulaciones de la mano.
- Comprobada la referencia del nuevo componente en el prefab y su herencia en `Garden_Moves`.
- La lógica de entrega en bandeja y contabilización se conserva en `TomatoHarvestBasket`. El componente `TomatoFruit` se instala únicamente en la etapa madura.
- `unity version --format json`: CLI 1.0.0-beta.12.
- `unity status --format json` y `unity list --project-path ... --format json` no encontraron una instancia conectada de Pipeline.

## Validación en Unity autorizada y realizada

- Se restableció la conexión del servidor local de Pipeline ya instalado, sin actualizar paquetes.
- `unity recompile --project-path ... --timeout 60 --format json`: compilación completada, cero errores y cero advertencias. Resultado: `PrunerCompilation.json`.
- Se ejecutó `PrunerRuntimeCheck.cs` mediante `unity command run_script` en una sesión de Play pausada de `Garden_Moves`, usando sus tijeras, semilla, maceta, fruto y bandeja reales.
- Las muestras de agarre, cierre y cambio de mano se inyectaron en `UpdateBlades`; no se simularon sensores ni mandos físicos. La recogida del fruto utilizó eventos de selección, movimiento y liberación de Meta.
- Pasaron las comprobaciones de inicialización, un único Rigidbody, referencias de agarre, dos colliders convexos, animación de ambas hojas, rechazo de fruto verde, corte de tallo próximo, cierre parcial, reapertura obligatoria, cambio de mano, pérdida de seguimiento, pausa, desprendimiento, escala, recuperación, recogida y una única cosecha al entregar en la bandeja.
- La recuperación se comprobó forzando la expiración del contador; no se esperaron los ocho segundos en tiempo real.
- Revisadas visualmente las capturas renderizadas `PrunerOpen.png` y `PrunerClosed.png`.
- Informe: `PrunerRuntimeVerification.txt`. Resultado completo del comando: `PrunerRuntimeResult.json` (el campo interno `data.result.success` es `true`).
- Se salió de Play al terminar. `Garden_Moves` conserva `isDirty: false`, igual que antes de las pruebas; no se guardaron cambios de las pruebas en la escena.

### Ajuste del agarre desde el inicio

- `GardenInteractionSuspension` permite el agarre y evita congelar el Rigidbody de las tijeras mientras la sesión aún no ha comenzado.
- `GronnyTutorial` permite sus agarres de manos y mandos desde la primera lección.
- `PrunerStartupCompilation.json`: cero errores y cero advertencias.
- `PrunerStartupCheck.cs`, ejecutado en Play pausado: 15 comprobaciones superadas con el menú y el tutorial reales. Incluyen disponibilidad inicial, selección/movimiento/liberación mediante eventos Meta, continuidad durante las lecciones y suspensión/restauración al pausar posteriormente.
- Informes: `PrunerStartupVerification.txt` y `PrunerStartupResult.json`. No se validaron mandos ni manos físicos en Quest 3.

### Gestos de puño y suelta sobre la mesa

- `GardenPruner` usa el cierre medio de índice, medio, anular y meñique. Las tijeras arrancan abiertas y regresan a esa pose al quedar libres.
- `PrunerHandGrab` se incluye en el prefab y conserva la selección de Meta al abrir la mano. Libera únicamente con los cuatro dedos abiertos y la palma hacia la mesa durante el intervalo configurado.
- La mesa actual usa un collider sólido en `TableSet`; también se reconocen jerarquías `Table` y `Mesa`, o una referencia explícita en `releaseTable`. Los objetos sólidos que obstruyen la palma impiden la suelta.
- `PrunerGestureCompilation.json`: compilación completada con cero errores y cero advertencias del compilador. El CLI instalado es ahora 1.0.0-beta.17 y avisa de los nombres antiguos de Pipeline; no se actualizó ningún paquete.
- `PrunerGestureCheck.cs`: 29 comprobaciones superadas con articulaciones `IHand` sintéticas, el predicado de liberación del SDK y el collider real de la mesa. Se comprobaron puño, apertura sin soltar, orientaciones de ambas manos, distancia, obstrucciones, pérdida de datos válidos y limpieza del override.
- El inicio y final del agarre de mano se suministraron al componente durante la prueba; no se comprobó la búsqueda automática de candidatos del SDK con sensores físicos. El intervalo de suelta se comprobó avanzando el tiempo registrado, sin esperar físicamente 0,15 segundos.
- Se repitieron y superaron las 15 comprobaciones de agarre inicial y las 32 comprobaciones de animación/cosecha.
- Informes nuevos: `PrunerGestureVerification.txt` y `PrunerGestureResult.json`. Al finalizar se salió de Play; la escena quedó con `isDirty: false`.

## Validación pendiente en Meta Quest 3

1. Confirmar el agarre con cada mando, que solo su gatillo mueve las hojas y que hay que abrir antes de cortar si se recoge con el gatillo apretado.
2. Comprobar con manos el agarre por puño, la apertura/cierre de los cuatro dedos y la permanencia del objeto en la mano abierta.
3. Cortar un tomate maduro, recogerlo y depositarlo en la bandeja; revisar la tolerancia de 1,5 cm y la comodidad del recorrido.
4. Probar la suelta con mano abierta y palma hacia la mesa, ajustar distancia/ángulo/intervalo para comodidad y confirmar que abrirla en otras orientaciones conserva el agarre.
5. Comprobar colisiones, caída física, recuperación tras ocho segundos reales, cambio de mano, menú y pérdida real de seguimiento.
6. Validar rendimiento y comportamiento en el dispositivo. No se generó APK.
