# Saludo y aprendizaje de movimiento

Guion acordado: `GronnyWelcomeAndMovementDialogues.json`.
El lote original generó cuatro WAV con el generador local existente, KittenTTS v2,
voz Kiki, preset expressive y temperature 1.1. La revisión del 28 de octubre
de 2026 sustituye únicamente el saludo con esa misma configuración.
No se instalan paquetes.

| Grabación | Duración | Uso |
| --- | ---: | --- |
| `01_welcome` | 18,60 s | Presentación de Gronny, manos frente al usuario si utiliza hand tracking, cultivo de tomate, jardinería sustentable y reserva limitada después de practicar. |
| `controls_move_hands` | 10,16 s | Explicación del gesto de movimiento. |
| `controls_move_controllers` | 4,84 s | Explicación del joystick izquierdo. |
| `movement_success` | 3,16 s | Felicitación e invitación a acercarse a la mesa. |

Los tres WAV anteriores se sustituyen en sus rutas existentes, conservando
sus `.meta`. La felicitación se añade en `Assets/Audio/water_saving`.
Las copias anteriores están en la ruta indicada por
`output/WelcomeMovementBackup.txt`, junto al catálogo y al manifiesto anteriores.
El catálogo pasa a 58 audios: 45 de lección y 13 de menú/accesibilidad.

## Secuencia

Después de Play, Gronny da el nuevo saludo. Sigue directamente la instrucción
de movimiento del modo de entrada activo. No se repite la explicación de la
reserva entre el saludo y el movimiento. El detalle de las recargas se ofrece
cuando la regadera se vacía durante el cultivo con suministro limitado.

Justo después de presentarse, el saludo pide: "If you're using hand tracking,
hold both hands comfortably in front of you where you can see them."
Esta indicación se oye aunque todavía no se haya detectado el modo de entrada.
Se conserva completa la explicación de jardinería sustentable y suministro
limitado. La invitación a acercarse a la mesa se elimina del saludo y se mantiene
únicamente en `movement_success`, después de practicar el movimiento.

La detección de movimiento se activa al empezar su propia instrucción, evitando
que caminar durante el saludo salte la lección. Requiere un evento de traslación
relativa o velocidad de Meta y al menos 10 cm de desplazamiento horizontal del
rig. Se aceptan incrementos pequeños del joystick; la condición anterior exigía
un centímetro por evento y podía ignorar pasos lentos a frecuencias altas.
Mover solo la cabeza, girar, subir/bajar o esperar no completa la lección.

Al completar el desplazamiento se encola `movement_success` una sola vez,
antes del agarre. La frase en curso termina con normalidad. La demostración
mantiene sus pausas visuales de al menos 2,5 segundos; la voz conserva pitch 1.

## Verificación de la revisión del 28 de octubre de 2026

Guion de esta sustitución: `GronnyHandsStartupDialogues.json`. Se sincronizan
los tres guiones vigentes de bienvenida y lección, la transcripción del catálogo,
el inventario y el manifiesto de hashes. El WAV conserva su ruta y su `.meta`.
La copia anterior y los documentos previos están en la carpeta indicada por
`output/HandsStartupBackup.txt`.

`output/HandsStartupAudioQA.json` registra 18,6 s, 24 kHz, señal válida y ninguna
muestra saturada. La transcripción coincide salvo el nombre Gronny, reconocido
como Granny, igual que en el lote anterior; su pronunciación requiere escucha
humana. `output/HandsStartupIntegrity.json` confirma que solo cambió el WAV del
saludo entre los audios y escenas registrados y que se conservaron los `.meta`.
La validación en Unity queda pendiente: Pipeline detecta el Editor 6000.3.23f1,
pero la consulta de estado mediante `eval` y `unity recompile` agotan el tiempo
de espera del hilo principal. Informes: `output/HandsStartupEditorBefore.json`
y `output/HandsStartupCompile.json`. No se ejecutó Play ni se modificaron
paquetes para intentar resolver la conexión. También queda pendiente la escucha
y el comportamiento físico en Quest 3. Los informes siguientes pertenecen al
lote anterior.

## Verificación del lote original del 27 de octubre de 2026

`output/WelcomeMovementAudioQA.json` contiene transcripción y medidas de señal.
Los cuatro archivos tienen señal válida, 24 kHz y ninguna muestra saturada.
Las instrucciones y la felicitación coinciden con el guion tras normalizar
puntuación y contracciones. Whisper reconoce el nombre Gronny como Granny;
el resto del saludo coincide. Esto requiere valoración humana del nombre y
no se considera una coincidencia literal completa.

`GronnyAudioVerification.VerifyCatalogue` comprueba los 58 clips, hashes,
referencias Meta y tiempos de demostración. La migración de catálogo a versión 3
adapta una sola vez los tiempos de las dos demostraciones de movimiento a las
nuevas duraciones.

`Garden > Gronny > Verify welcome and movement in Play` comprueba la secuencia
real con AudioSource y eventos Meta simulados, sin guardar cambios de escena.
Informe: `output/WelcomeMovementPlay.txt`. Las pruebas no sustituyen la escucha
ni el seguimiento de manos en Quest 3.

Resultados: compilación con cero errores y cero advertencias; catálogo completo
correcto; 15 comprobaciones de movimiento, 21 de continuidad de audio y 89 de
menú/suministro aprobadas. Resumen en `output/WelcomeMovementResults.json`.
Los informes de regresión se conservan como `WelcomeMovementContinuityPlay.txt`
y `WelcomeMovementMenuPlay.txt`. Solo cambiaron los tres WAV previstos; todos
los `.meta` anteriores conservan su hash (`WelcomeMovementIntegrity.json`).
