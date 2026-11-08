# Césped corto continuo

Garden_Moves sustituye las matas altas y aisladas por hojas finas y curvadas
que cubren el terreno. La altura nominal es 12 cm, con variación de 8 a 15 cm.
Se cubre también la zona de juego bajo Gronny y la mesa. El camino conserva
su superficie despejada; las flores, árboles, cultivos y objetos se conservan.
No se cambia la cámara, gravedad ni superficie física plana.

## Implementación

GardenShortGrass crea seis mallas compartidas: tres densidades, cada una con
una variante próxima de hojas curvadas y otra lejana simplificada. Se reparten
en 172 zonas de 6 x 6 m. Solo se almacenan seis mallas y tres materiales de
trabajo, en lugar de duplicar la geometría de cada zona.

GardenShortGrass.shader utiliza el mismo perfil de altura que GardenDistantGround
para adaptar cada raíz al terreno en GPU. Excluye las hojas del camino y del
exterior del mundo mediante triángulos degenerados, sin alpha blending ni
alpha clipping. El viento mueve las puntas hasta 1,8 cm, con raíces estables.
Tiene color de raíz/punta y variación suave, una luz principal y ambiente.
No utiliza texturas, geometría/tessellation shaders, sombras ni física por hoja.
Incluye instancing y macros de estéreo XR. La deformación utiliza el tiempo
que proporciona Unity; no hay Update de C# por hoja ni reconstrucción continua.

LODGroup nativo usa umbrales de 0,18 y 0,008. La mitad de las hojas próximas
se reduce gradualmente entre 4 y 7 m; el nivel lejano conserva la otra mitad.
Las zonas tienen límites CPU conservadores que incluyen la curvatura y el
viento. Los parámetros de altura y densidad están en GardenDistantGround.

Se retiró la dispersión anterior de 192 prefabs de hierba. Se desactivaron
110 MeshRenderer decorativos GrassSmall/GrassTall del jardín original mediante
overrides de escena: sus prefabs, modelos y materiales originales se conservan.
El material de césped corto está enlazado en Floor > GardenDistantGround.
Para reconstruir en Editor: menú contextual Rebuild distant ground.

## Verificación en Unity 6000.3.23f1

- Compilación C# final: cero errores y advertencias.
- Shader Short Meadow: cero errores y advertencias informados.
- Ninguno de los 110 renderers antiguos de hierba permanece habilitado.
- Se conserva el suelo físico a y=0.
- Se revisaron capturas próximas y desde una altura de jugador de 1,3 m.
- La prueba aislada de viento cambió 918 píxeles de silueta entre dos
  fotogramas separados 1,21 s, con cámara y geometría CPU fijas.
- Mayor malla: 52.020 vértices, dentro del índice de 16 bits.
- Profiler.GetRuntimeMemorySizeLong de las seis mallas únicas: 7,83 MiB en el
  Editor. La primera versión con mallas independientes ocupaba 142,42 MiB.

Los 1.600.464 triángulos lógicos de todas las zonas en nivel próximo y los
267.192 del nivel lejano son alternativas repetidas de las mallas compartidas.
No se dibujan todos simultáneamente: intervienen frustum, LOD y reducción GPU.
El recuento no es una medición de triángulos visibles, draw calls ni FPS.
La proyección y exclusión GPU también cuestan tiempo de vértices: falta medir
ese coste, el estéreo y el aliasing en Quest 3. No se ejecutó build Android ni
se afirma rendimiento validado en visor.

Informes: Docs/Validation/ShortGrassCompile.json, ShortGrassVerification.json,
ShortGrassWindVerification.json. Capturas: ShortGrassCloseup.png y
NaturePlayerView.png. La prueba usa una hoja representativa con el mismo
shader y parámetros; no sustituye una prueba de recorrido o comodidad en VR.
