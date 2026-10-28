# Lección de ahorro de agua y grabaciones actualizadas

Fecha: 27 de octubre de 2026.

Flujo vigente: una sola escena, `Garden_Moves`, con tutorial y cultivo continuos.
Se retiró la práctica separada. Las voces vuelven a su tono y velocidad originales;
únicamente las demostraciones visuales de manos tienen pausas adicionales.

Actualización vigente: se revirtió la separación de práctica y jardín a petición
del usuario. El tutorial progresivo vuelve a `Garden_Moves` con el mismo cultivo
y el suministro de práctica anterior. Las manos tienen un ritmo de enseñanza
más lento. Véase `ReversionTutorial.md`; las pruebas de la separación ya no
describen la versión vigente.

## Grabaciones

Se importaron inicialmente los 44 WAV de `C:/Users/Trole/Documents/generate_voice/audios`
sin ejecutar ni modificar ese generador. Posteriormente se regeneraron tres
instrucciones accesibles de semilla y el saludo con el generador local existente,
sin modificarlo. Los 27 archivos existentes se
sustituyeron en sus rutas originales, manteniendo sus `.meta` y referencias. Se añadieron 17 WAV en
`Assets/Audio/water_saving`. Las copias anteriores están dentro del proyecto,
en la carpeta indicada en `Docs/WaterSavingAudioImport.json`.

El manifiesto conserva SHA-256 anteriores y posteriores, incluido el de la
bienvenida anterior respaldada. El catálogo contiene 57 cues: 44 de la lección
y 13 del menú/accesibilidad.
Los IDs originales reproducen directamente las grabaciones actuales,
sin redirigir a las versiones previas `*_left` o `*_accessible`. Los rótulos
Tomato y Ready to harvest siguen disponibles como identificación adicional.
Las previsualizaciones derechas y las voces de menú se conservan.

### Identificación de la semilla sin depender del color

Se regeneraron `controls_grab_hands`, `controls_grab_controllers` y
`03_plant_seed` con la misma voz Kiki. Ahora identifican la semilla por su
etiqueta Tomato, sin pedir reconocer el rojo. El guion está en
`Docs/GronnySeedAccessibleDialogues.json` y también actualiza el guion general.
Los WAV se reemplazaron en sus rutas existentes, conservando todos los `.meta`.
La bienvenida y las otras 54 grabaciones no cambiaron en esta revisión.

El manifiesto conserva los hashes originales y añade `override_sha256` y la
copia de la revisión anterior para estos tres archivos. La versión 2 del catálogo
ajusta una sola vez los tiempos visuales a las nuevas duraciones; reconectar no
vuelve a escalarlos. El generador de origen fuera del proyecto no se modificó.

Validación de esta revisión: las tres transcripciones automáticas coinciden con
el guion y no presentan saturación (`output/SeedAudioVerification.json`), la
compilación terminó sin errores ni advertencias (`output/SeedAudioCompile.json`),
el catálogo de 57 audios pasó (`output/SeedAudioCatalogue.json`) y la reconexión
conserva los tiempos (`output/SeedAudioMigrationCheck.json`). La escucha y la
legibilidad de la etiqueta en Quest 3 quedan pendientes. La batería de Play
descrita más abajo corresponde a la revisión anterior y no se repitió para este
cambio de audio.

La instrucción de plantación se precisó después: «Move the seed with the Tomato
label over the soil in the centre of an empty small pot. Release the seed into
the pot to plant it». Explica dónde soltarla y que así se planta, en lugar del
ambiguo «then let go». Esta revisión modifica únicamente `03_plant_seed`, que
no tiene pasos visuales temporizados. El informe de reconocimiento de esta
versión es `output/PlantingClarityVerification.json`. La importación y validación
del catálogo pasaron en `output/PlantingClarityCatalogue.json` (duración 7,76 s).

`Docs/GronnyWaterSavingDialogues.json` contiene los textos de las 44 grabaciones,
leídos del guion del generador. Garden > Gronny > Connect water-saving lesson
actualiza referencias y textos. La primera ejecución retima los pasos visuales
según la duración anterior y la nueva; las siguientes conservan esos tiempos.
El agarre de regadera tiene una demostración propia con mano cerrada o Grip,
separada de la explicación de inclinar y enderezar. Los controles de rellenado
tienen sus grabaciones y demostraciones de selección correspondientes.

