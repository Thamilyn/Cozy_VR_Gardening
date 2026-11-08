# Corrección de referencias de maceta y Meta — 8 de noviembre de 2026

Se corrigieron los dos errores detectados durante la validación de las guías animadas, sin actualizar Unity, Meta SDK ni dependencias.

## Cambios

- `Assets/OurAss/GrabablePrfab/Shovel.prefab`: el `InteractableUnityEventWrapper` de `ISDK_HandGrabInteraction` tenía `_interactableView` vacío. Ahora referencia el `HandGrabInteractable` existente en el mismo objeto. Se conservan los componentes de agarre de manos y mandos, sus eventos y el Rigidbody.
- `Assets/OurAss/GrabablePrfab/PotSmall.prefab`: la maceta decorativa `PotSmall (2)` llevaba un `TomatoPotStages` sin superficie ni `GardenSoil`, aunque no tiene `GardenPot` ni participa en el cultivo. Se retiró únicamente ese componente inválido y su entrada en la lista del objeto. Se conservan mallas, materiales, colisiones y agarres.

La maceta de cultivo `small-pot-1` conserva su `TomatoPotStages`, superficie, tierra y siete referencias de crecimiento. La maceta configurada de `Side2_Pots` también conserva su configuración. No se modificó la escena ni sus referencias de plantación.

## Validación

- Inspecciones antes/después en Unity 6000.3.23f1: `Docs/Validation/GardenReferenceErrorsBefore.json` y `GardenReferenceErrorsAfter.json`. Tras importar los dos prefabs no hay wrappers sin Interactable View ni controladores de maceta sin superficie asignada.
- 14 comprobaciones aprobadas en Play pausado: ambas macetas configuradas, sus siete apariencias y visual inicial, conservación de la maceta de cultivo y de la decorativa, wrapper Meta con Awake/Start terminados, enlace con HandGrabInteractable, Rigidbody compartido por los agarres y arranque completo de GardenShovel. El bloqueo temporal del agarre por menú/tutorial se respeta. Informes: `GardenReferenceRegression.txt` y `GardenReferenceRegressionResult.json`.
- En un nuevo arranque no reaparecieron los dos errores ni se registraron excepciones en el tramo completo de Editor.log. El historial limitado de Pipeline se desbordó con las advertencias XR; por eso se comprobó el archivo completo desde el desplazamiento de bytes registrado antes de entrar en Play. Informes: `GardenReferenceStartup.log`, `GardenReferenceStartupResult.json`, `GardenReferenceLogStart.json` y `GardenReferenceConsoleAfter.json`.
- Los prefabs se compararon con sus copias previas en `output/GardenReferenceFix_20261108/`: únicamente cambiaron la referencia Meta y el componente decorativo inválido. Se conservaron los `.meta` existentes.
- Estado final: Editor fuera de Play, sin pausa, sin compilación fallida y `Garden_Moves` sin cambios pendientes. Informe: `GardenReferenceFinalState.json`. No se guardó la escena, no se generó APK ni se probó Quest 3.

Persisten advertencias de XR al ejecutar sin visor. Las comprobaciones verifican configuración y arranque; queda pendiente el agarre físico con manos y mandos en Quest 3. Los informes de error anteriores se conservan como historial y no describen el resultado del nuevo arranque.
