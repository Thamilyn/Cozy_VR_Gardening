using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

/// <summary>Ripe fruit follows the plant until grabbed and counts only after release in the tray.</summary>
[DisallowMultipleComponent]
public sealed class TomatoFruit : MonoBehaviour
{
    private SeedItem source;
    private Grabbable grabbable;
    private Rigidbody body;
    private TomatoHarvestBasket basket;
    private bool picked;
    private bool stored;
    private float nextBasketSearch;

    public void Initialize(SeedItem seed)
    {
        source = seed;
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null) collider = gameObject.AddComponent<SphereCollider>();
        collider.radius = 0.5f;
        GardenGrabSetup.ConfigureGrabbable(gameObject);
        grabbable = GetComponent<Grabbable>();
        body = GetComponent<Rigidbody>();
        body.mass = 0.08f;
        body.useGravity = false;
        body.isKinematic = true;
        grabbable.ForceKinematicDisabled = true;
    }

    private void Update()
    {
        if (stored || grabbable == null) return;
        bool held = grabbable.SelectingPointsCount > 0;
        if (held && !picked)
        {
            picked = true;
            // Preserve world size while detaching from the movable pot.
            transform.SetParent(null, true);
            body.useGravity = true;
        }
        if (!picked || held) return;
        if (basket == null && Time.unscaledTime >= nextBasketSearch)
        {
            basket = FindFirstObjectByType<TomatoHarvestBasket>();
            nextBasketSearch = Time.unscaledTime + 1f;
        }
        if (basket != null && basket.Contains(transform.position)) basket.Collect(this, source);
    }

    public void StoreIn(Transform tray)
    {
        stored = true;
        grabbable.enabled = false;
        foreach (GrabInteractable grab in GetComponentsInChildren<GrabInteractable>()) grab.enabled = false;
        foreach (HandGrabInteractable grab in GetComponentsInChildren<HandGrabInteractable>()) grab.enabled = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.useGravity = false;
        body.isKinematic = true;
        transform.SetParent(tray, true);
        transform.position = tray.position + Vector3.up * 0.045f;
    }
}
