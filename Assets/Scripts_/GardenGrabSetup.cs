using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Prepares the movable garden props for Meta Quest controller grab interaction.
/// </summary>
public sealed class GardenGrabSetup : MonoBehaviour
{
    private const string SetupResourceName = "GardenGrabSetup";

    [SerializeField] private GameObject interactionRigPrefab;
    [SerializeField] private string[] grabbableObjectNames = { "RiceBin", "Plant_Pot" };
    [SerializeField] private ControllerButtonUsage grabButton = ControllerButtonUsage.GripButton;
    [SerializeField, Min(0.1f)] private float searchDuration = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForGardenScene()
    {
        if (SceneManager.GetActiveScene().name != "Garden_Test" ||
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
        float deadline = Time.unscaledTime + searchDuration;

        do
        {
            bool allObjectsReady = true;

            foreach (string objectName in grabbableObjectNames)
            {
                GameObject target = FindSceneObject(objectName);
                if (target == null)
                {
                    allObjectsReady = false;
                    continue;
                }

                ConfigureGrabbable(target);
            }

            if (allObjectsReady)
            {
                yield break;
            }

            yield return null;
        }
        while (Time.unscaledTime < deadline);

        foreach (string objectName in grabbableObjectNames)
        {
            if (FindSceneObject(objectName) == null)
            {
                Debug.LogWarning($"Garden grab setup could not find a GameObject named '{objectName}'.", this);
            }
        }
    }

    private void EnsureInteractionRig()
    {
        if (FindFirstObjectByType<GrabInteractor>(FindObjectsInactive.Include) == null)
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
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = target.AddComponent<Rigidbody>();
        }

        body.useGravity = true;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (target.GetComponentInChildren<Collider>(true) == null)
        {
            AddBoundsCollider(target);
        }

        Grabbable grabbable = target.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = target.AddComponent<Grabbable>();
        }

        grabbable.InjectOptionalTargetTransform(target.transform);
        grabbable.InjectOptionalRigidbody(body);

        GrabInteractable grabInteractable = target.GetComponentInChildren<GrabInteractable>(true);
        if (grabInteractable == null)
        {
            GameObject interactableObject = new GameObject("ControllerGrabInteractable");
            interactableObject.transform.SetParent(target.transform, false);
            grabInteractable = interactableObject.AddComponent<GrabInteractable>();
        }

        grabInteractable.InjectRigidbody(body);
        grabInteractable.InjectOptionalPointableElement(grabbable);
    }

    private static void AddBoundsCollider(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        BoxCollider collider = target.AddComponent<BoxCollider>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds firstBounds = renderers[0].bounds;
        Bounds localBounds = new Bounds(
            target.transform.InverseTransformPoint(firstBounds.center),
            Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

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

        collider.center = localBounds.center;
        collider.size = localBounds.size;
    }

    private static GameObject FindSceneObject(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }
}
