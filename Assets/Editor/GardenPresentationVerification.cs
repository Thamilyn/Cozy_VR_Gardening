using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Isolated placement checks; does not save or change garden scenes.</summary>
public static class GardenPresentationVerification
{
    [MenuItem("Garden/Verify presentation clearance")]
    public static void Verify()
    {
        StringBuilder report = new("Unity " + Application.unityVersion + "\n");
        try
        {
            Check("Calendar edges clear a mesh table", CalendarEdges, report);
            Check("Animated demonstration and caption move together", AnimatedCaption, report);
            Check("A tall wall selects nearer free space", NearerSpace, report);
            Check("Blocked UI hides and recovers its interaction collider", BlockedPanel, report);
            Check("Presentations do not intersect each other", SeparatePanels, report);
            Check("Repeated obstructions cannot accumulate height", HeightLimit, report);
            Check("A static board stays still when the can crosses it", StaticBoard, report);
            Check("A fixed board remains visible before tracking and behind transient occluders", StaticBoardVisibility, report);
            Check("Calendar selection demo finds a side slot below the height ceiling", SelectionBesideCalendar, report);
            Check("Moving UI cannot become the Meta player's floor and remains pointable", PlayerGrounding, report);
            Check("An elevated rig and capsule start on the real floor", PlayerStartingFloor, report);
            Check("Sitting and standing never ground the player on their own colliders", PlayerPosture, report);
            Check("Initial eye calibration preserves the floor and subsequent tracked movement", InitialEyeCalibration, report);
            Debug.Log(report.ToString());
        }
        catch (Exception error)
        {
            report.AppendLine("FAIL: " + error.Message);
            throw;
        }
        finally
        {
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllText("Docs/Validation/GardenPresentationVerification.txt", report.ToString());
        }
    }

