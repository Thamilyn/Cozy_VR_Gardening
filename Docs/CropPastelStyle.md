# Tomates, maceteros y tierra pastel

`GardenCropPastelSetup` aplica `Cozy Garden/Styled Props` a los materiales PBR de los prefabs en `Assets/Prefabs/TomatoStages`, incluidos los de reserva, y a los objetos opacos restantes de la escena. Los materiales originales permanecen intactos: las copias están en `Assets/Materials/GardenPastel/Crops`. Se mantiene su nombre original para conservar la identificación de `Soil` y de los tomates por los scripts de cultivo y cosecha.

Las copias conservan albedo, tinte, UV, caras y recorte alfa. Omiten mapas normales, reflejos metálicos y brillo especular. Los recortes de hojas y flores siguen activos en sus tres materiales. La botella transparente mantiene su shader y transparencia, con brillo, metal y reflejos desactivados. No se cambian materiales de Gronny, manos Meta, interfaz ni guías de interacción.

Se actualizaron 35 materiales mediante copias locales y 12 prefabs. La escena también usa las copias para el macetero del SDK y el plato con material estándar. No se añaden componentes por fotograma, texturas ni pasadas de render.

## Verificación

`Validation/CropPastelCompile.json`: recompilación sin errores ni advertencias de compilación. `Validation/CropPastelVerification.json`: shader sin errores, nombres preservados, todas las etapas importadas con material Soil y sin materiales PBR pendientes. `Validation/PastelMatureTomatoReview.png`: render de la planta madura en el Editor.

La inspección de referencias comprueba los requisitos de identificación de tierra/frutos y de recortes. No se ha ejecutado el ciclo completo de plantar, regar y cosechar ni se ha medido el rendimiento en Quest 3 para este ajuste.
