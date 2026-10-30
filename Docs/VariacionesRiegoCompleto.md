# Variaciones al completar el riego

Implementado el 29 de octubre de 2026, tras la autorización del usuario. Los
seis WAV nuevos están generados, importados y conectados al catálogo de Unity.

## Causa revisada

`GronnyTutorial.Update` solicita `water_stopped_success` y
`water_ready_next_phase` una vez por fase cuando `Ready` es verdadero, durante
el cultivo con reserva limitada. Ambos tienen una sola grabación en el catálogo.
El aviso `help_stop_watering` ya tiene tres variantes, pero solo se solicita
cuando se sigue vertiendo después de alcanzar el objetivo. No es la felicitación
que se escucha al terminar de regar.

`06_enough_water` pertenece al primer riego guiado, en fase 0, y se entrega una
sola vez por sesión. Se conserva su grabación y demostración originales.

## Cambio implementado

`GronnyCompletedWateringVariations.json` añade tres frases para la felicitación
y tres para la indicación de avanzar. Conservan el inglés, el tono amable, el
ahorro de agua y el nombre del botón Advance phase.

`GronnyWaterSavingAudioSetup.ConnectCompletedWateringVariations` conecta solo
esos dos avisos tras validar el lote completo. Usa la misma validación que los
avisos de agua existentes. Reconectar reemplaza las listas de esos dos avisos
sin acumular duplicados. Los originales y las variantes de los avisos restantes
se conservan.

`GronnyVoicePlayer` ya rota la grabación original y las variantes cuando comienza
la reproducción. No requiere cambios: mantiene la rotación entre fases y pausa,
no consume variantes canceladas y conserva la toma al reanudar una interrupción.
Cada uno de estos dos avisos tiene cuatro grabaciones contando la original.

## Generación y conexión

El usuario autorizó el generador local, la revisión de voz, la conexión y la
comprobación de compilación en Unity. Se reutilizaron los modelos y dependencias
existentes, sin instalaciones ni descargas. Todos los resultados quedaron dentro
del proyecto.

Desde la raíz del proyecto, con el Python del entorno existente
`C:/Users/Trole/Documents/generate_voice/venv/Scripts/python.exe`:

```text
<Python existente> -X utf8 -B Docs/GenerarVocesMenu.py --dialogues Docs/GronnyCompletedWateringVariations.json --output-dir Assets/Audio/water_saving/variations
<Python existente> -X utf8 -B Docs/VerificarVocesLote.py --dialogues Docs/GronnyCompletedWateringVariations.json --audio-dir Assets/Audio/water_saving/variations --report output/CompletedWateringAudioVerification.json
```

El generador conservó KittenTTS v2, Kiki, expressive y temperatura 1.1. Rechaza
sobrescribir WAV existentes. Se ejecutó `ConnectCompletedWateringVariations`
mediante Unity CLI 1.0.0-beta.12 y el Pipeline instalado, en Unity 6000.3.23f1.
La conexión también está disponible en el menú
`Garden > Gronny > Connect completed watering variations`.

El CLI inicialmente no detectaba el Editor abierto. Se restauró su conexión
reiniciando el servidor Pipeline existente desde el menú del Editor, sin cambiar
paquetes ni configuración del proyecto.

## Validación

`unity recompile` confirmó que la compilación estaba al día, sin errores ni
advertencias (`output/CompletedWateringCompile.json`).
`GronnyAudioVerification.VerifyCatalogue` pasó: 58 avisos base y 18 variantes,
incluidas estas seis. Comprobó referencias, cobertura de archivos, transcripciones,
duraciones, tiempos de demostración y recursos de Meta
(`output/CompletedWateringConnection.json` y `Docs/Validation/GronnyCatalogue.txt`).
Las nuevas importaciones usan los mismos ajustes de audio que los avisos existentes.

La comparación de hashes confirmó que los 145 archivos anteriores de audio y
metadatos se conservaron. La comparación del catálogo serializado confirmó que
solo cambiaron las listas de variantes de `water_stopped_success` y
`water_ready_next_phase` (`output/CompletedWateringPreservation.json`).

El reconocedor local Whisper confirmó las palabras de las tres felicitaciones.
En las tres indicaciones de avance transcribió «Advance phase» como «Advanced
Phase»; la misma diferencia aparece al revisar la grabación original
`water_ready_next_phase.wav` con el mismo reconocedor. Se conservaron las tomas
y se dejó constancia de esa diferencia, sin afirmar coincidencia exacta. No se
detectó saturación en las seis grabaciones
(`output/CompletedWateringAudioVerification.json` y
`output/CompletedWateringBaselineAudioVerification.json`).

La rotación existente, la cancelación y la reanudación se revisaron estáticamente.
Quedan pendientes la escucha humana y la prueba del comportamiento entre fases,
pausa y reanudación en Play y Meta Quest 3. No se ejecutaron pruebas de Play ni
se generó un APK.