    private static void Check(string name, Action<Transform, Transform> test, StringBuilder report)
    {
        GameObject fixture = new("Presentation clearance verification") { hideFlags = HideFlags.HideAndDontSave };
        fixture.transform.position = new Vector3(1000f, 1000f, 1000f);
        GameObject eye = new("Verification viewer");
        eye.transform.SetParent(fixture.transform, false);
        eye.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        try
        {
            test(fixture.transform, eye.transform);
            report.AppendLine("PASS: " + name);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);
            Physics.SyncTransforms();
        }
    }

    private static void CalendarEdges(Transform fixture, Transform eye)
    {
        Collider table = Solid(fixture, new Vector3(0f, 0.8f, 1.3f), new Vector3(2f, 0.5f, 1f));
        MeshCollider mesh = table.gameObject.AddComponent<MeshCollider>();
        mesh.sharedMesh = table.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(table);
        GardenPresentationPlacement panel = Panel(fixture, eye, out RectTransform content, out _);
        Assert(content.position.y > mesh.bounds.max.y, "The panel centre should already be above the table.");
        Assert(Bottom(content) > mesh.bounds.max.y + 0.03f, "The bottom edge must also clear the table.");
        float settledHeight = panel.transform.position.y;
        Assert(panel.ResolveNow() && Mathf.Abs(panel.transform.position.y - settledHeight) < 0.001f,
            "An unobstructed panel must remain stable.");
    }

    private static void AnimatedCaption(Transform fixture, Transform eye)
    {
        Collider table = Solid(fixture, new Vector3(0f, 0.8f, 1.3f), new Vector3(2f, 0.5f, 1f));
        GameObject demo = new("Animated demonstration");
        demo.transform.SetParent(fixture, false);
        demo.transform.localPosition = new Vector3(0f, 1.3f, 1.3f);
        Collider hand = Solid(demo.transform, Vector3.zero, Vector3.one * 0.12f);
        Collider caption = Solid(demo.transform, new Vector3(0f, -0.35f, 0f), new Vector3(0.3f, 0.06f, 0.02f));
        UnityEngine.Object.DestroyImmediate(hand);
        UnityEngine.Object.DestroyImmediate(caption);
        GardenPresentationPlacement placement = demo.AddComponent<GardenPresentationPlacement>();
        Physics.SyncTransforms();
        placement.Configure(demo.transform, eye);
        Renderer text = demo.transform.GetChild(1).GetComponent<Renderer>();
        Assert(text.bounds.min.y > table.bounds.max.y, "The caption must clear the tabletop.");
        Vector3 initialPosition = demo.transform.position;
        text.transform.localPosition = new Vector3(0f, -0.6f, 0f);
        Assert(placement.ResolveNow() && !text.bounds.Intersects(table.bounds) &&
            !demo.transform.GetChild(0).GetComponent<Renderer>().bounds.Intersects(table.bounds) &&
            demo.transform.position != initialPosition && demo.transform.position.y <= fixture.position.y + 1.651f,
            "A lower animation pose must move the whole demonstration to free space within its total lift limit.");
    }

    private static void NearerSpace(Transform fixture, Transform eye)
    {
        Collider wall = Solid(fixture, new Vector3(0f, 1.5f, 1.3f), new Vector3(3f, 4f, 0.2f));
        GardenPresentationPlacement panel = Panel(fixture, eye, out RectTransform content, out _);
        Assert(panel.ResolveNow() && content.position.z < wall.bounds.min.z - 0.04f,
            "A tall wall must cause a move towards the viewer.");
    }

    private static void BlockedPanel(Transform fixture, Transform eye)
    {
        Collider blocker = Solid(fixture, new Vector3(0f, 1.4f, 1.3f), Vector3.one * 8f);
        GardenPresentationPlacement panel = Panel(fixture, eye, out _, out BoxCollider interaction);
        Canvas canvas = panel.GetComponent<Canvas>();
        Assert(!canvas.enabled && !interaction.enabled, "Fully blocked UI and its hit target must be hidden.");
        blocker.enabled = false;
        Physics.SyncTransforms();
        Assert(panel.ResolveNow() && canvas.enabled && interaction.enabled,
            "UI and its hit target must recover when space becomes available.");
    }

    private static void SeparatePanels(Transform fixture, Transform eye)
    {
        Panel(fixture, eye, out RectTransform first, out _);
        GardenPresentationPlacement second = Panel(fixture, eye, out RectTransform content, out _);
        Bounds firstBounds = RectBounds(first);
        Assert(second.ResolveNow() && !RectBounds(content).Intersects(firstBounds),
            "Two panels opened in the same place must be separated.");
    }

    private static void HeightLimit(Transform fixture, Transform eye)
    {
        Collider obstacle = Solid(fixture, new Vector3(0f, 0.8f, 1.3f), new Vector3(4f, 0.5f, 4f));
        GardenPresentationPlacement panel = Panel(fixture, eye, out RectTransform content, out _);
        float initialHeight = fixture.position.y + 1.3f;
        for (int i = 0; i < 12; i++)
        {
            obstacle.transform.position += Vector3.up * 0.1f;
            Physics.SyncTransforms();
            panel.ResolveNow();
            Assert(panel.transform.position.y <= initialHeight + 0.351f,
                "Repeated checks must not reset the total lift budget.");
            Assert(RectBounds(content).center.y <= eye.position.y + 0.251f,
                "The visual centre must stay below the eye-relative ceiling.");
        }
        Assert(!panel.GetComponent<Canvas>().enabled,
            "When the table leaves no room under the ceiling, the panel must wait for free space.");
        obstacle.enabled = false;
        Physics.SyncTransforms();
        Assert(panel.ResolveNow() && panel.GetComponent<Canvas>().enabled, "The bounded panel must recover.");
        panel.transform.position = fixture.position + new Vector3(0f, 0.9f, 1.3f);
        panel.ResetReferencePose();
        panel.ResolveNow();
        Assert(panel.transform.position.y < initialHeight, "Reopening must accept a new placement reference.");
    }

    private static void StaticBoard(Transform fixture, Transform eye)
    {
        GardenPresentationPlacement panel = Panel(fixture, eye, out RectTransform content, out BoxCollider target);
        panel.Configure(content, eye, target, keepStatic: true);
        Vector3 position = panel.transform.position;
        Quaternion rotation = panel.transform.rotation;
        Collider can = Solid(fixture, position - fixture.position, Vector3.one * 0.2f);
        for (int i = 0; i < 4; i++)
        {
            can.transform.position += Vector3.up * 0.05f;
            eye.position += Vector3.right * 0.1f;
            Physics.SyncTransforms();
            panel.ResolveNow();
            Assert(panel.transform.position == position && panel.transform.rotation == rotation,
                "Watering and head movement must not reposition the fixed board.");
            Assert(panel.GetComponent<Canvas>().enabled,
                "Watering must not hide the fixed board.");
        }
    }

    private static void StaticBoardVisibility(Transform fixture, Transform eye)
    {
        Solid(fixture, new Vector3(0f, -0.1f, 1.3f), new Vector3(4f, 0.2f, 4f));
        Collider table = Solid(fixture, new Vector3(0f, 0.8f, 1.3f), new Vector3(2f, 0.5f, 1f));
        GardenPresentationPlacement panel = Panel(fixture, eye, out RectTransform content, out BoxCollider target);
        Vector3 authoredPosition = panel.transform.position;
        Quaternion authoredRotation = panel.transform.rotation;
        Assert(Bottom(content) > table.bounds.max.y, "The authored board must start clear of the table.");

        // On the first frame CenterEyeAnchor can still be at the tracking origin.
        eye.position = fixture.position;
        Canvas canvas = panel.GetComponent<Canvas>();
        panel.Configure(content, eye, target);
        Assert(!canvas.enabled, "The fixture must reproduce the old placement suppression before tracking.");
        panel.Configure(content, eye, target, keepStatic: true);
        Assert(canvas.enabled && panel.transform.position == authoredPosition,
            "An untracked camera must not lower or suppress a fixed instruction board.");
        Solid(fixture, new Vector3(0f, 0.8f, 0.6f), new Vector3(2f, 2f, 0.1f));
        for (int i = 0; i < 4; i++)
        {
            eye.position = fixture.position + Vector3.up * (i % 2 == 0 ? 1.1f : 1.7f);
            eye.rotation = Quaternion.Euler(0f, 90f * i, 0f);
            Physics.SyncTransforms();
            Assert(panel.ResolveNow() && canvas.enabled && target.enabled &&
                panel.transform.position == authoredPosition && panel.transform.rotation == authoredRotation,
                "Posture, head rotation and objects crossing the sight line must not hide or move the board.");
        }
    }

    private static Bounds RectBounds(RectTransform rectangle)
    {
        Vector3[] corners = new Vector3[4];
        rectangle.GetWorldCorners(corners);
        Bounds bounds = new(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++) bounds.Encapsulate(corners[i]);
        return bounds;
    }

    private static void SelectionBesideCalendar(Transform fixture, Transform eye)
    {
        Panel(fixture, eye, out RectTransform calendar, out _);
        // A low work surface blocks the initial demonstration and all centre-line near slots.
        Collider table = Solid(fixture, new Vector3(0f, 1f, 0.85f), new Vector3(0.5f, 0.5f, 1f));
        GameObject demo = new("Calendar selection demonstration");
        demo.transform.SetParent(fixture, false);
        demo.transform.localPosition = new Vector3(0f, 1.1f, 0.85f);
        Collider model = Solid(demo.transform, Vector3.zero, new Vector3(0.25f, 0.5f, 0.35f));
        Renderer visual = model.GetComponent<Renderer>();
        UnityEngine.Object.DestroyImmediate(model);
        GardenPresentationPlacement placement = demo.AddComponent<GardenPresentationPlacement>();
        Physics.SyncTransforms();
        placement.Configure(demo.transform, eye);
        Assert(placement.ResolveNow() && visual.enabled, "The selection demo must remain visible beside the calendar.");
        Assert(Mathf.Abs(demo.transform.localPosition.x) >= 0.55f - 0.001f &&
            !visual.bounds.Intersects(table.bounds) && !visual.bounds.Intersects(RectBounds(calendar)),
            "The side slot must clear both the table and the calendar.");
        Assert(demo.transform.localPosition.y <= 1.451f && visual.bounds.center.y <= eye.position.y + 0.251f,
            "The side search must preserve both height limits.");
    }

    private static GardenPresentationPlacement Panel(Transform fixture, Transform eye,
        out RectTransform content, out BoxCollider interaction)
    {
        GameObject root = new("Verification panel", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(fixture, false);
        root.transform.localPosition = new Vector3(0f, 1.3f, 1.3f);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(0.85f, 0.71f);
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        GameObject child = new("Hideable content", typeof(RectTransform));
        child.transform.SetParent(root.transform, false);
        content = child.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(0.85f, 0.71f);
        interaction = root.AddComponent<BoxCollider>();
        interaction.size = new Vector3(0.85f, 0.71f, 0.01f);
        GardenPresentationPlacement placement = root.AddComponent<GardenPresentationPlacement>();
        Physics.SyncTransforms();
        placement.Configure(content, eye, interaction);
        return placement;
    }

    private static void PlayerGrounding(Transform fixture, Transform eye)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GardenCalendarVR.prefab");
        Assert(prefab != null && prefab.GetComponent<BoxCollider>().isTrigger,
            "The saved calendar must be non-solid even before its runtime setup.");
        GardenPresentationPlacement panel = Panel(fixture, eye, out _, out BoxCollider target);
        panel.enabled = false;
        Assert(target.isTrigger, "Placement must keep UI targets non-solid.");
        panel.transform.SetPositionAndRotation(fixture.position + Vector3.up * 0.4f,
            Quaternion.Euler(90f, 0f, 0f));

        GameObject player = new("Verification player");
        player.transform.SetParent(fixture, false);
        Vector3 standingPosition = fixture.position + Vector3.up * 0.705f;
        player.transform.position = standingPosition;
        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.radius = 0.25f;
        capsule.height = 1.4f;
        var controller = player.AddComponent<Oculus.Interaction.Locomotion.CharacterController>();
        controller.InjectAllCharacterController(capsule);

        // Positive control: a solid panel inside the body reproduces the upward snap.
        target.isTrigger = false;
        Physics.SyncTransforms();
        Assert(controller.TryGround() && player.transform.position.y > standingPosition.y + 0.1f,
            "The fixture must reproduce the solid-panel grounding problem.");
        target.isTrigger = true;
        player.transform.position = standingPosition;
        Physics.SyncTransforms();
        Assert(!controller.TryGround() && player.transform.position == standingPosition,
            "A trigger panel inside the body must not become ground.");
        Collider floor = Solid(fixture, Vector3.down * 0.1f, new Vector3(4f, 0.2f, 4f));
        Physics.SyncTransforms();
        Assert(controller.TryGround() && controller.GroundHit.collider == floor,
            "The player must find the real floor beneath the trigger.");
        float groundedHeight = player.transform.position.y;
        for (int i = 0; i < 6; i++)
        {
            panel.transform.position += Vector3.up * 0.05f;
            Physics.SyncTransforms();
            Assert(controller.TryGround() && controller.GroundHit.collider == floor &&
                Mathf.Abs(player.transform.position.y - groundedHeight) < 0.001f,
                "Moving the panel repeatedly must preserve the real floor and player height.");
            Assert(target.Raycast(new Ray(target.bounds.center + Vector3.up, Vector3.down), out _, 2f),
                "The trigger must remain selectable through the Collider.Raycast used by Meta UI.");
        }
    }

    private static void PlayerStartingFloor(Transform fixture, Transform eye)
    {
        Collider floor = Solid(fixture, new Vector3(0f, 2f, 0f), new Vector3(4f, 0.2f, 4f));
        // A platform above the floor must not determine the spawn height.
        Solid(fixture, new Vector3(0f, 3.2f, 0f), new Vector3(2f, 0.2f, 2f));
        GameObject origin = new("Elevated player origin");
        origin.transform.SetParent(fixture, false);
        origin.transform.localPosition = Vector3.up * 4.4f;
        eye.SetParent(origin.transform, false);
        eye.localPosition = Vector3.up * 1.7f;
        GameObject body = new("Elevated locomotion capsule");
        body.transform.SetParent(origin.transform, false);
        body.transform.localPosition = Vector3.up * 3f;
        CapsuleCollider capsule = body.AddComponent<CapsuleCollider>();
        capsule.height = 1.4f;
        capsule.radius = 0.25f;
        var controller = body.AddComponent<Oculus.Interaction.Locomotion.CharacterController>();
        controller.InjectAllCharacterController(capsule);
        Physics.SyncTransforms();
        for (int i = 0; i < 2; i++)
        {
            Assert(GardenGrabSetup.PlacePlayerOnFloor(origin.transform, controller, floor),
                "Startup must find the assigned floor beneath an elevated rig.");
            Assert(Mathf.Abs(origin.transform.position.y - floor.bounds.max.y) < 0.001f &&
                Mathf.Abs(controller.Pose.position.y - controller.Height * 0.5f - controller.SkinWidth -
                    floor.bounds.max.y) < 0.001f,
                "Both the tracking origin and capsule feet must start at floor height.");
            Assert(Mathf.Abs(eye.localPosition.y - 1.7f) < 0.001f,
                "Startup must preserve the tracked head height.");
        }
        Vector3 settledOrigin = origin.transform.position;
        Pose settledCapsule = controller.Pose;
        floor.transform.position += Vector3.right * 10f;
        Physics.SyncTransforms();
        Assert(!GardenGrabSetup.PlacePlayerOnFloor(origin.transform, controller, floor) &&
            origin.transform.position == settledOrigin && controller.Pose.position == settledCapsule.position,
            "A missing floor beneath the spawn must leave the player unchanged.");
    }

    private static void PlayerPosture(Transform fixture, Transform eye)
    {
        GameObject rig = new("Verification player rig");
        rig.transform.SetParent(fixture, false);
        GameObject body = new("Player body");
        body.transform.SetParent(rig.transform, false);
        body.transform.localPosition = Vector3.up * 0.705f;
        CapsuleCollider capsule = body.AddComponent<CapsuleCollider>();
        capsule.height = 1.4f;
        capsule.radius = 0.25f;
        var controller = body.AddComponent<Oculus.Interaction.Locomotion.CharacterController>();
        controller.InjectAllCharacterController(capsule);
        controller.LayerMask = ~0;
        var walls = body.AddComponent<Oculus.Interaction.Locomotion.WallPenetrationTunneling>();
        walls.LayerMask = ~0;
        // A hand overlapping the body must not be usable as a platform beneath the head.
        Collider hand = Solid(rig.transform, Vector3.up * 0.4f, new Vector3(0.6f, 0.05f, 0.6f));
        Physics.SyncTransforms();
        Assert(controller.TryGround() && controller.GroundHit.collider == hand,
            "The fixture must reproduce grounding on a collider belonging to the player.");
        GardenGrabSetup.ConfigurePlayerCollisionLayers(rig.transform);
        int playerLayer = LayerMask.NameToLayer("GardenPlayer");
        Assert(playerLayer >= 0 && capsule.gameObject.layer == playerLayer && hand.gameObject.layer == playerLayer &&
            (controller.LayerMask.value & (1 << playerLayer)) == 0 &&
            (walls.LayerMask.value & (1 << playerLayer)) == 0,
            "Body and hand colliders must be excluded from locomotion and wall queries.");

        Collider floor = Solid(fixture, Vector3.down * 0.1f, new Vector3(4f, 0.2f, 4f));
        Physics.SyncTransforms();
        GardenGrabSetup.PlacePlayerOnFloor(rig.transform, controller, floor);
        for (int i = 0; i < 12; i++)
        {
            float height = i % 2 == 0 ? 1f : 1.8f;
            controller.TrySetHeight(height);
            Physics.SyncTransforms();
            Assert(controller.TryGround() && controller.GroundHit.collider == floor,
                "Seated and standing capsules must find the real floor through the player's hand.");
            Assert(Mathf.Abs(controller.Pose.position.y - controller.Height * 0.5f - floor.bounds.max.y) < 0.01f &&
                rig.transform.localPosition.y == 0f,
                "Repeated posture changes must keep the feet and tracking origin on the floor.");
        }
    }

    private static void InitialEyeCalibration(Transform fixture, Transform eye)
    {
        GameObject tracking = new("Tracking space");
        tracking.transform.SetParent(fixture, false);
        eye.SetParent(tracking.transform, false);
        eye.localPosition = new Vector3(0f, 1.65f, 0.1f);
        GameObject hand = new("Tracked hand");
        hand.transform.SetParent(tracking.transform, false);
        hand.transform.localPosition = new Vector3(0.25f, 1.1f, 0.3f);
        Vector3 originPosition = fixture.position;
        Vector3 measuredEye = eye.localPosition;
        Vector3 measuredHand = hand.transform.localPosition;
        Vector3 handWorldPosition = hand.transform.position;
        for (int i = 0; i < 2; i++)
        {
            Assert(GardenPlayerHeightCalibration.ApplyInitialHeight(fixture, tracking.transform, eye, 1.3f),
                "A tracked seated pose must support initial height calibration.");
            Assert(fixture.position == originPosition &&
                Mathf.Abs(eye.position.y - fixture.position.y - 1.3f) < 0.001f,
                "Calibration must change eye height without moving the locomotion origin.");
            Assert(eye.localPosition == measuredEye && hand.transform.localPosition == measuredHand &&
                Mathf.Abs(hand.transform.position.y - handWorldPosition.y + 0.35f) < 0.001f,
                "Head and hands must receive the same offset without altering the tracked local poses.");
        }
        eye.localPosition += Vector3.up * 0.4f;
        Assert(Mathf.Abs(eye.position.y - fixture.position.y - 1.7f) < 0.001f,
            "Standing up after calibration must preserve the real relative change in head height.");
        Vector3 calibratedPosition = tracking.transform.position;
        Assert(!GardenPlayerHeightCalibration.ApplyInitialHeight(fixture, fixture, eye, 1.3f) &&
            !GardenPlayerHeightCalibration.ApplyInitialHeight(fixture, tracking.transform, eye, float.NaN) &&
            tracking.transform.position == calibratedPosition && fixture.position == originPosition,
            "Invalid calibration targets must leave both the floor origin and tracking space unchanged.");
    }

    private static Collider Solid(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        return cube.GetComponent<Collider>();
    }

    private static float Bottom(RectTransform rectangle)
    {
        Vector3[] corners = new Vector3[4];
        rectangle.GetWorldCorners(corners);
        return Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
