using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prepares garden props for both tracked hands and Meta Quest controllers.
/// </summary>
public sealed class GardenGrabSetup : MonoBehaviour
{
    private const string SetupResourceName = "GardenGrabSetup";

    [SerializeField] private GameObject interactionRigPrefab;
    // Extra names can still be supplied by existing scene/prefab overrides.
    [SerializeField] private string[] grabbableObjectNames = { "RiceBin", "Plant_Pot" };
    [SerializeField] private ControllerButtonUsage grabButton = ControllerButtonUsage.GripButton;
    [SerializeField, Min(0.1f)] private float scanInterval = 0.5f;

    private static readonly string[] GardenPropNames =
    {
        "RiceBin", "Plant_Pot", "Basket_S", "Basket_L", "Shovel", "Rake", "Pruner",
        "Watering", "WateringCup", "bottle-oil", "PotSmall", "PotBig", "PotRectangle",
        "NameStake", "Sprout", "Tomato", "Potato", "Corn", "Carrot_Orange",
        "Mint", "Hydrangea", "TulipRed", "TulipYellow", "Sickle", "Mallet",
        "CeremicPot", "Barrel", "WoodenCrate_S", "WoodenCrate_L", "WoodenBox"
    };

    private readonly HashSet<GameObject> configuredObjects = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForGardenScene()
    {
        if (!IsGardenScene(SceneManager.GetActiveScene()) ||
            FindFirstObjectByType<GardenGrabSetup>(FindObjectsInactive.Include) != null)
        {
            return;
        }

        GameObject setupPrefab = Resources.Load<GameObject>(SetupResourceName);
        if (setupPrefab == null)
        {
            Debug.LogError($"Could not load Resources/{SetupResourceName}.prefab.");
            return;
        }

        Instantiate(setupPrefab);
    }

