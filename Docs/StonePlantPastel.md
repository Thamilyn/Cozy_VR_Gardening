# Rocas y brote: acabado pastel

Las rocas generadas por `GardenNatureScenery` comparten `PastelStone.mat`: base gris mate, sin textura de grietas, con un tinte suave de musgo sobre las caras superiores. El material es una referencia serializada de `GardenDistantGround`, por lo que se conserva al reconstruir el entorno.

El brote `Sprout` y su base `Basket_S`, bajo `Plant_Pot` en `Garden_Moves`, utilizan materiales locales con su atlas de color original y el shader `Cozy Garden/Styled Props`. Se eliminan los reflejos PBR y se mantiene la iluminación pastel del jardín. Los materiales embebidos originales y los recursos del SDK no se modifican.

El ajuste de musgo tiene intensidad cero por defecto en el shader compartido para preservar el resto de objetos. No se añaden texturas, pasadas ni animaciones a estos objetos.

## Validación

- Recompilación en Unity sin errores ni advertencias de compilación: `Validation/StonePlantCompile.json`.
- Verificación de materiales y shaders: `Validation/StonePlantVerification.json`.
- Capturas renderizadas en el Editor: `Validation/PastelStoneReview.png` y `Validation/PastelPlantReview.png`.
- Escena guardada fuera de Play. No se ha medido el rendimiento en un dispositivo Quest 3 para este cambio.
