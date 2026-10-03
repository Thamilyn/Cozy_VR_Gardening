# Guion de voz de Gronny

Cozy VR Gardening

Este documento reúne 19 diálogos en inglés para generar la voz de Gronny con KittenTTS: 13 intervenciones principales y 6 ayudas. La bienvenida es general y la primera lección enseña a cultivar tomate. Las instrucciones disminuyen durante el crecimiento para dar al jugador más autonomía.

## Indicaciones para generar los audios

- Generar un audio independiente por diálogo y conservar el identificador como nombre de archivo.
- Copiar únicamente el texto hablado, sin títulos ni indicaciones de reproducción.
- Usar la misma voz y configuración en todos los audios.
- Interpretación cálida, suave y ligeramente juguetona, con pronunciación clara y pequeñas pausas.
- Conservar los audios originales. Los nombres sugeridos permiten reconocerlos al incorporarlos a Unity.

## Bienvenida y enseñanza inicial

### 01_welcome

**Cuándo reproducirlo:** Primera bienvenida al acercarse a la mesa. Presentación general para las distintas plantas que se aprenderán.

Oh, hello there! Welcome to our little garden. My name is Gronny, and I'll help you learn how to grow and care for your plants.

### 02_tomato_intro

**Cuándo reproducirlo:** Inicio de la lección de tomate, después de la bienvenida general.

Let's start with a tomato plant! I'll guide you through the first steps.

### 03_plant_seed

**Cuándo reproducirlo:** Antes de plantar la semilla de tomate.

Pick up the red tomato seed and gently drop it onto the soil in an empty small pot.

### 04_first_watering

**Cuándo reproducirlo:** Después de plantar. Explicación inicial del uso de la regadera.

There we go! Now pick up the watering can and tilt it gently over the soil. Aim near the roots, and keep the water off the leaves.

### 05_water_target

**Cuándo reproducirlo:** Enseñanza inicial del objetivo y el límite de riego, después de la explicación de la regadera.

Watch the water amount on the panel. Try to reach the target without going over the limit.

### 06_enough_water

**Cuándo reproducirlo:** Primer riego dentro del rango. Usar durante la enseñanza inicial.

That's enough water! Hold the can upright to stop pouring.

### 07_first_advance

**Cuándo reproducirlo:** Después de detener el primer riego, cuando ya se puede avanzar.

Now open your calendar and select Advance phase. Let's give our tomato some time to grow.

## Crecimiento y cosecha

### 08_sprout

**Cuándo reproducirlo:** Al aparecer el brote. Recordatorio breve del nuevo objetivo de agua.

Look, a little sprout! Check its new water target, then give it another careful watering.

### 09_young_plant

**Cuándo reproducirlo:** Al aparecer la planta joven. El jugador interpreta el objetivo de riego.

Our tomato is getting bigger! Can you check how much water it needs at this stage?

### 10_flowering

**Cuándo reproducirlo:** Al aparecer las flores. Gronny deja que el jugador decida la siguiente acción.

The flowers are here! You've had some practice now. Take a look at the panel and decide what to do next.

### 11_green_fruit

**Cuándo reproducirlo:** Al aparecer los frutos verdes. Último riego con autonomía.

Look, green tomatoes! This time, you're in charge. Check what the plant needs and take care of it.

### 12_harvest

**Cuándo reproducirlo:** Al madurar el fruto. Primera explicación de cómo cosechar.

Our tomato is ripe! Pick the largest red tomato and gently place it in the harvest tray.

### 13_complete

**Cuándo reproducirlo:** Al depositar el tomate maduro en la bandeja de cosecha.

Well done! You've grown and harvested a tomato. Thank you for taking care of our little garden!

## Ayudas y avisos

### help_planting

**Cuándo reproducirlo:** Si la semilla se suelta fuera de la tierra de una maceta válida.

Try dropping the seed onto the soil in the centre of an empty small pot.

### help_low_water

**Cuándo reproducirlo:** Si se intenta avanzar sin alcanzar el objetivo de agua.

The plant needs a little more water. Check the target on the panel.

### help_excess_water

**Cuándo reproducirlo:** Si se supera el límite de agua de la fase.

There's too much water in the pot. Hold the can upright and let the extra water drain away.

### help_drained

**Cuándo reproducirlo:** Cuando termina el drenaje del exceso de agua.

The extra water has drained away. Next time, try stopping when you reach the target.

### help_calendar

**Cuándo reproducirlo:** Si el jugador necesita recordar cómo avanzar con el calendario.

Open your calendar and select Advance phase when you've finished caring for the plant.

### help_refill

**Cuándo reproducirlo:** Si la regadera está vacía.

Your watering can is empty. Open the calendar and select Refill water.

## Uso de la ayuda progresiva

Los diálogos 03 a 07 enseñan el procedimiento inicial. Durante el crecimiento, las intervenciones 08 a 11 pasan de recordar la acción a pedir que el jugador decida el cuidado. La cosecha se explica cuando aparece por primera vez.

En el último riego, no repetir automáticamente las instrucciones que indican cuándo parar o avanzar. El panel puede conservar los datos de volumen y objetivo. Registrar las ayudas recibidas permite distinguir un intento autónomo de uno asistido. Completar la cosecha, por sí solo, no demuestra que el jugador actuó sin ayuda.

El aviso de exceso de agua puede mantenerse como feedback. Si incluye una instrucción hablada para resolver la situación, registrarla como ayuda durante la evaluación. La reproducción de los audios y el registro de autonomía deben conectarse al juego; este documento contiene el guion.

## Referencia de generación

KittenTTS: https://github.com/KittenML/KittenTTS#demo
