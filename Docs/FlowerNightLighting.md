# Flores con iluminación diurna y nocturna

La causa era UniversalUnlitSubTarget en el Shader Graph Flower del pack.
Ese target ignora la luz principal y el ambiente que GardenPhaseTimeCycle
actualiza al cambiar de día a noche. No era un fallo del cubemap del cielo.

Se creó Assets/Materials/GardenFlowerLit.shadergraph a partir del original,
cambiando únicamente UniversalUnlitSubTarget por UniversalLitSubTarget.
Se conservan los nodos, texturas, máscara alfa, transparencia y viento.
El shader original del pack y sus materiales no se modifican.

GardenDistantGround referencia explícitamente la variante en flowerLightingShader
para incluirla en la escena/build. GardenNatureScenery pasa esa referencia a
GardenNatureStyle, que la asigna a la copia de trabajo del material Flowers
antes de configurar el viento. El resto de la vegetación y el ciclo del cielo
no cambian. No hay emisión conectada en el nuevo grafo.

## Comprobación

Unity 6000.3.23f1: compilación C# sin errores ni advertencias. Las flores
generadas de Garden_Moves usan Shader Graphs/GardenFlowerLit.

VerifyFlowerLighting.cs renderizó una flor con los valores reales day/night
serializados en GardenPhaseTimeCycle, con cámara fija y viento desactivado
solo en el material de prueba. Se restauraron el sol y los ajustes ambientales.

- Shader original Unlit: luminosidad media noche/día = 1,00.
- Variante Lit: luminosidad media noche/día = 0,472.

La comparación usa píxeles de una captura de Editor, no una medida fotométrica.
Las imágenes muestran una flor clara de día y oscura/azulada de noche.
El grafo conserva una advertencia heredada sobre pow con posibles bases
negativas, sin errores de shader informados.

Una segunda comprobación con cámara fija confirmó que el viento sigue activo:
822 píxeles cambiaron y 159 cambiaron de silueta entre fotogramas separados
1,21 s. No se añadió lógica por flor ni actualización de iluminación global.
La variante Lit añade cálculo de iluminación respecto a Unlit; falta medir
su coste y revisar la transición en Play y en Quest 3.

Informes y capturas en Docs/Validation:
FlowerLightingCompile.json, FlowerLightingVerification.json,
FlowerWindVerification.json, FlowerLitday.png y FlowerLitnight.png.

## Variante cel pastel posterior

Las flores usan ahora PastelFlowers con iluminación cel explícita. Aunque el
target del grafo es Unlit, su función evalúa luz principal y ambiente. La
comprobación nocturna posterior está en GardenPastelStyle.md.

