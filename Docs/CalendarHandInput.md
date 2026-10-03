# Calendar hand input

In `Garden_Moves`, the Meta comprehensive interaction rig supplies hand rays and
pinch selection. The calendar's `RayInteractable`, `ColliderSurface` and
`PointableCanvas` forward these selections to the existing Unity buttons.

- Pinch with the left index finger to open the calendar.
- With the calendar visible, aim either hand's ray at **Advance phase** or
  **Close**, pinch, then release to click the button.
- A left-hand pinch pointing away from the calendar toggles its visibility.
- The left Touch controller's **Y** button also toggles visibility.

`CalendarVisibilityController.leftHand` is assigned in `Garden_Moves`. A held
pinch triggers only once. Another toggle requires the pinch strength to drop to
0.3 or less for `pinchReleaseDuration` (default 0.1 seconds). Missing hands,
low-confidence tracking and system gestures disarm the toggle until a reliable
release is observed.

Calendar gestures are sampled after the Meta grab interactors. A left hand near
a grabbable prop, or either hand holding an object, consumes the calendar pinch.
Afterwards, release the pinch fully before making a fresh calendar gesture.
The Y button remains available while holding a tool.

## Verification

Use **Garden > Verify calendar hand input** in Unity to check the prefab's
ray/button references and the pinch latch. On the Quest, also check:

1. Hold a left pinch for several seconds: the calendar opens once and stays still.
2. Release, point at **Advance phase**, pinch and release: advance exactly one
   milestone and keep the calendar open. Repeat with the other hand.
3. Point at **Close**, pinch and release: close once and keep it closed.
4. Briefly hide a pinched hand from the headset, then bring it back: no toggle
   until you release and pinch again.
5. Press **Y** with controllers while hands are untracked: toggle normally.
6. Advance to the final phase after meeting every water goal: **Cosecha** stays disabled; **Reiniciar** and **Recargar agua** remain available.
7. Pick up, tilt and release the watering can with each hand: the calendar must
   remain unchanged. Release the left pinch, then pinch away from props to toggle
   the calendar normally.

## Tomato prototype controls

Garden_Moves adds **Avanzar fase**, **Recargar agua**, **Traer regadera** and **Reiniciar** to the existing bottom row at runtime. All four use the existing Meta ray surface and PointableCanvas, within the same collider footprint. An unmet water goal leaves Advance enabled so a click can explain the missing water, but CalendarSystem blocks time and growth. Stop pouring for at least 0.5 seconds before advancing. Excess requires a 5-second pause for accelerated prototype drainage.

Restart reloads Garden_Moves after every held prop has been released. Verify twice on the headset that pinch toggling, both hand rays and controller grips still work after reload. Refill is available through a calendar ray click for hand users; it does not change the calendar, journal or plant water totals. Detailed reproduction steps and hardware checks are in GardenGrowth.md.

## Recover a dropped watering can

Open the calendar and click **Traer regadera** with the Meta ray and pinch (or controller). Keep the right hand visible and free; the left hand is used if the right is unavailable. The most recently released, unheld can returns upright with its handle about 15 cm in front of that hand; before any release, the nearest unheld can is used. It stays suspended until a normal Meta grab, then release restores gravity. This preserves its remaining water and all plant/calendar counters. A held can is never moved, and missing tracking or busy hands produces guidance instead. The suspended can uses the existing held-object layer so it cannot become the locomotor floor.

On Quest, drop a can on the floor, recover it, grab, pour and release it again. Repeat after teleporting, with either free hand and with controllers. Also confirm that recovery does not refill water or move a held can, and that the four calendar labels remain readable. Recovery has only been statically reviewed; headset validation is pending.
