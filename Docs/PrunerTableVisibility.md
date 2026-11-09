# Tijeras visibles en la mesa principal

9 de noviembre de 2026. Garden_Moves, Unity 6000.3.23f1.

La copia de Pruner de la mesa principal se veía en Editor en el extremo lateral (1.441, 0.8996, -2.029). No se reprodujo una desaparición de esa copia en Editor. Las otras dos copias estaban fuera del área de trabajo, y la situada en Side1 caía al suelo al iniciar.

Se movió únicamente la copia de la mesa principal a (-0.15, 0.906, -2.36), delante de la maceta y en una zona libre. GardenPruner registra un punto de retorno solo si TableSet sostiene su ubicación inicial. Mantiene esa pose hasta el primer agarre y restaura las restricciones anteriores al recibir Select de Meta. Si después cae más de 45 cm por debajo de su pose de inicio y permanece sin sujetar dos segundos, vuelve a ese punto. Dejarla en otro lugar del tablero no la recupera. Las otras dos copias mantienen su comportamiento anterior.

Validación: recompilación sin errores. PrunerTableCheck.cs pasó sobre Garden_Moves en Play: meshes visibles, apoyo en el tablero, aparcado con cuerpo dinámico, referencias de ambos agarres, tres segundos de física e impulso sin desplazar la pose inicial, menú, selección/movimiento/liberación por la ruta real de eventos Meta, no recuperar al colocar sobre la mesa y recuperación tras caída. La expiración del temporizador se proporcionó sintéticamente. Informe: Docs/Validation/PrunerTableVerification.txt. Captura desde el lado del jugador: PrunerTableAfter.png.

Se salió de Play sin guardar cambios de las pruebas. La recolocación sí está guardada en la escena. La build nueva incluye también el arreglo anterior de la regadera. Pendiente: sensores físicos y percepción dentro de Quest 3.