## Saludo después de Play

El menú de lanzamiento no reproduce un saludo automático. Tras pulsar Play y
soltar la selección, Gronny reproduce una sola vez `01_welcome`: «Hello! I'm
Gronny. Come over to the gardening table, and let's get started». La misma voz
Kiki invita a acercarse a la mesa. Después continúa `water_reserve_explanation`:
Gronny explica desde el principio que rellenar transfiere agua de la reserva y
no crea agua nueva. Los controles conservan su orden posterior y no se añaden
`menu_intro` ni `02_tomato_intro`. Volver del menú de pausa reanuda la voz sin
repetir el saludo. La invitación no impone una nueva barrera de distancia.

El guion está en `Docs/GronnyWelcomeDialogues.json` y en el guion general. El WAV
se reemplaza en su ruta original, conservando el `.meta` y el identificador. El
manifiesto conserva el hash y la copia de la bienvenida anterior y registra la
nueva grabación como sustitución aprobada.

Se restauró la toma que incluye «I'm Gronny», de 4,84 s, manteniendo el flujo
después de Play. `output/WelcomeSelfIntroductionCatalogue.json` comprueba la
referencia y el texto. Solo se sustituyó `01_welcome.wav`, conservando todos los
`.meta` y las otras grabaciones. La transcripción automática de esta toma
coincide salvo el nombre propio, que reconoce como «Granny»
(`output/WelcomeAfterPlayNameRecognition.json`); la pronunciación del nombre
queda pendiente de revisión auditiva. Las 87 comprobaciones de Play anteriores
validan el mismo flujo, pero no se repitieron para esta sustitución de audio.

El menú también explica por escrito que primero hay práctica con recarga libre y
después una reserva limitada. Al empezar la parte limitada se reproducen su
introducción y reglas; no se repite la explicación de recarga del inicio.

## Práctica y reserva

`GardenWaterSupply` se crea junto al menú únicamente en Garden_Moves. Las
preferencias de sonido y el resto del menú se mantienen.

La herramienta de riego es `Watering`. Se retiró `WateringCan` de `bottle-oil`
en la escena y el instalador automático ahora busca `Watering`. La botella
conserva agarre y física, pero no vierte, recibe recargas ni participa en el
presupuesto del agua. Revisión estática realizada; comprobación en Play pendiente.

- Durante el primer riego guiado se practica con recarga gratuita. Una regadera
  vacía se repone al sostenerla, sin agotar una reserva ni terminar la actividad.
  El exceso sigue pidiendo detener el vertido y esperar al drenaje existente.
- Al completar el primer avance a brote, comienza la reserva limitada. Se calcula
  sumando los objetivos de agua que quedan y añadiendo un 100 % de margen.
- La capacidad de trabajo de cada regadera es 240 ml y el caudal es 10 ml/s:
  una regadera llena dura 24 segundos. Al iniciar la reserva se
  descartan los sobrantes de práctica y se llena una sola regadera desde la reserva.
  Agua en regaderas más reserva equivale exactamente al presupuesto disponible.
- Todas las recargas, incluido B y el botón del calendario, pasan por la misma
  transferencia. Una regadera parcialmente llena recibe solo el espacio libre;
  una reserva insuficiente permite una recarga parcial y nunca queda negativa.
- Las regaderas creadas durante la parte limitada se registran vacías; no añaden
  agua gratis. Recuperar una regadera no altera su contenido.
- La reserva se muestra en su marcador fijo junto a Gronny y cada regadera muestra
  su propio nivel. Comparten la barra de las plantas y un mensaje breve. El tanque
  añade la cantidad restante en texto pequeño. La práctica se identifica como
  recarga libre en el tanque.

Los valores de capacidad y margen se pueden ajustar en GardenWaterSupply; los
objetivos agronómicos del prototipo siguen viniendo de SeedsController. Estos
valores son parámetros de juego, no una nueva recomendación real de riego.

