# Continuidad de la voz al actuar

Se reprodujo un corte con AudioSource real: al completar el agarre, el predicado
de pertinencia dejaba de cumplirse y GronnyVoicePlayer detenía la grabación en
curso. Informe anterior a la corrección: `output/VoiceContinuityBeforeFix.txt`.

También había cortes explícitos al plantar/cambiar de fase, durante cada
actualización del salto de día y al cambiar el modo de entrada. Cualquier voz
de prioridad mayor podía interrumpir y reinsertar la anterior desde el principio.

## Comportamiento actual

- Completar una acción o cambiar de entrada retira inmediatamente la demostración
  anterior, pero permite terminar la frase que ya comenzó.
- La ayuda pendiente que ya no corresponde se descarta antes de empezar.
- `FinishCurrent` limpia la cola de la fase anterior y termina la frase actual.
  Se usa en los cambios de fase, reserva, salto temporal y cosecha.
- Los mensajes ordinarios, incluido el aviso positivo de objetivo alcanzado,
  esperan la frase actual. La UI y los permisos de interacción siguen respondiendo
  al estado real; el audio no retrasa el progreso del juego.
- Los avisos urgentes de prioridad 100 pueden interrumpir. Si la instrucción
  anterior aún corresponde, se reanuda desde su muestra de audio guardada y su
  tiempo visual. No vuelve a empezar ni registra otra asistencia.
- Stop explícito, desactivar el guía o terminar la escena siguen deteniendo
  inmediatamente. Abrir el menú conserva la pausa y reanudación existentes.

Las grabaciones, importadores, volumen y tono originales no se modificaron.
Este cambio corrige decisiones del reproductor y del tutorial; no se cambió el
modelo de agua, las acciones necesarias ni las escenas.

## Comprobación

Garden > Gronny > Verify voice continuity in Play usa AudioSource real con
cambios sintéticos de pertinencia y saltos de muestra, y también llama a la
plantación, riego y calendario reales del tutorial. No guarda la escena. El
informe es `output/VoiceContinuityPlay.txt`. La compilación se registra en
`output/VoiceContinuityCompile.json`. La batería de menú sigue comprobando
pausa/reanudación, volumen, Stop y tono original.

La compilación pasó sin errores ni advertencias y las 21 comprobaciones de
continuidad pasaron. Además de los casos aislados, se comprobó que plantar de
verdad, alcanzar el objetivo de agua y comenzar el salto de día no cortan la
frase del tutorial en curso. Se conservan los WAV y sus `.meta` sin cambios.
También pasaron las 89 comprobaciones generales de menú y suministro
(`output/VoiceContinuityMenuPlay.txt`). El Editor quedó fuera de Play y con
Garden_Moves abierta sin cambios pendientes (`output/VoiceContinuityFinalEditor.json`
y `output/VoiceContinuityFinalScenes.json`).

La prueba no mide fallos de dispositivo, transmisión de Quest Link ni carga de
audio en un visor físico. La escucha en Quest 3 queda pendiente.
