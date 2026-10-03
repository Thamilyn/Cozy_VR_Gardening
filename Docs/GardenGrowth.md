# Prototipo de tomates en Garden_Moves

Estado de implementación: 2 de octubre de 2026. La validación ejecutada se registra en Docs/Validation/TomatoVerification.txt si el verificador terminó; la prueba física en Quest 3 sigue pendiente.

## Inventario y alcance

Ya existían siembra al soltar, agarres Meta para manos y mandos, cuatro GardenPot, regaderas con depósito finito, calendario manual de seis hitos y cinco prefabs visuales de tomatera. EditorBuildSettings ya tenía Garden_Moves como primera y única escena habilitada cuando comenzó este encargo; se conservó ese cambio previo.

Se añadieron objetivos y contadores de agua por fase, bloqueo central del calendario, guía visual, una bandeja de cosecha, un fruto maduro agarrable y reinicio desde el calendario. Garden_Moves admite una sola semilla de tomate en cualquiera de las tres PotSmall. Plant_Pot contiene una cesta y otros modelos y no admite tomates en esta actividad. Los scripts mantienen los requisitos anteriores de rábano y lechuga, aunque esos cultivos no participan en el prototipo guiado.

## Recorrido manual reproducible

1. Abrir Assets/Scenes/Garden_Moves.unity en Unity 6000.3.23f1 y entrar en Play con el rig Meta. El día comienza en 0; no hay guardado persistente.
2. Leer el cartel del guía cerca del profesor. Agarrar TomatoSeed mediante pinza/palma o Grip y soltar sobre la tierra de una PotSmall. Se admite una sola semilla por actividad. Soltar sobre la mesa, una maceta ocupada o Plant_Pot no planta el tomate y muestra una pista.
3. Abrir el calendario con pinza izquierda lejos de los objetos o Y del mando izquierdo. Seleccionar Avanzar fase sin agua: el día y la planta deben permanecer iguales y el guía debe indicar cuántos ml faltan.
4. Agarrar Watering o bottle-oil e inclinar sobre el centro de la tierra. El chorro y el oscurecimiento del suelo dan feedback. Solo el agua que alcanza el contorno visible de tierra de una maceta plantada suma al objetivo; verter fuera consume el depósito sin sumar. El caudal es 20 ml/s.
5. Consultar el cartel junto a la planta: riego actual, volumen de fase, rango de referencia, total y estado. Al alcanzar la referencia, enderezar la regadera y esperar al menos 0,5 s. Avanzar fase debe cambiar el día a 7 y mostrar el brote.
6. Repetir el cuidado en las fases brote, crecimiento, floración y fruto verde. Cada salto exige su propio riego; el agua de fases anteriores no paga la siguiente. Los hitos son 0, 7, 45, 60, 80 y 110 días aproximados. El tiempo no avanza automáticamente y regar por sí solo no cambia la fase.
7. En fruto verde, el guía pide observar la referencia y decidir el cuidado sin dar otra receta paso a paso. Riego suficiente confirma la acción; exceso y avance sin agua dan feedback de error. Después de 25 s sin actuar aparece una pista de recuperación.
8. En día 110, agarrar el tomate rojo más grande, de 6,5 cm, y soltarlo dentro de la bandeja COSECHA delante de las macetas. Los tomates verdes son visuales y no se pueden cosechar. Solo un fruto rojo se prepara para la entrega del prototipo. Agarrarlo o soltarlo fuera no registra éxito; puede recogerse de nuevo.
9. Al entregar, el fruto queda en la bandeja y el guía anuncia éxito. El diario registra una sola cosecha. Abrir el calendario, soltar todos los objetos y seleccionar Reiniciar. La escena se recarga: día 0, semilla, herramientas, macetas, agua y diario vuelven al estado inicial. Repetir el ciclo con la siguiente persona.
10. Recargar agua rellena ambas regaderas desde el calendario y funciona con manos. B del mando derecho también rellena la regadera sostenida. Recargar no suma agua aplicada. Si una regadera cae al suelo, seleccionar Traer regadera: la última soltada aparece junto a una mano libre y queda suspendida hasta agarrarla. Conserva el agua que tenía.

## Estados de agua

