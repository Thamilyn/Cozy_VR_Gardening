public static class PrepareHarvestAnimationPlay { public static object Main() {
if (!UnityEditor.EditorApplication.isPlaying) throw new System.Exception("Play required");
UnityEngine.Application.runInBackground = true;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var menu = UnityEngine.Object.FindFirstObjectByType<GardenMenuController>();
typeof(GardenMenuController).GetField("started", flags).SetValue(menu, true);
typeof(GardenMenuController).GetField("visible", flags).SetValue(menu, false);
typeof(GardenMenuController).GetField("awaitingRelease", flags).SetValue(menu, false);
((GardenInteractionSuspension)typeof(GardenMenuController).GetField("suspension", flags).GetValue(menu)).Release();
foreach (var tutorial in UnityEngine.Object.FindObjectsByType<GronnyTutorial>(UnityEngine.FindObjectsSortMode.None))
    tutorial.enabled = false;
var seeds = UnityEngine.Object.FindFirstObjectByType<SeedsController>();
if (seeds.PrototypeTomato == null)
{
    GardenPot pot = null;
    foreach (var candidate in UnityEngine.Object.FindObjectsByType<GardenPot>(UnityEngine.FindObjectsSortMode.None))
        if (candidate.AcceptsTomato && candidate.GetComponent<TomatoPotStages>() != null) { pot = candidate; break; }
    SeedItem seed = null;
    foreach (var candidate in UnityEngine.Object.FindObjectsByType<SeedItem>(UnityEngine.FindObjectsSortMode.None))
        if (!candidate.IsPlanted && candidate.Crop == SeedCrop.Tomato) { seed = candidate; break; }
    seed.transform.position = pot.PlantingPoint;
    if (!seeds.TryPlant(seed) || !seeds.TryCoverSeed(pot)) throw new System.Exception("Plant/soil fixture failed");
}
var plant = seeds.PrototypeTomato;
var state = seeds.GetState(plant);
state.tomatoPhase = 5;
state.stage = GrowthStage.Mature.ToString();
plant.stage = GrowthStage.Mature;
plant.seed.ShowStage(GrowthStage.Mature);
UnityEditor.EditorApplication.isPaused = true;
return new { ripe = plant.seed.HarvestFruit.IsRipe, cuttable = plant.seed.HarvestFruit.CanCutStem };

} }
