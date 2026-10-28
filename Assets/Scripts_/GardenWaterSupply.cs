using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One measured water budget for the tomato, shared by every can and refill route.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(200)]
public sealed class GardenWaterSupply : MonoBehaviour
{
    [SerializeField, Min(1f)] private float canCapacityMillilitres = 240f;
    [SerializeField, Min(1f)] private float budgetMultiplier = 2f;
    private const float Epsilon = 0.001f;
    private SeedsController seeds;
    private CalendarSystem calendar;
    private readonly List<WateringCan> cans = new();
    private float missedSince = float.NaN;
    private float lastPourAt = float.NegativeInfinity;
    private float nextEvaluation;
    public static GardenWaterSupply Active { get; private set; }
    public bool IsPractice { get; private set; } = true;
    public bool Exhausted { get; private set; }
    public bool Completed => seeds != null && seeds.HarvestComplete;
    public bool ReserveEmpty => !IsPractice && !Completed && ReserveMillilitres <= Epsilon;
    public int Attempt { get; private set; }
    public float ReserveMillilitres { get; private set; }
    public float InitialReserveMillilitres { get; private set; }
    public float AttemptBudgetMillilitres { get; private set; }
    public float PracticePouredMillilitres { get; private set; }
    public float PracticeUsefulMillilitres { get; private set; }
    public float PracticeWastedMillilitres => Mathf.Max(0f, PracticePouredMillilitres - PracticeUsefulMillilitres);
    public float PouredMillilitres { get; private set; }
    public float UsefulMillilitres { get; private set; }
    public float MissedMillilitres { get; private set; }
    public float ExcessMillilitres { get; private set; }
    public float WastedMillilitres => Mathf.Max(0f, PouredMillilitres - UsefulMillilitres);
    public float TotalUsefulMillilitres => PracticeUsefulMillilitres + UsefulMillilitres;
    public float TotalWastedMillilitres => PracticeWastedMillilitres + WastedMillilitres;
    public bool IsMissingSoil => !float.IsNaN(missedSince) && Time.unscaledTime - missedSince >= 0.75f &&
        Time.unscaledTime - lastPourAt < 0.25f;
    public bool HasNoMeasuredWaste => TotalWastedMillilitres <= Epsilon;
    public bool HasEmptyHeldCan => cans.Exists(can => can != null && can.isActiveAndEnabled &&
        can.IsHeld && can.CurrentWaterLitres * 1000f <= Epsilon);
    public float AvailableMillilitres => ReserveMillilitres + WaterInCansMillilitres;
    public string FailureMessage => AvailableMillilitres <= Epsilon
        ? "The reserve and watering cans are empty.\nYour tomato still needs water."
        : "Not enough water remains to finish your tomato.\nTry again with a fresh supply.";

