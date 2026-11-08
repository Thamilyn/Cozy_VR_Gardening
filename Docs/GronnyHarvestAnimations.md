# Guías animadas de corte y recogida

El tutorial de cosecha utiliza el sistema existente `GardenControlDemonstration`, sus manos de Meta y sus modelos Touch Plus. Se añaden las categorías `CutStem = 8` y `PickTomato = 9`, conservando los valores de las demostraciones anteriores.

## Comportamiento

- **Antes de cortar:** muestra un tomate rojo unido por un tallo y una copia visual de Pruner. La mano pasa de agarre a apertura y cierre; en mandos se anima el gatillo y se señala el control. Las hojas se cierran sobre el tallo y el ejemplo muestra el fruto entero separado, sin trocearlo. Las leyendas son específicas de manos o mandos.
- **Después del corte real:** sustituye la guía por acercamiento, pinza/Grip y elevación del tomate separado. No espera a que termine una frase anterior para corregir la guía visual; el audio conserva su continuidad.
- **Después del agarre real:** retira la animación. Las felicitaciones y el cierre posterior en la bandeja mantienen su orden y no se repiten.
- Durante la voz, los pasos siguen el tiempo del clip. Una vez terminada, la guía sigue en un ciclo visual sin repetir audio hasta completar la acción. Las poses de práctica se mantienen al menos 2,5 s y las transiciones usan el suavizado existente. El ciclo de recogida dura 7,5 s aunque su voz dure 3,24 s.
- Las guías se ocultan durante el menú y al cambiar de modo; se usa la misma posición de demostraciones de jardinería junto a Gronny. No se mueven con la herramienta real ni siguen la cabeza cuando hay un anclaje fijo.

`GardenHarvestDemonstration` copia solo Transform, MeshFilter, MeshRenderer y materiales del prefab existente de Pruner. No instancia sus componentes de juego ni ejecuta su Awake. Las piezas de ejemplo no tienen agarres, Rigidbody dinámicos, colisiones ni eventos que completen la tarea o registren cosecha. Sus nombres llevan el prefijo Example para que el escáner de utilería no los convierta en objetos agarrables. Se liberan los materiales propios al ocultar la guía.

## Inspector

Todo está asignado en `Assets/Resources/GronnyAudio/Tutorial.asset`:

- **Pruning Shears Visual:** `Assets/MamkinEnthusiast/3D Mini Garden Props/Prefabs/Pruner.prefab`.
- `harvest_cut_stem`: **Demo = CutStem**, cuatro pasos a 0; 3,2; 8,5 y 10,8 s; animación de gatillo y su referencia de control.
- `harvest_stem_cut`: **Demo = PickTomato**, tres pasos a 0; 1 y 2,4 s; animación de Grip y su referencia de control.
- `harvest_picked`: **Demo = None**; se conserva la felicitación sin otra acción de ejemplo.

Se conservan los tres WAV, sus referencias y transcripciones. No requiere editar la escena, añadir componentes a Pruner ni cambiar Meta SDK. Los pasos pueden ajustarse desde el catálogo sin modificar las herramientas reales. Para volver a aplicar la configuración: `Docs/Validation/ConfigureHarvestAnimations.cs`, mediante la validación autorizada en Unity.

## Verificación realizada

- Compilación con Unity 6000.3.23f1: cero errores y cero advertencias del compilador. Pipeline avisa de sus nombres antiguos; no se actualizó.
- **30 comprobaciones** de `HarvestAnimationRegression.cs`: ambas entradas, copia visual sin interacción, hojas abiertas/cerradas, deformación real de la malla de mano, fruto entero, leyendas adecuadas, levantamiento, ritmo de práctica, limpieza por cambio de entrada, permanencia sin voz, cambio inmediato al cortar y ocultación al recoger/abrir el menú. Las poses y cambios de contexto se suministraron de forma sintética.
- **17 comprobaciones** de voz repetidas con la nueva configuración: madurez, corte con la herramienta existente, Select de Meta, entrega rápida, felicitaciones conservadas, orden, fuente única y ausencia de duplicados.
- Revisadas las vistas renderizadas de apertura, cierre, pinza/Grip y elevación para manos y mandos en `Docs/Validation/HarvestAnimations/`. Las capturas en Play pausado hornean temporalmente las mallas animadas y compensan la escala del FBX; el juego conserva los SkinnedMeshRenderer de Meta.
- Informes: `HarvestAnimationCompilation.json`, `HarvestAnimationRegression.txt`, `HarvestAnimationRegressionResult.json`, `HarvestVoiceWithAnimationsResult.json` y `HarvestAnimationFinalState.json`, en `Docs/Validation/`.

Durante la primera preparación de Play se produjo un cierre nativo de Unity al renderizar Game View. Se conserva `HarvestAnimationEditorCrash.txt`. Se recuperó el Editor sin actualizar componentes y la validación final se realizó en sesiones limpias. Un intento de preparación posterior agotó el plazo del hilo principal antes de estar listo; no se contabiliza como prueba aprobada. Los resultados enumerados son los de las ejecuciones posteriores completadas.

Pendiente: comprobar en Quest 3 el tamaño y la ubicación junto a Gronny, comodidad, legibilidad, orientación de la mano y del mando, tiempos reales de voz y rendimiento. Las comprobaciones sintéticas y las capturas no validan el seguimiento físico ni garantizan la estabilidad general del Editor. No se generó APK ni se guardaron cambios temporales de prueba en la escena.