### Marcador del estanque junto a Gronny

`GardenWaterTankDisplay` se crea al iniciar el suministro, a la derecha del panel
de Gronny, separado por 10 cm. El indicador mide 36 × 28 cm, con una barra de
30 cm. Al primer Play, `GardenMenuController.PlacePauseStation` intercambia su
posición y orientación con el botón Menu: el tanque queda sobre la guía y Menu
ocupa el antiguo lugar del tanque, a la derecha. Ambos quedan fijos en el jardín;
abrir/cerrar el menú no repite el intercambio. Usa el mismo `GardenWaterIndicator` que la
planta: fondo blanco redondeado, doce segmentos y un único mensaje debajo.
Muestra la proporción de reserva respecto a `InitialReserveMillilitres`.
Debajo del estado añade la cantidad restante con letra de 28 puntos frente a
los 36 del estado: ml por debajo de 1000 ml (hasta un decimal) o L a partir de
un litro (hasta dos decimales). En práctica indica `Unlimited` para no mostrar
una reserva finita inexistente. El texto usa la reserva real, sin sumar el agua
de las regaderas, y se actualiza junto con la barra.
Esta referencia se fija al empezar el intento limitado, después de llenar la
primera regadera. Así la barra empieza llena y baja con las siguientes recargas,
en lugar de aparecer ya reducida cuando Gronny anuncia el suministro limitado.
Usa `ReserveMillilitres`: no incluye el agua que ya se transfirió a las regaderas.
El presupuesto total y el reparto inicial de agua se conservan. El aviso ámbar
y la voz de reserva baja usan el mismo umbral: un cuarto de la reserva inicial.
Se actualiza cada 0,1 s, también durante pausa. En práctica muestra la barra
llena con `Tank: free refills`. Los mensajes de reserva disponible, baja, vacía y
reintento son breves; cian, ámbar y rojo son los colores compartidos con las
plantas y las regaderas. No tiene fondo de panel adicional, collider, raycaster
ni controles que intercepten los rayos Meta.

### Nivel de cada regadera

`WateringCan.Start` añade `WateringCanWaterDisplay` si todavía no existe. No hay
que editar escenas ni prefabs. Lee `WaterNormalized` y `CurrentWaterLitres`:
la barra baja al verter, sube al rellenar y refleja también una recarga parcial.
No mantiene otro contador ni altera el presupuesto del suministro.

La barra mide 18 cm y sigue la regadera por encima de su geometría, orientándose
hacia el visor incluso al inclinarla. Excluye el LineRenderer del chorro al
calcular la altura. El mensaje identifica la regadera (`Can: full`,
`Can: water remaining`, `Can: low water` o `Can: empty`), sin números.
Con un cuarto de capacidad o menos pasa a ámbar y vacía pasa a rojo, con texto
para distinguir ambos estados. Se oculta si se deshabilita `WateringCan` y se
elimina con la regadera. No incorpora collider, raycaster ni interacción Meta.

Se revisaron estáticamente la vista compartida, la instalación automática, los
valores leídos, las escalas y la limpieza del indicador. No se ejecutaron Unity,
recompilación ni pruebas automatizadas para esta revisión. Queda pendiente
comprobar en Play y Quest 3 el tamaño y la orientación al agarrar/inclinar,
vertido y recargas completas/parciales, práctica, transición a reserva limitada,
tanque vacío con agua todavía en la regadera, pausa, reintento y cosecha.
La corrección del nivel inicial también se revisó estáticamente: al pasar la
primera fase debe mostrarse el tanque lleno tras el reparto inicial; después,
las recargas deben reducir la barra y activar el aviso y la voz de reserva baja
al mismo nivel. Esta comprobación en ejecución sigue pendiente.
La etiqueta de cantidad del tanque se revisó estáticamente; comprobar su
legibilidad, separación del estado, cambio de unidades y actualización durante
recargas en Unity y Quest 3 sigue pendiente. No se ejecutaron pruebas para este
ajuste visual.
El intercambio de posiciones del tanque y el botón Menu se revisó estáticamente.
Se actualizaron las comprobaciones existentes de colocación, sin ejecutarlas.
Queda pendiente validar en Unity y Quest 3 la separación de la cantidad respecto
a la guía y la selección Meta del botón en su nueva posición.

