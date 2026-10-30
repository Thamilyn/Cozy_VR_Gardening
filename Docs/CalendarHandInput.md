# Calendar hand input

In `Garden_Moves`, the Meta comprehensive interaction rig supplies hand rays and
pinch selection. The calendar's `RayInteractable`, `ColliderSurface` and
`PointableCanvas` forward these selections to the existing Unity buttons.

- Pinch with the left index finger to open the calendar.
- With the calendar visible, aim either hand's ray at **Advance phase**,
  **Refill water** or **Restart**, pinch, then release to click the button.
- A left-hand pinch pointing away from the calendar toggles its visibility.
- The left Touch controller's **Y** button also toggles visibility.

The unused **Close** button has been removed from the prefab and its builder.
Close the calendar with the left-hand visibility gesture or the **Y** button.
This removal has been statically reviewed; Unity and Quest 3 validation are pending.

`CalendarVisibilityController.leftHand` is assigned in `Garden_Moves`. A held
pinch triggers only once. Another toggle requires the pinch strength to drop to
0.3 or less for `pinchReleaseDuration` (default 0.1 seconds). Missing hands,
low-confidence tracking and system gestures disarm the toggle until a reliable
release is observed.

Calendar gestures are sampled after the Meta grab interactors. A left hand near
a grabbable prop, or either hand holding an object, consumes the calendar pinch.
Afterwards, release the pinch fully before making a fresh calendar gesture.
The Y button remains available while holding a tool.

The visibility gesture checks the left Meta `RayInteractor`'s processed ray,
matching the ray used to select buttons. While that ray points at the panel,
the visibility pinch is disarmed. Moving away during a UI pinch cannot close
the calendar; release fully away from the panel before making a new toggle
gesture. The raw `OVRHand.PointerPose` is only a fallback when no active left
Meta ray is available. Both hands continue to click through `PointableCanvas`.

Each time the calendar opens, it is placed in the player's horizontal viewing
direction at eye height. It uses world up so headset pitch and roll do not tilt
the panel. When looking straight up or down, it keeps its previous horizontal
direction. The panel stays fixed in the world while unobstructed for stable ray selection.

Calendar placement checks a corridor as wide and tall as the whole panel between
the UI and the viewer, including its clearance margin. A table or fixed prop in
front of an edge blocks that position even when the centre ray passes above or
beside it. Placement tries a bounded lift, nearer positions and side slots; if
none is clear, both the calendar and its ray target stay hidden until space is
available. Fixed instruction boards keep their authored placement.

The calendar also checks the live `Renderer.bounds` of mesh visuals under
`TableSet`, including `SeedBox`, so those objects do not need colliders. The
optional `tableContents` reference can select a different table hierarchy.
The renderer list refreshes when opening and once a second while active to
include newly created children; positions and enabled states are checked on
every placement check. Disabled or inactive visuals are ignored. This visual
check uses conservative bounds and does not add physics colliders to props.

The calendar enables `ignorePlayerAndGrabbables` on its placement component.
Watering cans, seeds, tomatoes and other Meta `Grabbable` hierarchies are excluded
from both physics checks and visual bounds checks, whether held or released.
`GardenPlayer`, `GardenHeldObject`, the camera rig and headset children are also
excluded. Approaching the panel with the headset no longer relocates or hides it.
The ray target remains a trigger, so it cannot push props or the player and still
accepts Meta ray selection. Fixed tables, walls and the seed box continue to block
placement. Other presentations retain their existing placement rules.

## Verification

Use **Garden > Verify calendar hand input** in Unity to check the prefab's
ray/button references and the pinch latch. On the Quest, also check:

1. Hold a left pinch for several seconds: the calendar opens once and stays still.
2. Release, point at **Advance phase**, pinch and release: advance exactly one
   milestone and keep the calendar open. Repeat with the other hand.
3. Release the left pinch, point away from the calendar, then pinch again:
   close once and keep it closed while holding the pinch.
4. Briefly hide a pinched hand from the headset, then bring it back: no toggle
   until you release and pinch again.
