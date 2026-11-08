# Dirección visual y viento del jardín

## Resultado

Garden_Moves combina la vegetación del pack GHIBLI con objetos de aspecto mate.
Se conserva el cielo, el seguimiento XR, las interacciones, los materiales de
agua y los colores de las indicaciones. Gronny conserva MToon10 y sus materiales
faciales: no se sustituye su shader ni se modifican las manos o mandos Meta.

## Vegetación

GardenNatureStyle configura las copias temporales de los materiales que crea
GardenNatureScenery. No se alteran los Shader Graph ni materiales originales
del pack. Los materiales se comparten por origen y conservan GPU instancing.

- Grass: viento 0,16, velocidad 0,65 y dirección (20,14), gradiente de raíz
  verde oscuro a punta verde cálida y variación de color en espacio mundial.
- Flower: viento 0,10 con la misma velocidad y dirección. Se conservan las
  texturas y máscaras de los pétalos.
- Leaves: jitter 0,025 y rotación leve. La copa lila se conserva; las copas
  verdes se acercan a la paleta del prado. EmissionPower se fija en 3 porque
  ese parámetro es el exponente Fresnel, no la intensidad de emisión.
- Props y Layered del pack conservan sus texturas; se suavizan los normales.

Se amplían ligeramente los límites de los renderers generados para el viento.
El suelo y el camino no se deforman. El movimiento es de vértices en GPU,
sin Update por cada planta, físicas, animadores ni generación de malla continua.

## Objetos y pasto original

GardenStyledProps.shader conserva albedo, UV, normales opcionales, cutout y
emisión. Usa luz principal y ambiente, transición suave de luz, sombras frías
y luz cálida, con menos contraste especular. Incluye Forward, ShadowCaster y
DepthOnly, macros XR e instancing. No calcula reflejos PBR ni luces adicionales.
GardenLandscape usa la misma paleta de iluminación para suelo y sendero.

GardenArtDirectionSetup ofrece Garden > Apply unified meadow shading.
Convierte materiales opacos compatibles usados por la escena; deja fuera los
SkinnedMeshRenderer, el rig Meta, los Canvas y los materiales transparentes o
especiales. Los FBX reciben materiales de escena en Assets/Materials/GardenStyle;
no se modifican los modelos. Los materiales .mat usados directamente se
actualizan conservando su GUID. Sus copias anteriores están en
Docs/GardenStyleBackups/<GUID>.mat. El listado completo está en
Docs/Validation/ArtStyleMaterials.txt.

Se actualizaron o crearon 19 materiales. La escena usa el nuevo shader en
234 renderers. De ellos, 110 renderers decorativos GrassSmall/GrassTall tienen
variantes de viento independientes: el atlas compartido por herramientas y
macetas no recibe animación. El viento desplaza como máximo 0,02 m, anclado en
la raíz y creciente hacia la punta. La misma deformación se aplica en los
pases de profundidad y sombra. Los cambios de color con _BaseColor que usan
las interacciones siguen siendo compatibles.

## Comprobación

Unity 6000.3.23f1 recompiló C# con cero errores y advertencias. Se revisó el
resultado mediante cámara de Editor a 1,3 m y se guardó Garden_Moves.
ShaderUtil no informó errores en los shaders usados. Persisten advertencias
importadas de Flower sobre pow y de MToon10 sobre compatibilidad/gradientes;
no se actualizaron dependencias para resolverlas.

Dos comprobaciones aislaron una mata frente a una cámara fija, sin físicas:

- WindVerification.json: 8.144 píxeles cambiaron y 6.921 cambiaron de silueta
  en 1,21 s con el shader Grass del pack.
- ForegroundWindVerification.json: 2.325 píxeles cambiaron y 1.273 cambiaron
  de silueta en 1,21 s con la variante de pasto junto a la mesa.

Estas capturas confirman movimiento de vértices renderizado, no solo cambios
de color. Son pruebas gráficas aisladas en Editor: no prueban FPS, comodidad
ni seguimiento del visor. Los callbacks de captura se eliminan al terminar.
No se ejecutó Play Mode ni build Android. Faltan perfilado de CPU/GPU,
overdraw, calidad móvil y revisión estereoscópica real en Quest 3.

Informes y capturas en Docs/Validation: ArtStyleCompile.json,
ArtStyleVerification.json, NaturePlayerView.png, WindFrameA/B.png y
ForegroundWindFrameA/B.png.

## Césped continuo posterior

Las matas de hierba descritas arriba se sustituyeron por parches compartidos de
césped corto. La descripción y validación vigente está en GardenShortGrass.md.
Los informes de viento anteriores se conservan como historial.


## Iluminación de las flores

Las flores usan ahora una variante Lit del grafo original para responder al día
y la noche. Detalles y comprobación: FlowerNightLighting.md.


## Cel pastel posterior

El acabado vigente se describe en GardenPastelStyle.md. Conserva viento y
respuesta nocturna mediante iluminación cel explícita en los grafos propios.

