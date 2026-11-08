# Acabado cel pastel

Se aplicó a Garden_Moves una paleta pastel compartida con tres bandas suaves
de iluminación. El cielo usa una variante propia de Cubemap Blend y conserva
las texturas de día/noche, tinte, exposición, niebla y parámetros de rotación.
Los originales de BOXOPHOBIC y del pack GHIBLI no se modifican.

## Shaders y escena

GardenPastelColor.hlsl define desaturación moderada y aclarado de medios tonos.
Los negros de los rótulos y sus blancos se preservan para mantener contraste.
El aclarado se aplica al albedo antes de iluminar: no añade brillo nocturno.
GardenPastelLighting.hlsl reúne luz principal, ambiente y tres bandas con
bordes suavizados. Styled Props, Landscape y Short Meadow usan esa función.

GardenPastelGraphBuilder genera variantes de Leaves, Layered, Props y Flowers
bajo Assets/Materials/GardenPastel. Conserva las redes de texturas, máscaras,
alfa y movimiento de vértices, e inserta un nodo con la iluminación cel común.
Los grafos usan un target Unlit para evitar calcular PBR una segunda vez,
pero NO son autoiluminados: la función personalizada evalúa explícitamente
GetMainLight y SampleSH. Esta diferencia es importante para las flores de noche.
Los bloques PBR de normales/emisión que no usa el target quedan inactivos.

GardenPastelSceneSetup permite aplicar el acabado desde
Garden > Apply pastel cel environment. Asigna explícitamente los shaders en
GardenDistantGround y el material PastelSky al ciclo GardenPhaseTimeCycle.
Las referencias serializadas evitan depender de Shader.Find en el dispositivo.
Gronny conserva MToon10; manos/mandos Meta, UI y shaders de interacción/agua
no se sustituyen. No se añadieron efectos de pantalla completa, contornos,
pasos de postprocesado ni controladores de actualización por planta.

## Verificación

Unity 6000.3.23f1 compiló C# sin errores ni advertencias. Los ocho shaders de
ambiente usados se revisaron sin errores informados. Se conserva una advertencia
heredada sobre pow en el grafo de flores; no se actualizaron dependencias.

Se revisaron capturas del jardín con los valores reales de día y noche del
ciclo, restaurando después sus ajustes. Se confirmó que el material del cielo
asignado al ciclo conserva ambas texturas.

Las flores pastel dan una razón de luminosidad media noche/día de 0,205 en
una captura aislada, frente a 1,0 del Unlit original. Es comparación de píxeles,
no una medición fotométrica. Las comprobaciones de viento se ejecutaron por
separado: cambiaron 298 píxeles de silueta en flores y 878 en césped corto en
aproximadamente 1,21 s, con cámara fija.

Informes en Docs/Validation: PastelCompile.json, PastelVerification.json,
PastelFlowerLighting.json, FlowerWindVerification.json y
ShortGrassWindVerification.json. Capturas: Pastelday.png y Pastelnight.png.
Faltan Play Mode/recorrido completo, build Android y perfilado/estéreo en Quest 3.
No se afirma un objetivo de FPS. Los cambios se guardaron en Garden_Moves.

## Assets sin referencia estática

El usuario pidió una lista de revisión; no se eliminó ningún archivo.
UnusedAssetsReview.md agrupa 766 candidatos (618,09 MiB de archivos fuente)
y muestra los 200 de mayor tamaño. UnusedAssetsReview.json contiene la lista
completa. Esto no implica que todos puedan borrarse ni un ahorro de build igual.
Se protegieron escenas habilitadas, Resources, StreamingAssets, referencias de
ProjectSettings, scripts/shaders y carpetas de integración Meta/XR/Plugins/TMP.
Las cargas por rutas de texto y usos en Editor o escenas fuera del build
requieren revisión manual antes de cualquier limpieza.