## Agotamiento y reintento

Una regadera vacía muestra CAN EMPTY y cómo rellenarla si queda reserva; si el
agua está en otra regadera, indica usar esa otra. La reserva vacía muestra
RESERVE EMPTY y el agua de las regaderas cuando todavía alcanza para terminar.
Se declara fin del intento en cuanto el agua disponible (reserva y regaderas
activas) ya no cubre todos los objetivos pendientes. Así no se deja al jugador
continuar un cultivo imposible hasta gastar la última gota. Si queda exactamente
el agua necesaria, el intento continúa; cumplir el último objetivo sin sobrante
permite avanzar a madurez y cosechar normalmente.

En agotamiento se detienen riego y crecimiento y aparece automáticamente un panel
Game over frente al jugador, con resumen y botón Try again. Tiene su propio
ray target Meta y exige soltar la selección antes de aceptar el clic. El menú
habitual conserva su estación fija junto a Gronny. El panel de fin de agua usa la
colocación de presentaciones existente para evitar objetos y mantenerlo visible.
Si queda fuera de la vista o su Canvas está oculto por falta de espacio, se
reintenta colocarlo frente al jugador cada medio segundo. Los mensajes explican
si el agua se acabó por completo o si el remanente ya no alcanza para terminar.

El audio del panel anuncia el reintento mientras el jardín está pausado; la voz
de agotamiento total solo se usa si no queda agua. No depende de que
GronnyTutorial esté activo. El botón Restart del
calendario también conserva su alternativa Try again. Al reintentar se cierran el
panel y su narración, y se exige una liberación de la entrada antes de reanudar.

Try again recarga Garden_Moves y comienza una sesión nueva en la plantación:
semilla sin plantar, macetas vacías, día 0, herramientas en su posición inicial,
recargas libres de práctica y contadores/diario a cero. El marcador del estanque
vuelve a `Tank: free refills`. Se eliminan los reproductores anteriores, sus colas y el
historial de diálogos. La sesión arranca automáticamente tras cargar la escena
y espera a soltar la selección de reintento antes de habilitar el juego y la voz.
Gronny comienza con `03_plant_seed`. Movimiento y agarre se consideran aprendidos
para entrar directamente en la siembra; las lecciones de riego, calendario y
crecimiento se repiten desde ese nuevo cultivo. Las preferencias de sonido se
conservan. El menú, el panel Game over y la alternativa del calendario usan el
mismo reintento; pulsarlo varias veces solo programa una recarga de escena.
El Restart normal del calendario conserva el arranque completo con menú inicial.

## Qué se mide

La salida real de cada regadera se divide en:

- Agua útil: cantidad que realmente aumenta el registro de la maceta plantada,
  hasta cubrir el objetivo de su fase.
- Exceso: agua que llega a esa tierra después de cubrir el objetivo.
- Fuera de objetivo: agua vertida sin llegar a la tierra de la planta objetivo.

Para la parte limitada se cumple `vertido = útil + exceso + fuera de objetivo`.
La práctica conserva sus propios contadores de vertido y agua útil. El resumen
final incluye práctica y el intento de la sesión actual. Un reintento desde
plantación reinicia estos contadores. El agua que queda sin usar no se
considera desperdicio. El drenaje no borra agua ya vertida ni devuelve agua a la
reserva. No se simula absorción radicular: “Water used” indica agua aplicada
útilmente según el objetivo educativo del prototipo.

La felicitación `water_no_waste_complete` solo se reproduce cuando no hay
desperdicio medido, incluida la práctica; no se deduce de haber cosechado. Al
cosechar se cierran el vertido y las recargas para conservar el balance final.

## Protección de interacción

Abrir el menú, cambiar de fase o perder seguimiento detiene el vertido antes de
descontar agua. En Quest se comprueba la presencia del usuario y la validez del
interactor que sostiene la regadera. Al recuperarse el seguimiento hay que
enderezar la regadera antes de volver a verter. Los ray targets de UI y los
colliders del propio rig no cuentan como obstáculos que desperdicien el chorro.

