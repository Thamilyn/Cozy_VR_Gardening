using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class HarvestVoiceRegression
{
    public static string Main()
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var checks = new List<string>();
        GameObject fixture = null;
        void Check(bool passed, string message)
        {
            if (!passed) throw new Exception(message);
            checks.Add("PASS: " + message);
        }
        void Set(object target, string field, object value) => target.GetType().GetField(field, flags).SetValue(target, value);
        void Call(object target, string method) => target.GetType().GetMethod(method, flags).Invoke(target, null);
        try
        {
            Check(EditorApplication.isPlaying && EditorApplication.isPaused, "Paused fixture, with no physical input required");
            var catalogue = Resources.Load<GronnyAudioCatalogue>("GronnyAudio/Tutorial");
            foreach (string id in new[] { "harvest_cut_stem", "harvest_stem_cut", "harvest_picked" })
            {
                var cue = catalogue.Find(id);
                Check(cue != null && cue.clip != null && cue.clip.length > 1f && cue.clip.channels == 1 &&
                    !string.IsNullOrWhiteSpace(cue.transcript), id + " has real imported mono audio and a transcript");
            }
            var seeds = Object.FindFirstObjectByType<SeedsController>();
            var fruit = seeds.PrototypeTomato.seed.HarvestFruit;
            var state = seeds.GetState(seeds.PrototypeTomato);
            fixture = new GameObject("Temporary harvest voice regression");
            var demo = fixture.AddComponent<GardenControlDemonstration>();
            demo.Configure(catalogue, null);
            var voice = fixture.AddComponent<GronnyVoicePlayer>();
            voice.Configure(catalogue, demo);
            var source = fixture.GetComponent<AudioSource>();
            // Exercise the real tutorial queue with its real crop; pause prevents its
            // Start coroutine from creating a second voice on this isolated fixture.
            var tutorial = fixture.AddComponent<GronnyTutorial>();
            Set(tutorial, "catalogue", catalogue);
            Set(tutorial, "seeds", seeds);
            Set(tutorial, "calendar", seeds.Calendar);
            Set(tutorial, "voice", voice);
            Set(tutorial, "demonstration", demo);
            Set(tutorial, "initialized", true);
            Set(tutorial, "phase", 5);
            void Queue() => Call(tutorial, "QueueHarvest");
            int Pending() => ((IList)typeof(GronnyVoicePlayer).GetField("pending", flags).GetValue(voice)).Count;
            void FinishSentence()
            {
                source.Stop();
                Call(voice, "Update");
                Set(voice, "nextVoiceAt", 0f);
                Call(voice, "Update");
            }
            state.tomatoPhase = 4;
            state.stage = GrowthStage.GreenFruit.ToString();
            Queue();
            Check(Pending() == 0, "A green tomato does not request the ripe instruction");
            state.tomatoPhase = 5;
            state.stage = GrowthStage.Mature.ToString();
            state.soilCovered = false;
            Queue();
            Check(Pending() == 0, "Missing soil cover does not request harvesting guidance");
            state.soilCovered = true;
            Queue(); Queue(); Queue();
            Check(Pending() == 1, "Ripe instruction is queued once");
            Call(voice, "Update");
            Check(voice.CurrentCueId == "harvest_cut_stem" && source.clip == catalogue.Find("harvest_cut_stem").clip,
                "Ripe event selects the generated stem-cut instruction");
            typeof(TomatoRuntimeVerification).GetMethod("VerifyStemCut", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { fruit, seeds });
            Queue(); Queue(); Queue();
            Check(Pending() == 1 && voice.CurrentCueId == "harvest_cut_stem",
                "Valid cut queues one acknowledgement and preserves the current sentence");
            FinishSentence();
            Check(voice.CurrentCueId == "harvest_stem_cut" && source.clip == catalogue.Find("harvest_stem_cut").clip,
                "Cut acknowledgement follows the ripe instruction");
            var grab = fruit.GetComponent<Grabbable>();
            typeof(Grabbable).GetMethod("Start", flags).Invoke(grab, null);
            var basket = Object.FindFirstObjectByType<TomatoHarvestBasket>();
            var volume = (BoxCollider)new SerializedObject(basket).FindProperty("collectionVolume").objectReferenceValue;
            fruit.transform.position = volume.transform.TransformPoint(volume.center);
            Pose pose = new(fruit.transform.position, fruit.transform.rotation);
            grab.ProcessPointerEvent(new PointerEvent(8151, PointerEventType.Hover, pose));
            grab.ProcessPointerEvent(new PointerEvent(8151, PointerEventType.Select, pose));
            Queue(); Queue();
            Check(fruit.IsPickedUp && Pending() == 1 && voice.CurrentCueId == "harvest_stem_cut",
                "Meta pickup queues one congratulation behind the cut acknowledgement");
            grab.ProcessPointerEvent(new PointerEvent(8151, PointerEventType.Unselect, pose));
            grab.ProcessPointerEvent(new PointerEvent(8151, PointerEventType.Unhover, pose));
            Call(fruit, "Update");
            Check(state.harvested, "Fast tray delivery uses the existing harvest path");
            Set(tutorial, "completed", true);
            voice.Enqueue("13_complete", "complete", () => state.harvested);
            FinishSentence();
            Check(voice.CurrentCueId == "harvest_picked" && source.clip == catalogue.Find("harvest_picked").clip,
                "Pickup congratulation survives immediate delivery and tutorial completion");
            Queue(); Queue();
            Check(Pending() == 1, "Repeated observations cannot replay delivered harvest cues");
            FinishSentence();
            Check(voice.CurrentCueId == "13_complete", "Session closing follows all three harvest cues");
            Check(fixture.GetComponents<AudioSource>().Length == 1, "All harvest speech shares one AudioSource");
            Check(seeds.Calendar.Journal.Count(entry => entry.action == "Assisted: harvest_cut_stem") == 1,
                "Repeated observations do not duplicate spoken help credit");
            checks.Add("NOT TESTED: perceived voice naturalness, real-time sentence duration, hardware audio or Quest 3 tracking. Sentence endings were advanced explicitly.");
            return string.Join("\n", checks);
        }
        catch (Exception error)
        {
            checks.Add("FAIL: " + error);
            throw;
        }
        finally
        {
            if (fixture != null) Object.DestroyImmediate(fixture);
            File.WriteAllLines("Docs/Validation/HarvestVoiceRegression.txt", checks);
        }
    }
}