- Insuficiente: volumen retenido menor que el objetivo. Avanzar no cambia el tiempo ni la planta.
- En rango: desde el objetivo hasta el límite de exceso (objetivo × 1,5). Se recomienda parar al llegar al objetivo, no perseguir el límite superior.
- Exceso: se supera el límite. Detener el riego y esperar 5 s sin aportar agua a esa maceta. El drenaje acelerado deja el volumen retenido en el objetivo; los contadores de agua aplicada y el diario conservan todo el exceso. Entonces se puede avanzar. Es una recuperación pedagógica para evitar atascar el recorrido, no un modelo de drenaje real.

El riego actual agrupa aportes a esa maceta separados por menos de 0,5 s. Una nueva aportación tras esa pausa inicia otra medición de riego. El acumulado de fase equivale al del día virtual actual porque el calendario salta entre hitos. Al avanzar se reinician riego actual, agua de fase y agua retenida; el total de la actividad se conserva. El color del suelo usa la proporción del objetivo, sin interpretar litros acumulados como humedad física. La nueva fase visualmente inicia otro ejercicio de cuidado; no representa secado real instantáneo.

## Componentes y ajustes

En [Cozy Garden] Growing System:

- SeedsController: referencia semanal, días representativos, multiplicadores de cinco fases, límite de exceso y espera de drenaje. Los valores y su justificación están en TomatoWaterModel.md.

Objetos guardados en la escena, hijos de [Cozy Garden] Growing System:

- Guía de cultivo y agua: mover con Rect Transform fuera de Play. Los hijos Instrucción del guía y Volumen y referencia permiten editar tamaño, fuente y estilo; Image controla el fondo. TomatoPrototypeGuide mantiene las referencias y actualiza el contenido durante la actividad. Welcome Instruction e Initial Water Information editan los mensajes iniciales. Face Player orienta el cartel al visor; desactivarlo para mantener una rotación manual. Follow Planted Pot está desactivado por defecto para respetar la posición del editor; activarlo para seguir a la maceta con Pot Panel Offset. Idle Hint Seconds controla la demora de pista.
- Bandeja Cosecha: mover, girar o escalar su Transform fuera de Play. Base y los cuatro bordes son mallas/colliders editables con el material existente. Zona de cosecha contiene el BoxCollider trigger del volumen válido de entrega: editar Center y Size si se modifica la geometría. Etiqueta Cosecha contiene un Canvas con el texto editable. TomatoHarvestBasket usa ese volumen y no genera geometría al iniciar Play.

Ambos objetos ya están visibles antes de Play y se conservan al guardar la escena. El cartel y la bandeja no se duplican al jugar ni al reiniciar. Los cambios hechos durante Play no sustituyen los ajustes guardados.

GardenPot calcula el área de su GardenSoil/MeshFilter y escala mundial; substrateAreaOverride permite introducir un área medida al cambiar el modelo. La detección actual considera una superficie rectangular; si se sustituye por sustrato circular se debe adaptar el contorno y asignar su área real. SeedItem reutiliza los prefabs en Assets/Prefabs/TomatoStages; el fruto cosechable usa Tomato_PLACEHOLDER_0 del prefab maduro. Conservar ese nombre o adaptar la asignación al reemplazar el arte.

CalendarSystem centraliza el bloqueo para cualquier llamada a AdvancePhase. Changed solo refresca el estado; PhaseAdvanced es el único evento que consume el cuidado y aumenta la fase del tomate. Los helpers de agarre/regadera se reinstalan al recargar la escena. No se modifican versiones de Unity, paquetes, configuración XR ni GUID existentes.

## Comprobaciones

Con permiso para ejecutar Unity, usar Garden > Verify tomato prototype and save report. Revisa la escena de entrada y referencias en una escena de previsualización aislada; ejecuta las comprobaciones de pinza/rayo existentes y las nuevas de área, riego parcial, exceso, cinco transiciones, contadores, cosecha idempotente y sesión nueva. No guarda ni reemplaza la escena abierta. El informe no acredita interacción real, calidad visual en visor ni rendimiento. Garden > Verify tomato runtime and restart ejecuta además una integración en Play: creación de botones, siembra, cinco transiciones, componentes Meta del fruto, entrega, recarga, espacio del texto y recarga de escena. Entra y sale de Play y deja Docs/Validation/TomatoRuntimeVerification.txt; usa llamadas de prueba, no manos físicas. Ejecutarlo con Garden_Moves abierta y fuera de Play.

