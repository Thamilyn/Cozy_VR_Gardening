# Plan de menú y accesibilidad

Estado: primera entrega de código (base de preferencias, menú y suspensión)
preparada, compilada y comprobada con 29 verificaciones sintéticas de Play;
validación física en Quest 3 pendiente. Detalle de alcance,
inventario y límites en `Entrega01MenuYPreferencias.md`. Las entregas posteriores
de biblioteca, imagen y accesibilidad siguen pendientes.

## Objetivo

Incorporar un menú de inicio y un acceso durante la partida para consultar controles,
volver a ver las demostraciones de manos y mandos, y ajustar imagen, sonido y
accesibilidad. Mantener Unity 6000.3.23f1 y las integraciones Meta existentes.

## Situación revisada

- `Garden_Moves` es la escena habilitada para la compilación.
- La interacción de objetos y la selección del calendario ya contemplan ambas manos.
- `CalendarVisibilityController` fija la apertura a la pinza izquierda o Y del mando
  izquierdo. `WateringCan` fija el atajo de rellenado al mando derecho.
- `CalendarPanel.RecoverCan` prioriza la mano derecha y después prueba la izquierda.
- `GronnyTutorial` enseña progresivamente y bloquea otras acciones. Su texto de
  movimiento indica joystick izquierdo; no se ha validado toda la configuración
  efectiva de locomoción del rig en ejecución.
- `GardenControlDemonstration` y el catálogo de Gronny ya proporcionan modelos,
  poses y secuencias reutilizables. Hay instrucciones que nombran una mano o botón.
- `GronnyVoicePlayer` aplica el volumen del catálogo al comenzar cada audio; no se
  encontró un AudioMixer en Assets ni ajustes de usuario en los scripts propios.
- `GardenPhaseTimeCycle` modifica iluminación y exposición del cielo durante las fases.
- El indicador de agua ya tiene cantidades y estados escritos, además de colores.
  El tutorial identifica la semilla de tomate por su color rojo.

Estas observaciones proceden de revisión estática; no certifican usabilidad en visor.

## Flujo propuesto

Menú inicial:

- Jugar.
- Controles y tutoriales: manos, mandos y demostración de cada acción.
- Ajustes: sonido, imagen y accesibilidad.

Durante la partida, el mismo menú ofrecerá Volver al jardín, Controles y tutoriales
y Ajustes. Volver al jardín conserva la sesión actual; no implica implementar
guardado del cultivo entre cierres de la aplicación.

Los ajustes y la ayuda deben estar disponibles desde el primer momento y durante
todos los pasos del tutorial. El acceso no puede depender de un gesto que todavía
no se haya aprendido. Definir su entrada tras revisar los gestos y botones del rig,
evitando colisiones con calendario, rellenado, agarre y gestos del sistema Meta.

## Entregas y orden de trabajo

### 1. Base de ajustes y revisión de entradas

- Inventariar las entradas efectivas del rig: manos, mandos, movimiento, giro,
  calendario, selección, agarre y rellenado.
- Centralizar preferencias de usuario con valores por defecto y restauración.
- Guardar localmente volumen, brillo, perfil de lateralidad y opciones visuales.
- Mantener estas preferencias separadas del progreso del cultivo y del tutorial.
- Definir cómo se suspenden las acciones del jardín al abrir el menú, incluyendo
  regadera sostenida, vertido, transición de fase, voz y demostraciones activas.
  No basar la solución únicamente en detener el tiempo global.

Criterio: las preferencias se aplican antes del tutorial y sobreviven al reinicio;
el menú se puede abrir sin haber completado la enseñanza.

### 2. Menú de inicio y acceso desde el jardín

- Reutilizar los patrones Meta de rayo y PointableCanvas presentes en el calendario.
- Ofrecer botones amplios, texto legible y posición cómoda sentado y de pie.
- Permitir seleccionar con cualquiera de las dos manos o mandos.
- Al entrar, mostrar el menú de lanzamiento frente al punto inicial del jugador,
  fijo en el mundo. La bienvenida espera a Play.
- Al pulsar Play por primera vez, trasladar el menú a su estación fija junto a
  Gronny, encima de «Guía de cultivo y agua»: alineado con el panel, con 8 cm de
  separación. Desde allí es un menú de pausa para ajustes y controles, con Return
  to garden. Abrir, navegar y volver no debe devolverlo al punto de entrada ni
  seguir la mirada. El panel abierto crece hacia arriba. Revisar comodidad en Quest 3.
- Iniciar la bienvenida y enseñanza de Gronny después de pulsar Jugar.
- Evitar acciones accidentales en el jardín mientras se usa el menú; al cerrarlo,
  exigir una nueva pulsación o pinza antes de ejecutar otra acción.