El modo Editor sin visor permite los agarres sintéticos de las comprobaciones;
ese caso no valida confianza de manos, mandos ni presencia en un dispositivo.

## Validación

- Reintento desde plantación: revisión estática del flujo de recarga, reinicio
  de voces y entrada directa a `03_plant_seed`. Se actualizaron las comprobaciones
  de Play para verificar sesión vacía, preferencias conservadas, liberación de
  entrada, primer diálogo de plantación y bloqueo de recargas duplicadas. Estas
  comprobaciones no se han ejecutado; compilación y prueba en Unity/Quest 3
  pendientes. Los informes históricos de abajo corresponden al reintento anterior
  que conservaba la planta y no validan este comportamiento nuevo.

- Ajuste de equilibrio y detección de intento imposible (27 de octubre): revisión
  estática del código y de los dos caudales serializados de Garden_Moves. Se
  ampliaron las comprobaciones existentes para capacidad/caudal, margen del
  presupuesto, suficiencia exacta, remanente insuficiente y segundo reintento
  conservando el cultivo. **Estas comprobaciones nuevas no se han ejecutado.**
  `unity status` no encontró un Editor conectado a Pipeline. Compilación, Play y
  validación física en Quest 3 pendientes; no se inició Unity ni se instalaron
  herramientas o paquetes.

- Revisión del saludo después de Play: compilación sin errores ni advertencias
  (`output/WelcomeAfterPlayCompile.json`), catálogo de 57 audios correcto
  (`output/WelcomeAfterPlayCatalogue.json`) y 87 comprobaciones correctas de Play
  (`output/WelcomeAfterPlayVerification.txt`). Se comprueba el menú inicial sin
  saludo, la espera a soltar Play, saludo antes de la reserva y continuación
  desde pausa sin repetirlo, además de las regresiones de menú y suministro.
  La transcripción de la grabación de 3,96 s coincide con el guion y no presenta
  saturación (`output/WelcomeAfterPlayVoiceVerification.json`). Solo cambió el
  WAV de bienvenida; sus `.meta` y los otros 56 audios se conservaron. El Editor
  quedó fuera de Play y la escena sin cambios pendientes
  (`output/WelcomeAfterPlayScenesAfter.json`). Escucha física en Quest 3 pendiente.

Las comprobaciones siguientes corresponden a la revisión anterior del inicio:

- Comprobación de catálogo de 57 audios correcta, incluida coincidencia SHA-256
  de las 44 grabaciones suministradas, ahora con el welcome corto:
  `output/ShortWelcomeCatalogue.json`.
- Compilación sin errores ni advertencias de C# y `compilationFailed=false`:
  `output/WaterGameOverCompile.json`.
- Pruebas ampliadas de menú y suministro en Play: 85 comprobaciones correctas,
  en `output/WaterGameOverPlayVerification.txt`. Cubren bienvenida corta en el
  lanzamiento, explicación inmediata de reserva al pulsar Play, aviso de reserva
  vacía, panel automático de Game over visible e interactivo, narración durante
  la pausa y coherencia del cartel de Gronny. También cubren recarga libre de práctica,
  transferencia finita directa y por calendario, regaderas nuevas vacías,
  vertido real sobre tierra y fuera de maceta, separación de exceso, balance de
  volumen, agotamiento, reintento conservando el cultivo, resumen cerrado tras
  cosecha y ausencia de falso agotamiento al cumplir el último objetivo.
  La batería también mantiene las regresiones de voces, menú, pausa y riego.
- Captura del panel: `output/WaterGameOver.png`.
- El Editor quedó fuera de Play y Garden_Moves sin cambios pendientes:
  `output/WaterGameOverEditorAfter.json` y `output/WaterGameOverScenesAfter.json`.
- Pendiente en Quest 3: fiabilidad del chorro, confianza del seguimiento, lectura
  del suministro/resumen, claridad del agarre de regadera y sincronización de voz.
