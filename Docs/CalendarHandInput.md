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
6. Advance to the final phase: **Cycle complete** stays disabled for hand input.
