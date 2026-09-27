# Siembra en Garden_Moves

La escena contiene tres prefabs en `Assets/Prefabs/SeedsPrefab`: tomate, rábano y lechuga. Cada uno incluye `Rigidbody`, `SphereCollider`, `Grabbable` y `GrabInteractable` del Meta XR SDK. `SeedItem` comprueba la suelta y `SeedsController` permite una planta por `GardenPot`. Hay cuatro destinos configurados: `Plant_Pot` y tres instancias de `PotSmall`.

## Prueba manual en Unity

1. Abra `Assets/Scenes/Garden_Moves.unity` y entre en Play con el rig Meta XR activo.
2. Agarre una semilla y suéltela cerca del centro superior de una maceta. La semilla queda anclada a esa maceta. Una segunda semilla en esa maceta debe permanecer suelta.
3. Agarre `Watering` y vierta sobre la superficie superior de la maceta sembrada. `bottle-oil` también tiene `WateringCan` en la escena. La regadera entrega litros; `SeedsController` multiplica por 1000 antes de registrar los mililitros.
4. En el Inspector de `[Cozy Garden] Growing System`, use el menú contextual de `CalendarSystem` > **Advance one game day**. Riegue hasta cumplir los requisitos de `SeedsController`. Deben aparecer, en orden, semilla, brote y planta desarrollada.
5. Consulte `CalendarSystem.Journal` y `CalendarSystem.Plants` por Inspector o API. La entrada de riego suma los mililitros por planta y día. Salga y vuelva a entrar en Play para comprobar la restauración.

`CalendarSystem.secondsPerGameDay` y los requisitos por cultivo se editan en el Inspector. El guardado está en `Application.persistentDataPath/Garden_Moves_calendar.json`; elimine ese archivo para comenzar una partida nueva.

El hijo `Ground` del prefab `PotSmall` queda debajo del punto de siembra. Usa `GardenSoilDry.mat` para verse café claro desde el editor y `GardenSoil` para oscurecerse al acumular 100 ml en esa maceta. `wetThresholdMillilitres` y ambos colores se pueden cambiar en el Inspector. El color se restaura desde el agua guardada al reiniciar.

## Arte provisional

`SeedItem.BuildPlant` crea tallos, hojas y fruto/raíz con primitivas de Unity. Sustituya esos objetos provisionales por modelos definitivos de brote y planta de tomate, rábano y lechuga, conservando el cambio de etapa y el anclaje a la maceta. El proyecto incluye modelos `Sprout.fbx` y `Tomato.fbx`; conviene revisar su escala y si representan la etapa deseada antes de asignarlos.
