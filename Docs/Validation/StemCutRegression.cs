using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class StemCutRegression
{
    public static string Main()
    {
        var checks = new List<string>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Check(bool passed, string text)
        {
            if (!passed) throw new Exception(text);
            checks.Add("PASS: " + text);
        }
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Paused Play fixture");
            var seeds = Object.FindFirstObjectByType<SeedsController>();
            var fruit = seeds.PrototypeTomato.seed.HarvestFruit;
            var filter = fruit.GetComponentInChildren<MeshFilter>();
            Vector3 tip = filter.transform.InverseTransformPoint(fruit.StemPosition);
            Check(tip.x > -0.06f && tip.x < -0.02f && tip.y > 0.015f && tip.y < 0.06f && tip.z > 0.09f,
                "Cut target lies in the actual stalk terminal ring of RootswithTomatoes.036, outside the calyx leaf tips");
            Check(fruit.IsRipe && fruit.CanCutStem && !fruit.IsStemCut && !fruit.IsPickedUp,
                "Ripe source state starts attached and unpicked");
            var grab = fruit.GetComponent<Grabbable>();
            Check(!grab.enabled && !fruit.GetComponent<Rigidbody>().useGravity,
                "Attached fruit cannot be grabbed and stays stable");
            Vector3 scale = fruit.transform.lossyScale;
            typeof(TomatoRuntimeVerification).GetMethod("VerifyStemCut", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { fruit, seeds });
            Check(fruit.IsStemCut && fruit.transform.parent == null && !fruit.IsPickedUp && !fruit.CanCutStem,
                "Existing Pruner passes invalid-action checks and a full stroke at the real stalk detaches exactly once");
            Check(Vector3.Distance(scale, fruit.transform.lossyScale) < 0.0001f,
                "Cut preserves fruit world scale");
            Check(grab.enabled && fruit.GetComponent<Rigidbody>().useGravity && !fruit.GetComponent<Rigidbody>().isKinematic,
                "Cut enables existing Meta grab and dynamic gravity");
            typeof(TomatoFruit).GetField("recoveryRemaining", flags).SetValue(fruit, 0f);
            typeof(TomatoFruit).GetMethod("Update", flags).Invoke(fruit, null);
            Check(fruit.IsStemCut && !fruit.IsPickedUp && fruit.transform.parent == null && !fruit.CanCutStem,
                "An unpicked cut tomato recovers without reconnecting or requiring a second cut");
            var basket = Object.FindFirstObjectByType<TomatoHarvestBasket>();
            var volume = (BoxCollider)new SerializedObject(basket).FindProperty("collectionVolume").objectReferenceValue;
            Vector3 delivery = volume.transform.TransformPoint(volume.center);
            fruit.transform.position = delivery;
            basket.Collect(fruit, fruit.Source);
            Check(!seeds.HarvestComplete && !fruit.IsPickedUp, "Falling into tray after cutting without a pickup does not harvest");
            typeof(Grabbable).GetMethod("Start", flags).Invoke(grab, null);
            Pose pose = new(fruit.transform.position, fruit.transform.rotation);
            grab.ProcessPointerEvent(new PointerEvent(4151, PointerEventType.Hover, pose));
            Check(!fruit.IsPickedUp, "Hand hover does not count as pickup");
            grab.ProcessPointerEvent(new PointerEvent(4151, PointerEventType.Select, pose));
            Check(fruit.IsPickedUp && grab.SelectingPointsCount == 1 && !seeds.HarvestComplete,
                "Meta Select completes pickup independently of tray delivery");
            basket.Collect(fruit, fruit.Source);
            Check(!seeds.HarvestComplete, "Held fruit cannot complete tray delivery");
            grab.ProcessPointerEvent(new PointerEvent(4151, PointerEventType.Unselect, pose));
            grab.ProcessPointerEvent(new PointerEvent(4151, PointerEventType.Unhover, pose));
            typeof(TomatoFruit).GetMethod("Update", flags).Invoke(fruit, null);
            Check(seeds.HarvestComplete && fruit.transform.parent == basket.transform && !grab.enabled,
                "Releasing the separated picked tomato inside the tray records harvest and stores it");
            int count = seeds.Calendar.Journal.Count(entry => entry.action == "Harvested");
            basket.Collect(fruit, fruit.Source);
            Check(count == 1 && seeds.Calendar.Journal.Count(entry => entry.action == "Harvested") == 1,
                "Repeated delivery does not repeat harvest credit");
            checks.Add("NOT TESTED: real tracking/physical gestures, audio recordings, Quest 3 comfort or performance.");
            return string.Join("\n", checks);
        }
        catch (Exception error)
        {
            checks.Add("FAIL: " + error);
            throw;
        }
        finally
        {
            File.WriteAllLines("Docs/Validation/StemCutRegression.txt", checks);
        }
    }
}