    private void Awake()
    {
        EnsureInteractionRig();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            ConfigureSceneObjects();
            // Continue discovering spawned props without resetting held/planted objects.
            yield return new WaitForSecondsRealtime(scanInterval);
        }
    }

    internal static bool IsGardenScene(Scene scene)
    {
        return scene.name == "Garden_Test" || scene.name == "Garden_Moves" ||
               scene.name == "Garden_Moves - Copy";
    }

    internal void ConfigureSceneObjects()
    {
        configuredObjects.RemoveWhere(target => target == null);
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject target = candidate.gameObject;
                if (!target.activeInHierarchy || configuredObjects.Contains(target)) continue;
                // A seed planted inside a pot must keep following the pot, including its visuals.
                SeedItem seed = target.GetComponentInParent<SeedItem>();
                if (seed != null && seed.IsPlanted) continue;

                bool explicitlyGrabbable = target.GetComponent<Grabbable>() != null ||
                    target.GetComponent<GardenPot>() != null ||
                    target.GetComponent<WateringCan>() != null ||
                    target.GetComponent<SeedItem>() != null;
                if (!explicitlyGrabbable)
                {
                    if (!MatchesPropName(target.name, GardenPropNames) &&
                        !MatchesPropName(target.name, grabbableObjectNames)) continue;
                    // Model prefabs can repeat the prop name inside a wrapper (e.g. PotSmall).
                    // Those meshes must move with the wrapper instead of gaining another body.
                    if (candidate.parent != null &&
                        candidate.parent.GetComponentInParent<Grabbable>() != null) continue;
                }

                ConfigureGrabbable(target);
                configuredObjects.Add(target);
            }
        }
    }

    private static bool MatchesPropName(string name, string[] names)
    {
        if (names == null) return false;
        foreach (string propName in names)
        {
            if (string.IsNullOrEmpty(propName)) continue;
            if (name == propName || name.StartsWith(propName + " (") ||
                name.StartsWith(propName + "(Clone)") || name.StartsWith(propName + " ")) return true;
        }
        return false;
    }

    private void EnsureInteractionRig()
    {
        if (FindFirstObjectByType<GrabInteractor>(FindObjectsInactive.Include) == null &&
            FindFirstObjectByType<HandGrabInteractor>(FindObjectsInactive.Include) == null)
        {
            if (interactionRigPrefab == null)
            {
                Debug.LogError("Garden grab setup has no Meta interaction rig prefab assigned.", this);
                return;
            }

            GameObject rig = Instantiate(interactionRigPrefab);
            rig.name = "[Cozy Garden] Meta Quest Interaction Rig";
        }

        ControllerSelector[] selectors =
            FindObjectsByType<ControllerSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (ControllerSelector selector in selectors)
        {
            if (selector.gameObject.name == "GripButtonSelector")
            {
                selector.ControllerButtonUsage = grabButton;
            }
        }
    }

    internal static void ConfigureGrabbable(GameObject target)
    {
        SeedItem seed = target.GetComponent<SeedItem>();
        if (seed != null && seed.IsPlanted) return;

        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.AddComponent<Rigidbody>();
        }

        // Non-convex imported meshes cannot belong to a moving Rigidbody.
        bool hasSolidCollider = false;
        foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled || collider.GetComponentInParent<Rigidbody>() != body) continue;
            if (collider is MeshCollider mesh && !mesh.convex)
            {
                // Keep the collider reference used by existing Meta interactables.
                // Their proximity checks also visit disabled colliders.
                mesh.convex = true;
            }
            hasSolidCollider |= !collider.isTrigger;
        }
        // Planting trigger zones are preserved, but do not replace physical collisions.
        if (!hasSolidCollider) AddBoundsCollider(target);

        Grabbable grabbable = target.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Grabbable>();
        }

        grabbable.InjectOptionalTargetTransform(target.transform);
        grabbable.InjectOptionalRigidbody(body);

        if (grabbable.SelectingPointsCount == 0)
        {
            body.useGravity = true;
            body.isKinematic = false;
        }
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        GrabInteractable grabInteractable = null;
        foreach (GrabInteractable candidate in target.GetComponentsInChildren<GrabInteractable>(true))
        {
            if (candidate.GetComponentInParent<Rigidbody>() == body)
            {
                grabInteractable = candidate;
                break;
            }
        }
        if (grabInteractable == null)
        {
            GameObject interactableObject = new GameObject("ControllerGrabInteractable");
            interactableObject.transform.SetParent(target.transform, false);
            grabInteractable = interactableObject.AddComponent<GrabInteractable>();
        }

        grabInteractable.InjectRigidbody(body);
        grabInteractable.InjectOptionalPointableElement(grabbable);

        HandGrabInteractable handGrab = null;
        foreach (HandGrabInteractable candidate in target.GetComponentsInChildren<HandGrabInteractable>(true))
        {
            if (candidate.GetComponentInParent<Rigidbody>() == body)
            {
                handGrab = candidate;
                break;
            }
        }
        if (handGrab == null)
        {
            GameObject handGrabObject = new GameObject("HandGrabInteractable");
            handGrabObject.transform.SetParent(target.transform, false);
            handGrab = handGrabObject.AddComponent<HandGrabInteractable>();
        }

        handGrab.InjectRigidbody(body);
        handGrab.InjectOptionalPointableElement(grabbable);
        handGrab.InjectSupportedGrabTypes(GrabTypeFlags.All);
    }

    private static void AddBoundsCollider(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        BoxCollider collider = target.AddComponent<BoxCollider>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds localBounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (renderer.GetComponentInParent<Rigidbody>() != target.GetComponent<Rigidbody>()) continue;
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            if (!hasBounds)
            {
                localBounds = new Bounds(target.transform.InverseTransformPoint(center), Vector3.zero);
                hasBounds = true;
            }

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        localBounds.Encapsulate(target.transform.InverseTransformPoint(corner));
                    }
                }
            }
        }

        if (hasBounds)
        {
            collider.center = localBounds.center;
            collider.size = Vector3.Max(localBounds.size, Vector3.one * 0.001f);
        }
    }

}
