# Voces de corte y recogida — 8 de noviembre de 2026

Las tres entradas de cosecha de `Assets/Resources/GronnyAudio/Tutorial.asset` ya tienen grabaciones reales en `Assets/Audio/dialogues_cozy_gardening/`. No requieren asignaciones manuales en el Inspector.

Se han añadido después demostraciones visuales para el corte y la recogida, sincronizadas con estas mismas grabaciones, sin regenerarlas: `Docs/GronnyHarvestAnimations.md`.

| ID | Duración | Activación |
| --- | ---: | --- |
| `harvest_cut_stem` | 12,04 s | Tomate realmente maduro, con tierra y sin cortar. Explica tomar las tijeras, abrir las hojas alrededor del tallo y cerrar la mano o apretar el gatillo. |
| `harvest_stem_cut` | 3,24 s | Primer corte válido: “Nicely done! Now you can pick up your tomato.” |
| `harvest_picked` | 3,08 s | Primer agarre Meta del tomate separado: “Well done! You have harvested your tomato!” |

Guion completo: `Docs/GronnyHarvestDialogues.json`. Se conserva el nombre natural de Pruner en inglés, *pruning shears*. El cierre general `13_complete` sigue después de entregar el fruto en la bandeja.

## Generación e integración

Se usó Python y el modelo ya instalado KittenTTS v2, voz Kiki, preset `expressive`, expresión `tender`, temperatura 0,9. No se instalaron paquetes, descargaron modelos ni usaron servicios externos. Los WAV son mono PCM de 16 bits a 24 kHz. No se alteraron tono ni velocidad; solo se aplicó un fundido de 5 ms y, cuando era necesario, margen de pico.

La instrucción inicial usa semilla 811; el reconocimiento de corte, 812; la felicitación final, 814. La primera toma de la felicitación se conservó en `output/GronnyHarvestVoice_20261108/takes/`. Se sustituyó la contracción por “You have” para que la frase se reconociera completa.

Reproducción de la generación: Python del entorno local existente, con `-X utf8 -B Docs/GenerarVozCosechaGronny.py`. El script rechaza sobrescribir tomas existentes; acepta IDs al final para generar solo una frase. Guarda audio, parámetros y hashes dentro del proyecto. La verificación local usa `Docs/VerificarVozCosechaGronny.py` y el modelo Whisper `base.en` ya instalado, en modo sin conexión.

`Docs/Validation/ConnectHarvestVoice.cs` valida el lote, copia los WAV al proyecto, importa con los ajustes del audio existente y asigna **Clip** y **Transcript** en las tres entradas. Se conservan su prioridad 10, modo común a ambas entradas y la misma instancia de `GronnyVoicePlayer`. No se añadieron fuentes de audio ni cambios en scripts de juego, escenas, prefabs o dependencias. Los `.meta` nuevos los generó Unity al importar; no se sustituyeron los de grabaciones existentes.

## Validación realizada

- Tres señales finitas, mono, 24 kHz, sin muestras saturadas. Las tres transcripciones coinciden con el guion después de normalizar puntuación y mayúsculas. Informe: `output/GronnyHarvestVoice_20261108/AudioQA.json`. La transcripción automática no evalúa la naturalidad de la interpretación.
- Unity importó y asignó los tres clips; GUID, duración y hash: `Docs/Validation/HarvestVoiceImport.json`.
- `Docs/Validation/HarvestVoiceRegression.cs`: 17 comprobaciones aprobadas en Play pausado sobre la cola real del tutorial, con el cultivo y la herramienta existentes. Comprueba rechazo de tomate verde/sin tierra, una instrucción de madurez, reconocimiento de corte, recogida mediante Select de Meta, entrega inmediata, conservación de felicitaciones tras completar, orden anterior al cierre, una sola fuente y ausencia de ayudas duplicadas.
- Las entradas de cierre de tijeras y selección fueron sintéticas. Se adelantaron explícitamente los finales de frase para comprobar la cola sin esperar la duración completa; esto no sustituye una prueba de escucha con tiempos reales.
- Informes: `Docs/Validation/HarvestVoiceRegression.txt` y `HarvestVoiceRegressionResult.json`.

Pendiente: escucha humana de la interpretación, transiciones con duración real y reproducción en Meta Quest 3. No se generó APK. Las voces de tierra que estaban pendientes no forman parte de este lote.
