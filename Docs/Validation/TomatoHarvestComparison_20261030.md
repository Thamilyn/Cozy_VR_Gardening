# Tomato harvest: collection trigger height comparison

Date: 2026-10-30 (Europe/Oslo)
Unity: 6000.3.23f1
Scene: Garden_Moves, using the user's currently open, unsaved scene.

## Preparation

The first invocation of Garden/Verify ripe tomato harvest stopped during setup:
the launch menu suspended the garden, so the synthetic planting was rejected.
It did not reach harvesting. No crop-code failure was established by that run.

For the measured runs, the launch menu was resumed through its existing Continue
and HandleInputRelease methods, with deterministic input-release samples as in
GardenMenuVerification. The tutorial was then disabled only during Play Mode.
The existing TomatoRuntimeVerification focused-harvest Tick sequence was used.
Application.runInBackground was enabled during Play Mode, and advancing frame
counts were confirmed before starting the sequence.

## Results

Only the collection trigger's local Y position differed between the two runs.
The comparison modification was made during Play Mode, without saving the scene.
Both retained local X = -0.6270, local Z = 0.3289 and size (0.31, 0.145, 0.21).

| Measurement | Current lowered trigger | Previous trigger height |
| --- | --- | --- |
| Trigger local Y | 0.0670 m | 0.1120 m |
| Trigger world Y | 0.9670 m | 1.0120 m |
| Final fruit root position | (0.7370, 0.9430, -2.3081) | (0.7370, 0.9430, -2.3081) |
| Lowest model vertex above floor | 0.000500 m | 0.000500 m |
| Fruit diameter throughout grab/release | 0.065000 m | 0.065000 m |
| Harvest checks | PASS | PASS |

The sequence checked detachment, release outside the tray without harvesting,
re-grab, delivery inside the tray, harvest accounting, visibility, parenting,
floor contact and disabled physics/grab components after storage.

Evidence:
- TomatoHarvestLoweredTrigger_20261030.txt
- TomatoHarvestHigherTrigger_20261030.txt
- PrepareHarvestCheck_20261030.cs and RunHarvestCheck_20261030.cs (Pipeline eval snippets, outside Assets)
- RaiseHarvestTriggerForComparison_20261030.cs (Play-Mode-only comparison)

## Interpretation and limits

The floating appearance in the user's screenshot was not reproduced by this
synthetic Meta pointer sequence. Lowering the trigger changed the collection
region, but did not change the final resting height in this comparison. The
measured 0.5 mm clearance matches TomatoFruit.RestOn's intentional offset.

This does not establish the cause of the screenshot or validate arbitrary release
orientations, physical hand/controller tracking, or the Quest 3 build. Console
review reported MetaXR ErrorFormFactorUnavailable during XR initialization, so
headset behavior was not validated. Pipeline also timed out on preparation calls
issued while Unity was importing; later successful calls confirmed the runtime
state before continuing. There were no C# compilation failures reported by the
connected Editor.

No gameplay scripts, packages, scene files or prefab assets were edited. The
saved Garden_Moves scene and both harvest scripts retained their original file
hashes. The Editor was left open outside Play Mode, with the user's unsaved scene
adjustments preserved.