5. Press **Y** with controllers while hands are untracked: toggle normally.
6. Advance to the final phase after meeting every water goal: **Cosecha** stays disabled; **Reiniciar** and **Recargar agua** remain available.
7. Pick up, tilt and release the watering can with each hand: the calendar must
   remain unchanged. Release the left pinch, then pinch away from props to toggle
   the calendar normally.
8. Open the calendar with the headset tilted sideways, looking up and looking
   down: the panel should remain upright at eye height. Turn around, close and
   reopen it: it should appear in the new horizontal viewing direction.
9. With the left ray over a button, pinch and move away before releasing:
   the calendar must stay open. Repeat normally with each hand and confirm one
   click per pinch/release. After closing with the visibility gesture, a held
   pinch must not reopen it.
10. Open the calendar facing the table with a fixed prop in front of one of its
    edges. It must find clear space without intersecting the table or fixed
    props. Repeat seated and standing.
    When no slot is free, the hidden panel must not accept ray clicks; clearing
    the space must restore it. **Garden > Verify presentation clearance** adds
    isolated checks for a tabletop and a side prop missed by the centre ray.
11. Face the seed box with its colliders absent: the calendar must avoid both
    intersecting it and placing an edge behind it. Repeat with a disabled mesh:
    that invisible mesh must no longer block placement. The presentation check
    includes colliderless visual placement and hide/recovery cases.
12. Open with **Y** while holding the watering can, a seed or a tomato. Pass each
    prop and both hands through the panel and its sightline, then release the
    props and reopen. The calendar must not move or hide because of them. Move
    the headset close to the panel: it must stay in place without pushing the
    player. Confirm both hand rays still click its buttons. The presentation
    verifier includes ignored prop colliders/visuals, hands, headset proximity,
    ray selection and retained fixed-obstacle blocking.

The movable-object exclusion and its editor checks have only been statically
reviewed; Unity execution and physical Quest 3 validation are pending.

The full-surface clearance change and its added editor checks have only been
statically reviewed; Unity execution and physical Quest 3 validation are pending.

The left-ray visibility fix and added regression checks have been statically
reviewed; Unity execution and physical Quest 3 validation are pending.

## Tomato prototype controls

Garden_Moves shows **Avanzar fase**, **Recargar agua** and **Reiniciar** in a centered bottom row at runtime. All three use the existing Meta ray surface and PointableCanvas, within the same collider footprint. An unmet water goal leaves Advance enabled so a click can explain the missing water, but CalendarSystem blocks time and growth. Stop pouring for at least 0.5 seconds before advancing. Excess requires a 5-second pause for accelerated prototype drainage.

Restart reloads Garden_Moves after every held prop has been released. Verify twice on the headset that pinch toggling, both hand rays and controller grips still work after reload. Refill is available through a calendar ray click for hand users; it does not change the calendar, journal or plant water totals. Detailed reproduction steps and hardware checks are in GardenGrowth.md.

## Recover a dropped watering can

The calendar no longer has a manual recall button. A can left elsewhere on the table stays there. Once it has been grabbed, recovery starts only while it is unheld and its centre of mass is at least 45 cm below its centre of mass at the return pose. After remaining below this height for two seconds of active gameplay, it returns to its starting pose or configured Recovery Point and waits for a normal Meta grab. Grabbing it or raising it above the limit cancels the timer; pausing the garden stops the countdown. This height check also detects a can that slides off the table later. Recovery preserves its remaining water and all plant/calendar counters. The waiting can uses the existing held-object layer so it cannot become the locomotor floor.

On Quest, leave the can at different positions on the table, upright and on its side: it must stay there after two seconds. Then drop it below the recovery height, wait for its return, grab and pour again. Repeat with either hand and with controllers, including grabbing it before the timer expires, a delayed slide off the table, and pausing during the countdown. Confirm that recovery does not refill water or move a held can, and that the three calendar labels remain readable. The height condition and removal of manual recall have only been statically reviewed; Unity and headset validation are pending.
