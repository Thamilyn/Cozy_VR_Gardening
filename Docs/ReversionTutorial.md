# Tutorial restaurado y ritmo de manos

Se revirtió la separación de práctica y jardín a petición del usuario. La única
escena de actividad habilitada vuelve a ser `Garden_Moves`. Se retiraron
`Garden_Practice`, sus utilidades de carga y su comprobación específica, con
copias de recuperación dentro del proyecto indicadas en
`output/TutorialRollbackBackupPath.txt`.

El mismo cultivo recorre el tutorial: movimiento, agarre y liberación de semilla,
plantación, riego, apertura del calendario y selección. No hay cambio de escena
ni otra planta al terminar las instrucciones. La práctica inicial vuelve a tener
recarga libre y la reserva limitada empieza tras el primer avance válido.

Se restauraron los tres WAV anteriores a la separación y sus textos de catálogo:
saludo con «I'm Gronny», introducción al tomate y ayuda de recarga. Los audios
accesibles de semilla y las otras voces conservan su estado anterior. Todos los
114 hashes de WAV y `.meta` coinciden con la captura anterior a la separación.

## Apariencia y riego

Al plantar se vuelve a mostrar `Tomate0`, la maceta con tierra sin un brote. El
primer brote usa `Tomatev1.4` después de satisfacer el objetivo de agua y avanzar
el calendario. Se restauró la asignación visual anterior: la separación había
asignado `Tomatev1.4` al acto de plantar y podía parecer crecimiento sin riego.

Se mantienen los bloqueos de avance por agua insuficiente y los pasos del
tutorial. La comprobación de audio/eventos incluye ahora un caso que intenta
avanzar inmediatamente después de plantar y exige que no cambie el día ni se
muestre un brote. El anuncio del brote precede a las explicaciones de la reserva.

## Manos más pausadas

El catálogo permite ajustar tres valores:

- Las voces vuelven a su velocidad y tono originales (`pitch = 1`). Se retiró
  `handInstructionSpeed`: ralentizar AudioSource alteraba el sonido y el usuario
  pidió corregirlo. La pausa adicional se aplica únicamente a la demostración.
- `handPoseHoldSeconds = 2.5`: intervalo mínimo entre pasos visuales.
- `handPoseBlendSeconds = 0.7`: transición más gradual entre poses.

El ciclo visual tiene su propio reloj y no se comprime para caber en un WAV
corto. Sigue repitiéndose en silencio hasta completar la acción. La pausa
conserva ese reloj; cambiar de entrada descarta la demo incompatible. Las voces
generales y las indicaciones de mandos mantienen su velocidad anterior.
`Tick` conserva el tiempo de audio para las comprobaciones de poses existentes;
la reproducción lenta usa `TickTeaching`.

## Verificación

Revisión estática e integridad de archivos:
`output/TutorialRollbackStaticVerification.json`. Se comprobó la restitución de
los audios y `.meta`, la eliminación de referencias al flujo separado, los textos
del catálogo y la configuración de escenas.

Tras restablecerse el límite del revisor se confirmó la revisión actual:

- Compilación sin errores ni advertencias: `output/OriginalVoiceSingleSceneCompile.json`.
- Catálogo y sustitución visual de macetas correctos:
  `output/OriginalVoiceSingleSceneCatalogue.json` y
  `Docs/Validation/TomatoPotVisualVerification.txt`.
- 89 comprobaciones correctas de Play:
  `output/OriginalVoiceSingleScenePlayVerification.txt`. Incluyen el tono original
  de las instrucciones de manos (`AudioSource.pitch = 1`), intervalos visuales
  de al menos 2,5 segundos, menú, saludo tras Play, pausa y suministro de agua.
- Se confirmó la integridad de los 114 hashes originales de WAV y `.meta`.
- Garden_Moves quedó abierta, fuera de Play y sin cambios pendientes:
  `output/OriginalVoiceSingleSceneFinalScene.json`. Garden_Practice y su voz
  exclusiva se retiraron después de abrir la escena principal y conservar copias
  de recuperación (`output/SingleSceneReturnBackup.txt`).

Queda pendiente valorar la escucha y los gestos físicos en Quest 3. Las 24
comprobaciones históricas de la separación no se atribuyen a esta revisión.

Para probar manualmente: salir de Play, abrir `Assets/Scenes/Garden_Moves.unity`
y pulsar Play. Confirmar tierra sin brote al plantar, rechazo del avance sin
riego, brote después de regar y avanzar, y ritmo de las manos con pinza real.
