using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PrunerTableCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    static void Check(bool condition, string message, List<string> checks)
    { if (!condition) throw new InvalidOperationException(message); checks.Add("PASS: " + message); }
    static void Continue(GardenMenuController menu)
    {
        float now=Time.unscaledTime;
        Call(menu,"HandleInputRelease",false,now);
        Call(menu,"HandleInputRelease",true,now);
        Call(menu,"HandleInputRelease",true,now+0.16f);
        Call(menu,"Continue");
        Call(menu,"HandleInputRelease",true,now);
        Call(menu,"HandleInputRelease",true,now+0.21f);
    }
    public static string Main()
    {
        EditorApplication.isPaused=true;
        var checks=new List<string>();
        try
        {
            var primary=Object.FindObjectsByType<GardenPruner>(FindObjectsSortMode.None)
                .Single(p=>Vector3.Distance(p.transform.position,new Vector3(-0.15f,0.906f,-2.36f))<0.03f);
            var body=primary.GetComponent<Rigidbody>();
            var grab=primary.GetComponent<Grabbable>();
            var home=new Pose(primary.transform.position,primary.transform.rotation);
            Check(primary.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)==2,"Both pruning-shear meshes are visible beside the tomato pot",checks);
            Check((bool)typeof(GardenPruner).GetField("hasTableHome",Private).GetValue(primary),"Working-table collider supports the new position",checks);
            Check(body.constraints==RigidbodyConstraints.FreezeAll && !body.isKinematic,"Tool is parked without changing Meta's dynamic body state",checks);
            Check(primary.GetComponentsInChildren<GrabInteractable>().All(g=>g.enabled && g.Rigidbody==body && ReferenceEquals(g.PointableElement,grab)) &&
                primary.GetComponentsInChildren<HandGrabInteractable>().All(g=>g.enabled && g.Rigidbody==body && ReferenceEquals(g.PointableElement,grab)),
                "Both Meta grab types are ready even in the initial menu",checks);
            Check(Object.FindObjectsByType<GardenPruner>(FindObjectsSortMode.None).Where(p=>p!=primary)
                .All(p=>!(bool)typeof(GardenPruner).GetField("hasTableHome",Private).GetValue(p)),"Other two copies retain their existing behavior",checks);
            SimulationMode previous=Physics.simulationMode;
            try
            {
                Physics.simulationMode=SimulationMode.Script;
                body.AddForce(Vector3.right*10f,ForceMode.Impulse);
                for(int i=0;i<150;i++) Physics.Simulate(0.02f);
            }
            finally { Physics.simulationMode=previous; }
            Check(Vector3.Distance(primary.transform.position,home.position)<0.001f,"Initial menu and three seconds of physics cannot displace the tool",checks);
            Continue(Object.FindFirstObjectByType<GardenMenuController>());
            Check(!GardenMenuController.GardenSuspended,"Session starts through real menu release flow",checks);
            foreach(IPointableElement element in new IPointableElement[]{primary.GetComponentInChildren<GrabInteractable>().PointableElement,
                primary.GetComponentInChildren<HandGrabInteractable>().PointableElement})
            {
                Pose initial=new Pose(primary.transform.position,primary.transform.rotation);
                Pose lifted=new Pose(initial.position+Vector3.up*0.12f,initial.rotation);
                element.ProcessPointerEvent(new PointerEvent(9600,PointerEventType.Hover,initial));
                element.ProcessPointerEvent(new PointerEvent(9600,PointerEventType.Select,initial));
                element.ProcessPointerEvent(new PointerEvent(9600,PointerEventType.Move,lifted));
                Check(grab.SelectingPointsCount==1 && body.constraints==RigidbodyConstraints.None && Vector3.Distance(primary.transform.position,lifted.position)<0.002f,
                    "Meta pickup releases parking and moves the shears",checks);
                element.ProcessPointerEvent(new PointerEvent(9600,PointerEventType.Unselect,lifted));
                element.ProcessPointerEvent(new PointerEvent(9600,PointerEventType.Unhover,lifted));
                Check(!body.isKinematic && grab.SelectingPointsCount==0,"Released shears use normal physics",checks);
            }
            body.position=home.position+Vector3.right*0.2f;
            primary.transform.position=body.position;
            typeof(GardenPruner).GetField("droppedFor",Private).SetValue(primary,2f);
            Call(primary,"UpdateTableRecovery");
            Check(Vector3.Distance(primary.transform.position,home.position)>0.1f,"Placing the shears elsewhere on the table does not recall them",checks);
            body.position=new Vector3(home.position.x,0.05f,home.position.z);
            primary.transform.position=body.position;
            typeof(GardenPruner).GetField("droppedFor",Private).SetValue(primary,2f);
            Call(primary,"UpdateTableRecovery");
            Check(Vector3.Distance(primary.transform.position,home.position)<0.001f && body.constraints==RigidbodyConstraints.FreezeAll,
                "After the drop timer expires the shears return to their visible table position",checks);
            Check(grab.SelectingPointsCount==0,"Recovery does not create a synthetic selection",checks);
            checks.Add("NOT TESTED: physical Quest sensors or perception; drop timer expiry was supplied synthetically.");
            checks.Add("RESULT: PASS");
            return string.Join("\n",checks);
        }
        finally { File.WriteAllLines("Docs/Validation/PrunerTableVerification.txt",checks); }
    }
}
