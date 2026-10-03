# Modelo educativo de agua del tomate

Fecha: 2 de octubre de 2026. Fuentes web consultadas con autorización del usuario.

## Referencias publicadas

La [guía de Utah State University sobre agua de vegetales](https://extension.usu.edu/yardandgarden/research/water-recommendations-for-vegetables) da 1–2 pulgadas por semana para tomate en jardín y señala que cambian las necesidades con la fase, tamaño, suelo y temperatura. Se adopta 25 mm/semana, aproximación a una pulgada, como referencia inicial de cálculo.

La [guía de riego de tomate, pimiento y berenjena de USU](https://extension.usu.edu/vegetableguide/tomato-pepper-eggplant/irrigation) recomienda suministro regular, aumentar al crecer y con calor, y una ligera reducción cuando el fruto alcanza tamaño maduro, manteniendo humedad consistente. La irregularidad puede causar problemas en flores y frutos.

La [guía educativa de Oregon State University](https://extension.oregonstate.edu/catalog/em-9032-educators-guide-vegetable-gardening), sección Watering, destaca establecimiento y desarrollo de flores/frutos; distingue riego de plántulas a menor profundidad y recomienda dirigir el agua a las raíces y limitar el follaje mojado.

La [guía de tomate en jardines de University of Minnesota](https://extension.umn.edu/garden-and-home/yard-and-garden/gardening-in-minnesota/growing-tomatoes), sección Watering, menciona aproximadamente una pulgada por semana, humedad uniforme y riego dirigido al suelo.

Estas fuentes no establecen una dosis exacta por fase para las macetas del proyecto. Los multiplicadores, tolerancia y tiempo de drenaje de abajo son decisiones del prototipo. En germinación se enseña mantener humedad sin encharcar; no se prescribe secar la planta para madurar.

## Área y calendario

Ground del prefab PotSmall usa el plano integrado de Unity, de 10 × 10 unidades. Su escala X/Z es 0,02 y cada PotSmall de Garden_Moves tiene escala 0,6. La superficie rectangular visible mide 10 × 0,02 × 0,6 = 0,12 m por lado. Área = 0,0144 m². GardenPot calcula el área con mesh.bounds y vectores transformados; no usa el radio del trigger de siembra como área de suelo.

Volumen en litros = profundidad en mm × área en m².

Los hitos biológicos aproximados son días 0, 7, 45, 60, 80 y 110. Cada botón resume múltiples días de crecimiento, pero exige una sola jornada representativa de cuidado, representativeCareDays = 1. No pretende entregar en segundos toda el agua que una planta consumiría durante 38 días. La equivalencia didáctica se mantiene igual para cada salto y queda separada de la edad mostrada.

Objetivo en ml = (25 mm/semana ÷ 7) × días de cuidado representados × multiplicador de fase × área × 1000.

## Umbrales elegidos

| Fase cuidada → fase siguiente | Multiplicador | Objetivo ml | Exceso si supera ml | Segundos hasta objetivo a 20 ml/s |
| --- | ---: | ---: | ---: | ---: |
| Semilla → brote | 1,0 | 51,43 | 77,14 | 2,57 |
| Brote → joven | 1,2 | 61,71 | 92,57 | 3,09 |
| Joven → floración | 1,5 | 77,14 | 115,71 | 3,86 |
| Floración → fruto verde | 1,7 | 87,43 | 131,14 | 4,37 |
| Fruto verde → maduro | 1,5 | 77,14 | 115,71 | 3,86 |

Los factores iniciales moderan el volumen para establecimiento, los siguientes elevan la demanda con crecimiento y flores, y el último baja ligeramente desde floración para ilustrar la reducción al aproximarse la maduración. Son una inferencia didáctica de tendencias, no cifras extraídas de las guías. El mismo factor inicial no implica que germinación tolere falta o exceso: toda transición exige cumplir el mínimo y evitar encharcar.

La tolerancia de 50 % concede margen para una interacción VR de principiante. El guía recomienda parar en el objetivo. Superar la tolerancia bloquea el avance hasta 5 s sin nuevo aporte; se elimina solo el excedente retenido, conservando el volumen aplicado. Ninguna fuente fija esos 5 s: son drenaje acelerado para recuperación del prototipo.

El modelo contabiliza llegada al sustrato, no humedad real, evaporación ni absorción radicular. No calcula clima, enfermedades ni duración real del drenaje. Los valores nunca deben extrapolarse como recomendación universal de ml por planta. Para uso real se debe observar humedad y considerar recipiente, drenaje, temperatura, tamaño y sustrato.

## Configuración y lectura

SeedsController expone referenceMillimetresPerWeek, representativeCareDays, tomatoWaterPhases[].demandMultiplier, excessMultiplier y drainageWaitSeconds. GardenPot permite asignar el MeshFilter de tierra y un área medida opcional. Si no existe área u objetivo válido, el calendario bloquea y pide corregir el Inspector.

Los valores se mantienen en float; el cartel redondea a 0,1 ml. El jugador puede necesitar una fracción adicional respecto al número mostrado por redondeo. El estado En rango indica si el valor interno realmente ha alcanzado el objetivo. El contador de riego se reinicia tras una pausa de 0,5 s y al cambiar de fase; fase/día se reinicia al avanzar; total y diario solo se limpian al reiniciar la escena.
