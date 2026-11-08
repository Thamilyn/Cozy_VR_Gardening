# Agarre de Pruner con manos — 8 de noviembre de 2026

Pruner aceptaba únicamente `Palm`, con índice, corazón y anular obligatorios simultáneamente. Además, la proximidad se calculaba sobre las mallas convexas estrechas de los mangos y las hojas.

## Cambio

- Se admiten `Pinch | Palm` mediante las inyecciones existentes de `HandGrabInteractable`. La pinza conserva la regla estándar de Meta; el agarre de palma requiere cerrar el corazón, con índice y anular opcionales. Una mano abierta no inicia una selección.
- Se crea un `BoxCollider` trigger alrededor de los mangos, con **18 mm de margen por lado**. Se transforma la mitad del modelo situada detrás de la bisagra al espacio de la raíz y se convierte el margen de metros a escala local. El volumen se prepara antes de que Meta almacene sus colliders en `Start`.
- El volumen pertenece al mismo Rigidbody. Se conservan las dos mallas convexas sólidas, masa, gravedad, interpolación y detección de colisiones existentes. El volumen no amplía el alcance de corte.
- Meta sigue seleccionando y moviendo la herramienta. El gesto existente mantiene Pruner al abrir la mano para manejar las hojas y permite soltarlo con la palma abierta hacia abajo sobre la mesa. No se fuerza la selección ni se modifica el rig de manos.

Para cogerlo, acercar pulgar e índice a los mangos y hacer pinza, o rodear los mangos y cerrar la mano. Después de cogerlo, abrir los dedos abre las hojas; volver a cerrar ejecuta el gesto de corte existente.

## Inspector

No hay referencias nuevas que asignar ni cambios de escena/prefab necesarios. `GardenPruner > Handle Grab Padding` permite ajustar el margen (valor inicial **0.018 m**, rango 0.005–0.03 m). El hijo `Pruner handle pickup` se genera automáticamente en Play y no necesita guardarse en el prefab. La tolerancia acompaña al objeto al moverlo.

## Verificación

- Recompilación en Unity 6000.3.23f1: sin errores ni advertencias de compilación. Permanece el aviso independiente del CLI sobre nombres antiguos de Pipeline; no se han actualizado paquetes.
- `Docs/Validation/PrunerGestureCheck.cs`: reglas/proximidad reales de `HandGrabInteractor.SelectingGrabTypes`, con entrada sintética de dedos para ambas manos. Se comprueba pinza, palma sin cierre simultáneo de tres dedos, rechazo de mano abierta y pinza lejana, y que desactivar el trigger elimina la tolerancia añadida. Se comprueba la inclusión del volumen en el caché de Meta de los tres Pruner de Garden_Moves, los colliders sólidos, gravedad, cierre/apertura, retención y liberación sobre la mesa.
- `Docs/Validation/EasierStemCutRegression.cs`: 24 comprobaciones aprobadas de corte, madurez, falta de herramienta, contacto con fruto, hojas ya cerradas, pausa, pérdida de seguimiento, cambio de mano y ausencia de cosecha sin recogida/entrega.
- Informes de esta ejecución: `PrunerPickupCompile.json`, `PrunerPickupPlay.json`, `PrunerGestureVerification.txt` y `PrunerPickupCutRegression.json`, dentro de `Docs/Validation`.
- Pruebas temporales en Play; no se guardan modificaciones de escena.

Pendiente: adquisición completa fotograma a fotograma con sensores reales, comodidad de ambas manos y mandos físicos en Meta Quest 3. La prueba de proximidad/gestos utiliza el filtro de candidatos de Meta con datos sintéticos; no reproduce cámaras ni seguimiento del dispositivo.
