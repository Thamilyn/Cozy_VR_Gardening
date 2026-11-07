# Preparación de tierra con SoilPot y Shovel

Escena: `Assets/Scenes/Garden_Moves.unity`.

## Implementación revisada estáticamente

- La maceta `small-pot-1` tiene `startsWithSoil: 0`. Se oculta únicamente el material de tierra del modelo inicial; se conservan el macetero, sus referencias y la superficie usada para medir el riego.
- El prefab existente `SoilPot` incorpora `GardenSoilSource`. Su zona de recogida se obtiene del submesh con material Soil y sigue las transformaciones del asset. La instancia de la escena queda junto a la maceta en la mesa.
- El prefab existente `Shovel` incorpora `GardenShovel` y reutiliza `GardenGrabSetup` para manos y mandos Meta. Solo una pala sostenida puede recoger o depositar tierra.
- Una carga visible pasa del SoilPot a la pala y se consume al rellenar una maceta vacía, con una sola palada. Una pala vacía no rellena macetas.
- La siembra y la recepción de agua requieren tierra. El tutorial permite agarrar la pala y la semilla durante el paso de preparación; la instrucción de siembra continúa después de rellenar la maceta. El agarre de la semilla funciona tanto con manos como con mandos sin depender de que la maceta tenga tierra.
- El collider de soporte del macetero conserva la base y termina a la altura de la tierra, en lugar de cerrar toda la abertura hasta el borde. La comprobación existente de alineación en `GardenCalendarVerification` incluye esta condición; aún no se ha ejecutado.
- Después de una suelta durante el juego, la semilla sigue comprobando la siembra durante los dos segundos previos a su recuperación. Puede entrar en la zona de tierra después de soltarse. Los intentos intermedios no repiten avisos; el rechazo se comunica al terminar el plazo. Volver a agarrarla cancela la siembra pendiente; una suelta durante el menú solo activa la recuperación.
- El menú suspende la transferencia de tierra. La pala caída vuelve a su posición inicial después de dos segundos; conserva su carga. El reinicio existente recarga la escena, restableciendo la maceta vacía.
- No hay fileIDs duplicados en los tres assets YAML modificados. Los cuatro GUID nuevos son únicos entre los archivos `.meta` del proyecto.

## Validación pendiente

`unity status --project-path "C:\Users\Trole\Cozy_VR_Gardening" --format json` detectó una instancia de Unity 6000.3.23f1 en el puerto 7800, pero devolvió `STATUS_ALL_UNREACHABLE`. La versión coincide con `ProjectSettings/ProjectVersion.txt`.

No se ejecutaron compilaciones, pruebas automatizadas, Play Mode ni pruebas en Meta Quest 3. No se inició el Editor ni se instalaron paquetes.

Al probar en Unity y Quest 3, comprobar:

1. La maceta comienza sin tierra visible y el SoilPot queda apoyado en la mesa, sin solaparse con otros útiles.
2. Se puede agarrar la semilla durante el paso de tierra, tanto con manos como con mandos. Soltarla con la maceta aún vacía no la planta y activa su recuperación. Tras rellenar, soltar la semilla sobre la maceta y también desde más arriba: debe quedar plantada aunque entre en la zona de siembra después de la suelta, sin rodar sobre una colisión invisible a la altura del borde.
3. Con manos y con mandos, introducir la hoja de la pala en la tierra muestra una carga alineada con la hoja. Tocar los lados del SoilPot o acercar solo el mango no recoge tierra.
4. Llevar la pala cargada sobre la abertura de la maceta muestra la tierra y vacía la pala. Pasar una pala vacía o acercarla a los lados de la maceta no la rellena.
5. El nuevo paso muestra instrucciones legibles en el panel. Tras completarlo continúa la siembra y la secuencia de riego, calendario, crecimiento y cosecha existentes. Comprobar también la transición si ya se sostiene la semilla con la otra mano al depositar la tierra.
6. Pausar con la pala cargada conserva la carga y no rellena la maceta. Reanudar permite continuar.
7. Soltar la pala al suelo permite recuperarla en la mesa; reiniciar o reintentar vuelve a exigir preparar la tierra.
8. El material que oculta la tierra funciona en la compilación Android con el modo estéreo de Quest 3.
9. Soltar la semilla fuera de la maceta produce un único rechazo al terminar la espera y la devuelve a su posición inicial. Volver a agarrarla antes cancela la recuperación; soltarla durante el menú no la planta al reanudar.