Pendiente en Quest 3:

- [ ] Compilar APK e iniciar: aparece Garden_Moves sin pasar por SampleScene.
- [ ] Completar los diez pasos anteriores con cada mano y con mandos.
- [ ] Probar suelo, borde, fuera de maceta, agua insuficiente, exceso y drenaje.
- [ ] Verificar lectura de todo el cartel sin recortes, contraste, orientación y distancia cómoda, incluida persona sentada.
- [ ] Mover el cartel y la bandeja fuera de Play, guardar y comprobar que conservan la posición al jugar y reiniciar. Confirmar que la bandeja está apoyada en la mesa y no bloquea objetos; revisar también su Zona de cosecha si se cambian los bordes.
- [ ] Agarrar/sueltar fruto varias veces fuera de la bandeja antes de entregarlo; no debe haber éxito prematuro.
- [ ] Reiniciar dos veces, incluyendo una maceta movida y herramientas desplazadas: rig, interacciones y depósito deben recuperarse.
- [ ] Verificar que sostener la regadera no abre el calendario ni mueve la cámara.
- [ ] Dejar caer la regadera, recuperarla con Traer regadera y volver a agarrar, regar y soltar. Repetir con ambas manos, tras desplazarse y con mandos; comprobar que el agua no cambia y los cuatro botones se leen bien.
- [ ] Revisar tasa de cuadros y comodidad durante crecimiento y reinicio.
- [ ] Realizar prueba con 2–3 principiantes; registrar duración, atascos y comprensión del exceso.

No hay contenido de voz de cultivo disponible: FR-15 queda pendiente de grabaciones, revisión del guion y ajuste de volumen. Las animaciones de saludo existentes se conservan. La pequeña decisión independiente de fruto verde es feedback formativo; no es un sistema de evaluación formal ni persiste resultados. La lectura del PDF externo no fue necesaria: se usó la especificación Markdown dentro de Assets/Docs. El arte definitivo y la prueba en visor permanecen pendientes.

## Paso visual del tiempo entre fases

Garden_Moves usa el shader Skybox/Cubemap Blend de FREE Skybox Extended Shader (BOXOPHOBIC), con los cubemaps de día y noche importados. Assets/Materials/GardenPhaseSkybox.mat es una copia propia sin rotación automática ni fog del shader. Los archivos del proveedor se conservan. El material se clona durante Play, por lo que la animación no modifica sus valores guardados.

Tras un riego válido, Avanzar fase inicia día → tarde cálida → noche → amanecer. Dura aproximadamente 6,3 segundos y después cambia el calendario y la etapa del tomate una sola vez. La planta permanece en su etapa anterior durante el ciclo. Riego insuficiente, exceso o falta de semilla bloquean el ciclo antes de comenzar. El riego se pausa sin consumir agua de la regadera durante la transición, y pulsaciones repetidas no encolan fases. Reiniciar sigue disponible al soltar los objetos; cancela el ciclo al recargar la escena.

En [Cozy Garden] Growing System → Garden Phase Time Cycle se editan Day Hold Seconds, Transition Seconds (cada uno de los tres fundidos), Afternoon Hold Seconds y Night Hold Seconds. Day, Afternoon y Night permiten ajustar mezcla de cubemaps, tintado, exposición, luz y ambiente. La noche mantiene luz suficiente para orientar al usuario. Es una representación abreviada del salto entre hitos de varios días, no un día real por fase.

La iluminación ambiental usa colores explícitos y no recalcula DynamicGI ni las reflexiones cada cuadro. La validación del shader en URP/Android, la visibilidad nocturna y el rendimiento real en Quest 3 permanecen pendientes. La comprobación opcional Garden > Verify tomato runtime and restart ahora espera los ciclos y verifica que el crecimiento sea posterior, que no se duplique por otra pulsación y que el riego esté pausado. No se ha ejecutado en esta implementación.

Comprobación manual: regar hasta el objetivo, detener el vertido, avanzar y observar los cuatro momentos antes del crecimiento; repetir hasta la cosecha. Intentar avanzar sin agua, pulsar varias veces y reiniciar durante un ciclo con las manos libres. Confirmar que se conservan agua y contadores durante la transición, y que el cielo vuelve al día al reiniciar.
