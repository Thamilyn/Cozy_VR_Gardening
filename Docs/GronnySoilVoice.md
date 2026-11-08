# Voces de tierra y pala — 8 de noviembre de 2026

Estado: las tres voces están generadas, verificadas e importadas en Unity 6000.3.23f1. Las entradas existentes del catálogo `Assets/Resources/GronnyAudio/Tutorial.asset` ya tienen Clip asignado; no se requieren asignaciones manuales.

Guion: `Docs/GronnySoilDialogues.json`. Se mantienen el inglés y la voz Kiki de Gronny, KittenTTS v2, expressive/tender, temperatura 0,9. Se reutiliza el modelo instalado sin conexión; no se instalan paquetes ni se descargan modelos. Audio, temporales e informes se guardan dentro del proyecto.

| ID | Duración | Activación existente |
| --- | ---: | --- |
| `soil_scoop_first` | 8,92 s | Paso de cobertura con la pala vacía: recoger tierra y explicar el vertido. |
| `soil_cover_seed` | 9,40 s | Paso de cobertura con la pala ya cargada: explicar el vertido. |
| `soil_seed_covered` | 3,16 s | Cobertura válida: confirmar antes de las instrucciones de riego. |

Las dos instrucciones iniciales son alternativas. El tutorial conserva una sola fuente y termina la frase en curso, descartando las instrucciones pendientes que ya no correspondan. El guion indica inclinar y mantener brevemente la hoja sobre la abertura, sin exigir una pose horizontal previa.

Las dos instrucciones ya incorporan guías animadas para manos y mandos, con los mismos WAV: `Docs/GronnySoilAnimations.md`. La felicitación de cobertura no añade otra acción de ejemplo.

## Generación y comprobación realizadas

Desde la raíz del proyecto, usar el Python existente en `C:/Users/Trole/Documents/generate_voice/venv/Scripts/python.exe`:

```text
-X utf8 -B Docs/GenerarVozTierraGronny.py
-X utf8 -B Docs/VerificarVocesLote.py --dialogues Docs/GronnySoilDialogues.json --audio-dir output/GronnySoilVoice_20261108 --report output/GronnySoilVoice_20261108/AudioQA.json
```

El generador rechaza sobrescribir WAV existentes y guarda los parámetros, duración y hashes en `output/GronnySoilVoice_20261108/Generation.json`. Revisar las tres transcripciones y las señales antes de importar. La comprobación utiliza Whisper base.en ya instalado y no sustituye una escucha humana.

`Docs/Validation/ConnectSoilVoice.cs` se ejecutó mediante Pipeline `run_script` fuera de Play. Importó los tres audios con los ajustes de `13_complete.wav` y asignó Clip y Transcript a sus entradas existentes. Primero completa todas las importaciones y después asigna y guarda las referencias, evitando que una reimportación descarte las asignaciones anteriores. Unity generó los tres `.meta` nuevos.

Verificación de este lote:

- Tres WAV mono PCM de 16 bits a 24 kHz, señales finitas y sin muestras saturadas. Las tres transcripciones coinciden con el guion tras normalizar puntuación y mayúsculas. Informe: `output/GronnySoilVoice_20261108/AudioQA.json`.
- Importación y asignación correctas, con duración, GUID y hash de cada clip: `Docs/Validation/SoilVoiceImport.json`. El importador compiló sin diagnósticos.
- Referencias guardadas comprobadas contra los `.meta` y hashes de los WAV. Las 80 grabaciones anteriores conservan su hash. El resto del catálogo conserva sus valores; Unity solo reajustó el salto de línea de dos textos de detalle. Informe: `Docs/Validation/SoilVoicePreservation.json`.
- El Editor quedó disponible y fuera de Play. No se ejecutaron pruebas de juego, no se generó APK ni se probó Quest 3. Quedan pendientes la escucha humana y las transiciones reales entre estas voces y el riego. La validación anterior de recogida no se ha repetido con este lote.