Criterio: abrir, navegar y cerrar con ambas manos y ambos mandos, también durante
el tutorial, sin cambiar agua, fases ni objetos por accidente.

### 3. Biblioteca de controles y tutorial de manos

- Reutilizar las voces y demostraciones existentes para movimiento, agarre y
  liberación, riego, apertura del calendario y selección.
- Permitir elegir manos o mandos, ver la secuencia completa o una acción concreta,
  repetir, avanzar, retroceder y salir en cualquier momento.
- Mantener la elección de la demostración independiente de la entrada usada para
  manejar el menú: se puede mirar el tutorial de manos usando los mandos.
- Separar la reproducción de ayuda de las condiciones de progreso de Gronny.
  Ver una demo no debe completar pasos, reiniciar la planta ni registrar por sí
  mismo acciones de cultivo o asistencia del tutorial original.
- Coordinar una única voz y demostración; al volver, reanudar la instrucción que
  corresponda al estado real de la partida.

Criterio: repetir controles antes, durante y después del tutorial sin perder
progreso ni generar voces o demostraciones superpuestas.

#### Claridad de las animaciones de manos

Requisito añadido por el usuario el 26 de octubre de 2026. Referencia:
[Prioriza mejoras del playtest](https://chatgpt.com/s/cx_6adf962862a08191b243f39f3ebcc5b2).
El resumen compartido recoge demostraciones poco claras y sobrecarga al presentar
voz, texto y demostración a la vez. No se ha vuelto a analizar aquí la hoja de
respuestas original.

- Trabajar primero el movimiento por microgestos, después la pinza de calendario,
  la selección y el agarre/liberación. Comprobar el riego con la misma presentación.
- Mostrar una sola acción por paso: pose inicial, movimiento, pose final y pausa
  suficiente para observarla. Mantener un objetivo breve durante la práctica.
- En movimiento, hacer distinguibles índice doblado, pulgar libre, contacto del
  pulgar con el índice, separación, deslizamiento adelante/atrás e índice extendido
  para salir. La trayectoria debe corresponder al gesto real de Meta.
- Elegir escala y ángulo que permitan ver el contacto entre dedos sin oclusiones.
  Para el movimiento, mostrar la mano sin flechas ni puntos superpuestos: el
  usuario indicó que la flecha confundía. Mantener etiquetas cortas; no hacer
  depender la explicación únicamente del color ni usar destellos.
- Separar visualmente pinzar y soltar, y mostrar el efecto de la acción (panel que
  se abre, botón seleccionado u objeto liberado) en una representación de ejemplo.
- Usar el texto del paso actual. Revisar el texto fijo del calendario en
  `GardenControlDemonstration.Tick`, que actualmente sustituye las etiquetas de
  sus pasos por una instrucción general de abrir/cerrar.
- Alinear cada paso con el WAV que finalmente se active. Las poses actuales se
  muestrean de clips Meta y se interpolan con tiempos del catálogo; no constituyen
  una nueva animación continua sincronizada con las grabaciones de la carpeta externa.
- Permitir repetir la acción y volver a verla despacio desde la biblioteca sin
  modificar el seguimiento real, los pasos aprendidos ni el diario de asistencia.
- Mantener el lado de la demostración coherente con la entrada efectiva. No
  activar la variante derecha mientras falten sus entradas y la solución al conflicto B.

Criterio: una persona nueva puede identificar e imitar cada gesto en Quest 3 sin
que un observador tenga que explicárselo; debe distinguir activación, acción y
liberación. Verificar sentado/de pie y volver a medir claridad y comodidad.
El deslizamiento visible del pulgar ya está implementado y tiene una comprobación
aislada de Play; véase `CorreccionSwipePulgar.md`. El resto de mejoras de claridad
y la prueba física en Quest 3 siguen pendientes.

### 4. Sonido y brillo

- Sonido: volumen general y voz de Gronny; añadir ambiente/efectos para las fuentes
  existentes. Los cambios deben escucharse durante la reproducción actual.
- Añadir silencio, restablecer valores y una muestra de voz solicitada por el usuario.
- Brillo: ajustar la imagen del juego con una vista previa y límites útiles.
  No presentarlo como control del brillo físico del visor.
- Revisar el renderizado existente antes de elegir la implementación; coordinarla
  con `GardenPhaseTimeCycle` y comprobar que no se pierde al cambiar de fase.
- Mantener legibles interfaz y señales de interacción en todo el rango de ajuste.

Criterio: ajustes inmediatos y persistentes, sin saltos al empezar otro audio o
cambiar de fase; coste de renderizado comprobado en Quest 3.

### 5. Lateralidad e inclusión motora

- Ofrecer mano principal derecha o izquierda para herramientas y recuperación.
- Mantener selección y agarre disponibles con ambas manos.
- Permitir configurar por separado la mano de movimiento y la de apertura de
  paneles; elegir mano principal no debe forzar a cambiar el joystick aprendido.
- Resolver conflictos entre calendario y rellenado antes de ofrecer intercambios
  de botones. Conservar alternativas visibles dentro de la interfaz.
- Adaptar textos, modelos, animaciones y voces a la configuración efectiva.
  Inventariar los audios que nombren izquierda, derecha, Y o B y preparar las
  variantes necesarias; cambiar solo el texto no resuelve una voz contradictoria.
- Revisar alcance de herramientas y comodidad sentado usando la calibración
  existente como punto de partida.

Criterio: completar el ciclo de tomate usando preferentemente cualquiera de las
dos manos, con instrucciones coincidentes. No anunciar soporte para jugar con
una sola mano sin una revisión específica de todas las acciones.

### 6. Daltonismo y señales visuales

- Añadir etiquetas e iconos a semillas y estados relevantes: falta de agua,
  objetivo alcanzado, exceso, drenaje y fruto listo para cosechar.
- Identificar la semilla como tomate mediante nombre/icono, además del color.
- Distinguir botones activos, desactivados y seleccionados con señales adicionales.
- Mantener estas ayudas básicas activas para todos los jugadores.
- Ofrecer paletas alternativas orientadas a protanopia, deuteranopia y tritanopia,
  con vista previa en señales reales de agua, semillas y calendario.
- Aplicar las paletas a la información funcional; evaluar cualquier cambio de
  colores del jardín por separado. No depender de un filtro global.
- Revisar las voces que identifican objetos solo por su color y ajustar el guion
  o sus variantes cuando la instrucción resulte insuficiente.

Criterio: identificar objetos y estados sin depender del color; revisar también
contraste, escalas de grises y legibilidad dentro del visor. Las simulaciones son
una ayuda de revisión, no sustituyen pruebas con usuarios.

## Validación final

- Revisión estática de referencias, persistencia y convivencia con Gronny.
- Con autorización para ejecutar la validación: compilación y comprobaciones de
  Unity mediante el CLI instalado, descubriendo antes sus comandos y opciones.
- En Quest 3: manos/mandos, preferencia derecha/izquierda, sentado/de pie,
  seguimiento perdido y recuperado, apertura del menú sosteniendo la regadera,
  cambio de entrada y ayuda repetida en distintas fases.
- Comprobar ausencia de vertidos/clics involuntarios, coherencia entre voz y demo,
  persistencia de ajustes, lectura de estados y rendimiento.
- Completar una sesión de cultivo sin voz y revisar las señales sin depender del
  color. Incluir pruebas con personas zurdas y con daltonismo cuando sea posible.

## Coordinación con la sustitución de voces

La carpeta indicada por el usuario,
`C:\Users\Trole\Documents\generate_voice\audios`, contiene 44 WAV. La revisión
del 26 de octubre encontró 27 nombres correspondientes a clips del catálogo actual,
todos con SHA-256 diferente, y 17 adicionales sin integrar. No se han sustituido.
La carpeta es una fuente de lectura; cualquier copia o respaldo debe permanecer
dentro del proyecto y conservar los `.meta` de los recursos existentes.

El guion de la conversación compartida incluye una práctica de riego, reserva
limitada, transferencia de agua al rellenar y reintento. Esas mecánicas no están
implementadas. Incluso un nombre ya existente, como `help_refill`, puede contener
ahora una promesa sobre la reserva. Revisar el texto de cada grabación antes de
sustituirla; activar las líneas sobre reserva solo junto a su comportamiento real.
Los identificadores accesibles y las variantes de lateralidad mantienen los
requisitos de `GuionVozMenuYAccesibilidad.md`.

Orden de trabajo: prueba física breve del menú; inventario y selección de voces
compatibles; mejora de las demostraciones de manos y ajuste de tiempos a esas
voces; prueba de comprensión en Quest 3. La reserva de agua requiere una entrega
propia coordinada con sus diálogos. No dar por integrada ninguna voz solo porque
el archivo exista en disco o coincida su nombre.

Primera entrega propuesta: base de ajustes y menú funcional. Después, biblioteca
de ayuda y sonido/brillo; cerrar lateralidad y daltonismo antes de considerar
terminada la función. No actualizar paquetes ni Unity para realizar este trabajo.
