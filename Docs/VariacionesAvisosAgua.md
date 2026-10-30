# Variaciones de los avisos de agua de Gronny

Implementado el 29 de octubre de 2026. Los 12 WAV nuevos están generados,
importados y conectados al catálogo de Unity.

`GronnyWaterWarningVariations.json` contiene tres frases nuevas para cada aviso:
agua fuera de la maceta, riego suficiente, exceso de agua y agua drenada. Se
mantienen el inglés, las acciones que debe realizar el jugador y un tono amable.
Cada aviso tiene cuatro grabaciones contando la original.

`GronnyVoicePlayer` comienza con la grabación original y recorre las variaciones
en orden antes de repetirla. La rotación es independiente para cada aviso y se
conserva al cambiar de fase o volver de pausa. Las solicitudes canceladas antes
de comenzar no consumen una variante. Una frase interrumpida conserva su
grabación y posición al reanudarse.

Los IDs, prioridades, intervalos de repetición, instrucciones breves y
demostraciones pertenecen al aviso original. Tener más grabaciones no permite
reproducir avisos con mayor frecuencia. Si no hay variantes conectadas se sigue
reproduciendo la grabación original.

## Generación y conexión

Se reutilizó `Docs/GenerarVocesMenu.py` con el Python del entorno local existente
de KittenTTS v2, voz Kiki, preset expressive y temperatura 1.1. La ejecución fue
autorizada por el usuario, sin instalar ni descargar dependencias o modelos.
Se ejecutó desde la raíz del proyecto, con todos los archivos de salida dentro:

```text
<Python existente> -X utf8 -B Docs/GenerarVocesMenu.py --dialogues Docs/GronnyWaterWarningVariations.json --output-dir Assets/Audio/water_saving/variations
```

El generador impide sobrescribir WAV existentes. Tras comprobar las tomas e
importarlas en Unity, se ejecutó `ConnectWarningVariations` mediante Unity CLI
y el Pipeline ya instalado, en Unity 6000.3.23f1. También está disponible en
`Garden > Gronny > Connect water warning variations`.
El comando valida el lote completo y las duraciones antes de asignar las
referencias. Conserva las grabaciones originales y los `.meta` existentes.
Reconectar reemplaza la lista de variantes de esos cuatro avisos, sin acumular
duplicados. `VerifyCatalogue` e inventario incluyen las variantes.

## Validación

La compilación está comprobada sin errores ni advertencias
(`output/WaterWarningCompile.json`). El catálogo pasa las comprobaciones de
referencias, cobertura, transcripciones y duración para las demostraciones:
58 avisos base y 12 grabaciones adicionales (`output/WaterWarningConnection.json`
y `Docs/Validation/GronnyCatalogue.txt`). Reconectar conserva tres variantes
por aviso y no acumula duplicados.

Se compararon los hashes de los archivos de audio y metadatos anteriores y
la serialización del catálogo: los WAV originales, sus `.meta`, los ajustes
base y las referencias existentes se conservan
(`output/WaterWarningPreservation.json`). Las nuevas importaciones usan los
mismos ajustes de audio que los avisos existentes.

Se revisaron los 12 WAV con el reconocedor local ya disponible. Una toma de
`help_missed_pot_03` se regeneró porque reconocía «Heat» en lugar de «Keep»;
la nueva toma coincide con el guion. Se conservó su `.meta` y la toma descartada
quedó en `output/WaterWarningMissedPot03FirstTake.wav`. Once transcripciones
coinciden con las palabras del guion; en `help_excess_water_02`, el reconocedor
separa «upright» como «up right». El informe conserva esa diferencia, sin
considerarla una sustitución de palabras. No se detectó saturación en las
doce tomas (`output/WaterWarningAudioVerification.json`).

La rotación, cancelación y reanudación se revisaron estáticamente. Quedan
pendientes la escucha humana y la prueba de repetición/pausa en Play y en Meta
Quest 3. No se ejecutaron pruebas de Play ni se generó una compilación APK.
