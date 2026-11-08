# Guías animadas de tierra y pala — 8 de noviembre de 2026

Las instrucciones `soil_scoop_first` y `soil_cover_seed` ya tienen demostraciones con las manos Meta y el mando Touch Plus del sistema existente. Se añaden `SoilScoop = 10` y `SoilPour = 11` al final de `GardenControlDemo`; los valores anteriores se conservan. No se regeneraron las voces.

## Comportamiento

- Con la pala vacía, la guía muestra una pasada corta sobre SoilPot, una carga marrón sobre la hoja, el transporte hasta la maceta y el vertido. La leyenda de agarre indica pinza o Grip según el modo.
- Al cargar la pala real, la guía cambia al ejemplo de pala cargada sobre la maceta. Inclina la cara 65 grados, mantiene brevemente y muestra seis granos reutilizables cayendo sobre la semilla. La carga desaparece y la semilla de ejemplo queda cubierta.
- Al cubrir la semilla real, el contexto de la guía deja de ser pertinente. El audio conserva su continuidad y las instrucciones pendientes obsoletas se descartan. La confirmación `soil_seed_covered` conserva Demo = None.
- Los pasos siguen el reloj de la voz. Después continúa una práctica visual sin repetir el audio: 12,5 s para recogida y 12 s para vertido, con poses de al menos 2,5 s. La pausa del menú y el cambio de entrada ocultan la guía.
- Se usa el anclaje de jardinería existente junto a Gronny, con escala 1,1 para acomodar la pala y las dos macetas. La pinza de la mano se alinea con el mango usando las puntas de índice y pulgar del esqueleto Meta.

`GardenSoilDemonstration` copia solamente Transform, MeshFilter, MeshRenderer y materiales de los modelos existentes. Conserva las mallas originalmente ocultas como ocultas y aplica el marrón únicamente a la tierra de las copias. No instancia los componentes de juego de la pala ni de las macetas: el ejemplo no puede agarrarse, recoger tierra real, cubrir una semilla real ni avanzar el tutorial. Los granos no tienen física ni se crean por fotograma. Sus materiales propios se liberan al ocultar la guía.

## Configuración guardada

En `Assets/Resources/GronnyAudio/Tutorial.asset` están asignados Shovel Visual, Soil Source Visual y Soil Pot Visual con los prefabs existentes. No se modificaron las escenas, las herramientas reales, Meta SDK ni los archivos de audio.

| Voz | Demo | Pasos dentro del WAV |
| --- | --- | --- |
| `soil_scoop_first` | SoilScoop | 0; 1,3; 3,3; 6,4; 8 s |
| `soil_cover_seed` | SoilPour | 0; 7; 8,4 s |
| `soil_seed_covered` | None | Sin demostración de otra acción |

Para reaplicar la configuración se conserva `Docs/Validation/ConfigureSoilAnimations.cs`, ejecutado mediante Unity Pipeline `run_script` fuera de Play. Las posiciones y los pasos son ajustables desde el catálogo.

## Validación realizada

- Unity 6000.3.23f1 recompiló la versión final con cero errores y cero advertencias del compilador: `Docs/Validation/SoilAnimationCompilation.json`.
- Pasaron 40 comprobaciones con Play pausado: ambos modos, carga y transporte visibles, inclinación, granos, cobertura, pinza alineada con el mango, materiales, ausencia de interacción física, mallas ocultas, ciclo lento, cambio de contexto y ocultación por menú/modo. Informes: `SoilAnimationRegression.txt` y `SoilAnimationRegressionResult.json`, en `Docs/Validation/`.
- Se revisaron las capturas de ambos modos en `Docs/Validation/SoilAnimations/`. Para las imágenes se hornean temporalmente las mallas de mano y se compensa la escala del FBX; el juego mantiene el skinning de Meta. Las primeras capturas permitieron corregir tamaño, altura, posición de la pinza y la superposición de las mallas originales ocultas de la maceta.
- Se conservan los hashes de las 80 grabaciones anteriores y las tres voces de tierra. Los demás cues del catálogo conservan sus valores: `SoilAnimationPreservation.json`.
- El Editor quedó fuera de Play, sin compilar y sin cambios pendientes en `Garden_Moves`: `SoilAnimationFinalState.json`. No se guardó la escena ni se generó APK.

Las pruebas usan tiempos de animación y condiciones de contexto suministrados sintéticamente; no ejecutan el recorrido físico completo de recoger y cubrir con un visor. Quedan pendientes la escucha con tiempos reales, la legibilidad y ubicación junto a Gronny, la comodidad y el rendimiento en Quest 3.

La consola también registró dos errores al iniciar la escena: `Missing tomato pot stage or planting surface at slot 0` y una referencia Interactable View ausente en `ISDK_HandGrabInteraction`. No se afirma que la escena esté libre de errores de configuración. Los ejemplos comprobados no contienen esos componentes. Se conserva el registro en `SoilAnimationConsole.json`. Pipeline también registra sus intentos con los nombres nuevos `script recompile`/`script recompile_status` antes de usar los comandos antiguos; la recompilación final sí terminó correctamente y no se actualizó el paquete. Un intento de pausa llegó antes de que Play estuviera listo; no se contabiliza como validación aprobada. La ejecución final terminó con los 40 checks aprobados.

Corrección posterior: los dos errores de maceta y Meta se repararon en los prefabs afectados. Pasaron 14 comprobaciones de arranque y el nuevo registro completo no contiene esos errores ni excepciones. Detalles: `Docs/CorreccionReferenciasMacetaMeta.md`. El registro anterior se conserva como historial.
