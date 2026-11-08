# Prado con assets GHIBLI

Garden_Moves utiliza los prefabs importados de
Assets/3D set of stylized nature - GHIBLI style/Art/Prefabs.
El pack original, sus materiales, meshes y archivos meta se conservan intactos.

## Composición

Gronny y la mesa mantienen sus posiciones, sobre un césped continuo con la
textura Grass_ground_Base_Color del pack. Se eliminó la instancia decorativa
Floor (1), que dibujaba la plataforma rectangular a y=0,03. Se conserva el
MeshCollider plano de Floor como soporte físico, sin dibujar su malla antigua.
No se cambia cámara, seguimiento, gravedad ni interacciones Meta.

Un Big Tree de 11 m al fondo, en (-2, 0, 18) respecto al centro del suelo,
actúa como referencia principal. Veinte árboles Tree_01/02/03 rodean el claro.
Se añaden doce arbustos, seis rocas, 192 grupos de césped, 96 de flores y seis
vallas del pack. El camino ocre de dos metros de ancho serpentea desde z=4
hasta z=18 por un lateral. Se excluye vegetación decorativa del corredor
central alrededor de Gronny, la mesa y la posición inicial del jugador.

Se mantiene la zona central plana de 22 m de radio y la curvatura exterior
hasta 42 m, con una caída de 20 m. La vegetación se sitúa en la parte plana.
No se cambian cielo ni ciclo día/noche. No se recrean las montañas lejanas de
la referencia; se establece el prado, el árbol principal, el camino y el bosque.

## Implementación y coste

GardenDistantGround crea el suelo y ocho sectores del camino.
GardenLandscapeMesh genera únicamente el sendero; se quitaron los árboles
procedurales anteriores. GardenNatureScenery distribuye 333 instancias de
prefabs con semilla fija, normaliza su tamaño usando los límites visibles
y alinea su base al césped. El objeto generado usa escala mundial unitaria.

Las colisiones decorativas, sombras, probes y motion vectors se desactivan.
Cada instancia tiene culling mediante LODGroup de un solo nivel; no se crean
mallas simplificadas. Las plantas pequeñas desaparecen antes que los árboles.
Se comparten copias temporales de materiales con instancing habilitado y
viento suave configurado mediante GardenNatureStyle. Esto no elimina el coste de evaluación
de los Shader Graph originales ni garantiza que Unity agrupe todos los draws.
No se duplica ninguna textura. La textura del suelo conserva sus mipmaps.

El suelo tiene 3.168 triángulos y el camino 128. Unity ha contado 102.851
triángulos en los prefabs decorativos: 106.147 en todo el entorno añadido,
antes de culling, excluyendo personajes y objetos preexistentes del jardín.
Esto no es una medición de FPS, draw calls ni overdraw en Quest 3.

## Validación en Unity

Se conectó mediante Unity CLI al Editor 6000.3.23f1. El acceso localhost
requería ejecutar el CLI fuera del sandbox; Pipeline no necesitó actualizarse.

- Recompilación C#: cero errores y cero advertencias.
- Shaders usados: sin errores informados. El shader Flower del pack conserva
  dos advertencias sobre pow con posibles bases negativas; no se cambió el pack.
- Confirmada la eliminación de Floor (1) y la superficie física a y=0.
- Confirmadas 333 instancias decorativas y cero colliders decorativos activos.
- Corregidas las variantes Flower_*_1: eran cabezas de flor planas. Ahora se
  usan Flower_01_3, Flower_02_3, Flower_03_3, Flower_06_2 y Flower_09_2 con tallos.
- Se limita también la anchura al normalizar prefabs para evitar otra expansión
  accidental. El culling de plantas pequeñas se ajustó de 0,025 a 0,008 de altura
  relativa en pantalla al comprobar que desaparecían demasiado pronto.
- Revisada una captura desde (0, 1,3, -3,6), con FOV 75 grados. Es una vista
  estática de Editor, no una captura de un visor ni una prueba de seguimiento.
- La escena se guardó en Unity. No se ejecutó Play Mode ni build.

Archivos de revisión en Docs/Validation:
NatureCompile.json, NatureUnityVerification.json y NaturePlayerView.png.
Los scripts Nature*.cs en esa carpeta son auxiliares ejecutados por eval_file;
no son componentes del juego. La captura muestra Gronny en la pose de edición.

Pendiente: rendimiento, overdraw, culling y comodidad en Quest 3, estéreo real,
y vista con animaciones y ciclo día/noche en Play. La composición usa los
assets reales de la referencia, pero no reproduce exactamente su iluminación,
densidad del prado ni las montañas. El cielo existente se mantiene.


## Dirección visual posterior

Se añadieron viento en GPU, una paleta compartida y materiales mates para los
objetos. La implementación y validación actual se describen en GardenArtStyle.md.
Los informes anteriores no representan el coste final del shader ni sustituyen
el perfilado en Quest 3.


## Césped corto

La cobertura de hierba vigente se describe en GardenShortGrass.md. Se retiraron
las matas altas de hierba; el resto de los prefabs de naturaleza se conserva.

