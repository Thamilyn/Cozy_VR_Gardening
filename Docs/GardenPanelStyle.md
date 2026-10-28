# Bordes redondeados de los paneles

Los fondos de la interfaz del jardín comparten el redondeado de
`GardenRoundedPanel`: menú inicial, botón Menu, pausa, controles, sonido,
Game over, guía de Gronny, calendario y etiquetas de semilla/fruto. También
se redondean los botones y las filas del calendario.

El efecto se añade al crear la interfaz o al inicializar la guía y el calendario,
incluidos los elementos inicialmente ocultos. Conserva los componentes Image,
sus referencias desde Button y CalendarPanel, colores, opacidad, dimensiones,
posiciones y selección Meta. Las imágenes con sprite mantienen su geometría;
si se asigna un sprite después, el efecto deja de modificar esa imagen.

El radio predeterminado es de 24 unidades de UI, limitado a la mitad del lado
menor. Solo se modifica la geometría del fondo, sin recortar textos ni hijos.
Los objetivos rectangulares de selección existentes conservan su tamaño.
Las barras de plantas, tanque y regaderas reutilizan la misma función geométrica
con sus radios anteriores de 14 y 7 unidades para conservar su diseño.

No se añaden texturas, materiales, máscaras ni paquetes. No se editan escenas
ni prefabs: el acabado se aplica en ejecución.

## Tamaños de texto junto a Gronny

En `Garden_Moves`, la instrucción principal de la guía usa tamaño 40 (antes 27)
y la ayuda secundaria tamaño 36 (antes 26). Se ajustan los dos Text existentes
de la escena, conservando sus referencias, rectángulos y salto de línea.

El tanque usa tamaño 36 para el estado (antes 28) y 28 para la cantidad
(antes 20). La cantidad dispone de un rectángulo de 40 unidades de alto y se
coloca más abajo para separarla del estado; el área del indicador se amplía a
360 × 280 unidades. Su posición y el ancho de la barra se conservan.
Las plantas y las regaderas mantienen sus tamaños de letra anteriores.

Esta revisión de tipografía se comprobó estáticamente. Queda pendiente validar
en Unity y Quest 3 el ajuste de los mensajes largos, la separación respecto a
la guía y la lectura desde la posición de juego; no se ejecutaron pruebas.

## Verificación

Se revisaron estáticamente los puntos de creación e inicialización de los fondos,
las imágenes de la guía y del prefab de calendario, la API de BaseMeshEffect de
la versión instalada y la conservación de las referencias de interacción.
No se ejecutaron Unity, recompilación ni pruebas automatizadas.

Queda pendiente comprobar en Unity y Quest 3 todos los paneles, los colores de
botones al señalar/pulsar, la reapertura del calendario, los iconos, la lectura
cerca de las esquinas y el suavizado de los bordes en ambos ojos.
