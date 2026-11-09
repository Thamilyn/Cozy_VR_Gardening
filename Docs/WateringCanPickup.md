# Regadera: agarre y postura inicial

9 de noviembre de 2026. Escena: Garden_Moves. Unity 6000.3.23f1.

El tutorial deshabilitaba ambos interactuables Meta de Watering hasta completar soilCovered. Ahora permite tomar la regadera durante la sesión; Allows(Pour) conserva el paso de riego y el menú conserva su suspensión.

WateringCan conserva la pose inicial hasta la primera selección Meta, utilizando la recuperación existente y liberando las restricciones al agarrar. La envolvente convexa importada queda como trigger de pickup; una caja del cuerpo y las formas existentes del pico proporcionan apoyo sólido. El asa superior incorpora un volumen trigger con 1,5 cm de margen para manos y mandos, creado antes de que Meta cachee colliders.

Validación: recompilación sin errores. WateringCanPickupCheck.Prepare y Verify pasaron en Play pausado sobre la escena real: inicio, menú, bloqueo de vertido, postura inicial con tres segundos de física y un impulso, cachés/referencias de ambos agarres, selección/movimiento/liberación por eventos Meta, apoyo sobre la mesa, pausa y reanudación. Ver WateringCanPickupVerification.txt. Se salió de Play sin guardar la escena.

Los eventos y muestras del menú fueron sintéticos. No validan sensores físicos, comodidad ni renderizado en Quest 3. La nueva build debe comprobarse en el visor.