    public float WaterInCansMillilitres
    {
        get
        {
            float total = 0f;
            foreach (WateringCan can in cans)
                if (can != null && can.isActiveAndEnabled) total += can.CurrentWaterLitres * 1000f;
            return total;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active = null;

    private void Awake() => Active = this;

    private void Start()
    {
        seeds = FindFirstObjectByType<SeedsController>();
        calendar = seeds != null ? seeds.Calendar : null;
        if (calendar == null) { enabled = false; return; }
        calendar.PhaseAdvanced += OnPhaseAdvanced;
        foreach (WateringCan can in FindObjectsByType<WateringCan>(FindObjectsSortMode.None)) RegisterCan(can);
        GardenWaterTankDisplay.Create(this, FindFirstObjectByType<TomatoPrototypeGuide>());
    }

    public void RegisterCan(WateringCan can)
    {
        if (can == null || cans.Contains(can)) return;
        cans.Add(can);
        can.SetSessionWater(canCapacityMillilitres / 1000f, IsPractice ? canCapacityMillilitres / 1000f : 0f);
    }

    private void OnPhaseAdvanced()
    {
        if (IsPractice && seeds.GetState(seeds.PrototypeTomato)?.tomatoPhase >= 1) BeginAttempt();
    }

    private void Update()
    {
        if (GardenMenuController.GardenSuspended || IsPractice || calendar == null || calendar.IsAdvancing) return;
        if (Time.unscaledTime < nextEvaluation) return;
        nextEvaluation = Time.unscaledTime + 0.1f;
        EvaluateExhaustion();
    }

    private void BeginAttempt()
    {
        float remaining = seeds.RemainingTomatoWaterMillilitres();
        if (remaining <= Epsilon) return;
        IsPractice = false;
        Attempt++;
        Exhausted = false;
        missedSince = float.NaN;
        AttemptBudgetMillilitres = remaining * Mathf.Max(1f, budgetMultiplier);
        ReserveMillilitres = AttemptBudgetMillilitres;
        // Practice leftovers never become an unbudgeted second reservoir.
        foreach (WateringCan can in cans)
            if (can != null) can.SetSessionWater(canCapacityMillilitres / 1000f, 0f);
        // Fill only one can initially. Every further can uses the same shared budget.
        if (cans.Count > 0 && cans[0] != null)
            cans[0].SetSessionWater(canCapacityMillilitres / 1000f, TakeForRefill(canCapacityMillilitres / 1000f));
        // The tank starts full relative to its own reserve, after the initial can allocation.
        InitialReserveMillilitres = ReserveMillilitres;
        calendar.NotifyChanged();
    }

    public bool Retry()
    {
        if (!Exhausted) return false;
        return GardenMenuController.RestartAtPlanting(gameObject.scene);
    }

    public float TakeForRefill(float requestedLitres)
    {
        if (!float.IsFinite(requestedLitres) || requestedLitres <= 0f) return 0f;
        if (IsPractice) return requestedLitres;
        if (Exhausted || Completed) return 0f;
        float amount = Mathf.Min(ReserveMillilitres, requestedLitres * 1000f);
        ReserveMillilitres = Mathf.Max(0f, ReserveMillilitres - amount);
        return amount / 1000f;
    }

    public void RecordPour(float pouredLitres, float usefulLitres, bool reachedTargetSoil)
    {
        if (!float.IsFinite(pouredLitres) || pouredLitres <= 0f || GardenMenuController.GardenSuspended || Exhausted || Completed) return;
        float poured = pouredLitres * 1000f;
        float useful = float.IsFinite(usefulLitres) ? Mathf.Clamp(usefulLitres * 1000f, 0f, poured) : 0f;
        if (IsPractice)
        {
            PracticePouredMillilitres += poured;
            PracticeUsefulMillilitres += useful;
        }
        else
        {
            PouredMillilitres += poured;
            UsefulMillilitres += useful;
            if (reachedTargetSoil) ExcessMillilitres += poured - useful;
            else MissedMillilitres += poured - useful;
        }
        if (reachedTargetSoil) missedSince = float.NaN;
        else if (float.IsNaN(missedSince) || Time.unscaledTime - lastPourAt > 0.25f) missedSince = Time.unscaledTime;
        lastPourAt = Time.unscaledTime;
    }

    public void EvaluateExhaustion()
    {
        if (IsPractice || Exhausted || seeds == null || calendar == null || calendar.IsAdvancing || seeds.HarvestComplete) return;
        float remaining = seeds.RemainingTomatoWaterMillilitres();
        // Offer recovery as soon as the crop becomes impossible to finish. Waiting
        // for literal zero leaves the player watering a crop they can no longer complete.
        Exhausted = remaining > Epsilon && AvailableMillilitres + Epsilon < remaining;
        if (!Exhausted) return;
        foreach (WateringCan can in cans) if (can != null) can.SuspendPouring();
        calendar.NotifyChanged();
    }

    public string StatusText => IsPractice ? "Practice: water refills freely."
        : Exhausted ? "GAME OVER: not enough water.\nSelect Try again for fresh water."
        : ReserveMillilitres > Epsilon && (HasEmptyHeldCan || WaterInCansMillilitres <= Epsilon)
            ? "CAN EMPTY | Reserve: " + ReserveMillilitres.ToString("0.0") + " ml\nCalendar: Refill water / Right controller: B."
        : ReserveEmpty && HasEmptyHeldCan && WaterInCansMillilitres > Epsilon
            ? "CAN EMPTY | Water remains in another can.\nUse the other can to continue."
        : ReserveEmpty ? "RESERVE EMPTY | Cans: " + WaterInCansMillilitres.ToString("0.0") + " ml\nUse the remaining water. No more refills."
        : "Reserve: " + ReserveMillilitres.ToString("0.0") + " ml | Cans: " + WaterInCansMillilitres.ToString("0.0") + " ml";

    public string SummaryText => "Water used: " + TotalUsefulMillilitres.ToString("0.0") + " ml\n"
        + "Water wasted: " + TotalWastedMillilitres.ToString("0.0") + " ml | Attempts: " + Attempt;

    private void OnDestroy()
    {
        if (calendar != null) calendar.PhaseAdvanced -= OnPhaseAdvanced;
        if (Active == this) Active = null;
    }
}
