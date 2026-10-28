using UnityEngine;
using UnityEngine.UI;

/// <summary>Fixed, non-interactive reserve display beside Gronny's gardening board.</summary>
[DisallowMultipleComponent]
public sealed class GardenWaterTankDisplay : MonoBehaviour
{
    private GardenWaterSupply supply;
    private GardenWaterIndicator indicator;
    private Text amount;
    private float nextRefresh;

    public static void Create(GardenWaterSupply waterSupply, TomatoPrototypeGuide guide)
    {
        RectTransform board = guide != null ? guide.GetComponent<RectTransform>() : null;
        if (waterSupply == null || board == null) return;

        GardenWaterIndicator indicator = GardenWaterIndicator.Create(null, "Garden water tank display", 2, statusFontSize: 36);
        RectTransform panel = indicator.RectTransform;
        panel.sizeDelta = new Vector2(360f, 280f);
        panel.localScale = Vector3.one * 0.001f;
        Vector3 boardEdge = board.TransformPoint(new Vector3(board.rect.xMax, board.rect.center.y, 0f));
        panel.SetPositionAndRotation(boardEdge + board.right * (0.1f + 0.18f), board.rotation);
        // Share the board's fixed scene parent, rather than the menu that moves after Play.
        panel.SetParent(board.parent, true);
        GardenWaterTankDisplay display = indicator.gameObject.AddComponent<GardenWaterTankDisplay>();
        display.supply = waterSupply;
        display.indicator = indicator;
        display.CreateAmountLabel();
        display.Refresh();
    }

    private void CreateAmountLabel()
    {
        GameObject root = new("Tank water amount", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        root.layer = gameObject.layer;
        amount = root.GetComponent<Text>();
        amount.rectTransform.SetParent(transform, false);
        amount.rectTransform.anchorMin = amount.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        amount.rectTransform.anchoredPosition = new Vector2(0f, -112f);
        amount.rectTransform.sizeDelta = new Vector2(360f, 40f);
        amount.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        amount.fontSize = 28;
        amount.alignment = TextAnchor.MiddleCenter;
        amount.color = Color.white;
        amount.raycastTarget = false;
        Shadow shadow = root.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.02f, 0.13f, 0.17f, 0.9f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        if (supply == null) { gameObject.SetActive(false); return; }
        Refresh();
    }

    private void Refresh()
    {
        bool practice = supply.IsPractice;
        float reserve = supply.ReserveMillilitres;
        amount.text = practice ? "Unlimited" : reserve >= 1000f
            ? (reserve / 1000f).ToString("0.##") + " L"
            : reserve.ToString("0.#") + " ml";
        bool empty = !practice && reserve <= 0.001f;
        bool low = !practice && reserve <= supply.InitialReserveMillilitres * 0.25f;
        Color tint = supply.Exhausted || empty ? GardenWaterIndicator.AlertColor
            : low ? GardenWaterIndicator.LowColor : GardenWaterIndicator.WaterColor;
        string message = practice ? "Tank: free refills"
            : supply.Completed ? "Harvest complete"
            : supply.Exhausted ? "Try again"
            : empty ? "Tank: empty"
            : low ? "Tank: low reserve"
            : "Tank: water available";
        indicator.SetLevel(practice ? 1f : supply.InitialReserveMillilitres > 0f
            ? reserve / supply.InitialReserveMillilitres : 0f, tint, message,
            supply.Exhausted || empty || low ? tint : Color.white);
    }
}
