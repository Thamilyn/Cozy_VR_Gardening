# Degradado rosa y celeste

`GardenPastelSky.shader` mezcla un horizonte rosa claro con celeste en la parte superior. `PastelSky.mat` guarda los colores, la intensidad (0.65) y la altura de la transición (0.65). Se pueden ajustar desde el inspector estándar del material.

El degradado usa la dirección del cielo para mantener un horizonte estable al mover la cabeza. Conserva un 35 % del cubemap diurno para sus detalles. Respeta el tinte y la exposición del ciclo existente y desaparece gradualmente entre los valores 0.25 y 0.95 de `_CubemapTransition`, dejando intacto el cubemap nocturno al llegar a 1.

No añade texturas, geometría ni pasadas de render. Se verificó el shader en el Editor sin mensajes de error y se renderizaron día y noche en `Validation/PinkSkyday.png` y `Validation/PinkSkynight.png`. No se ha medido el rendimiento en Quest 3.

