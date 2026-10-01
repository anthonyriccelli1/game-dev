using System.Collections.Generic;
using UnityEngine;

namespace RestaurantCity {
    // Presentation only: authoritative Work progress and its accepted event are inputs, never outputs.
    // Bound to the furnishing so rebuilds destroy the tool, food and audio together.
    public sealed class ChoppingFeedback : MonoBehaviour {
        Transform root, knife;
        readonly GameObject[] whole = new GameObject[6], cut = new GameObject[6];
        AudioSource sound;
        float previousProgress, phase;
        int previousItem = -1;
        public bool Active { get; private set; }
        public float Ratio { get; private set; }
        public int StrokeCount { get; private set; }

        public static Vector3 BoardTop(Transform station) {
            var board=station.Find("ChoppingBoard") ?? station.Find("Chopping board");
            return board ? board.localPosition + Vector3.up*(board.localScale.y*.5f+.006f) : new Vector3(-.42f,1.036f,0);
        }
        void Awake() {
            var group=new GameObject("Chopping feedback"); root=group.transform; root.SetParent(transform,false); root.localPosition=BoardTop(transform);
            knife=KitchenArt.ChoppingKnife(root).transform;
            for(int n=0;n<6;n++){whole[n]=KitchenArt.PrepGreens(root,false,n);cut[n]=KitchenArt.PrepGreens(root,true,n);}
            // The animated tool replaces the decorative resting knife. Keep the board itself intact.
            foreach(var name in new[]{"KnifeBlade","KnifeHandle","Knife blade","Knife handle"}) {
                var part=transform.Find(name); if(part) foreach(var r in part.GetComponentsInChildren<Renderer>())r.enabled=false;
            }
            sound=group.AddComponent<AudioSource>(); sound.spatialBlend=1; sound.minDistance=1.5f; sound.maxDistance=12; sound.rolloffMode=AudioRolloffMode.Linear;
            Rest();
        }
        internal void Present(KitchenStation station, KitchenItem item, bool accepted, bool paused, float dt, bool research) {
            bool greens=item!=null&&(item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.ChoppedGreens);
            bool raw=item!=null&&(item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.RawSauce);
            bool same=item!=null&&item.Id==previousItem;
            // A queued event alone is insufficient: the work must have advanced this ingredient since our last tick.
            Active=!paused&&raw&&accepted&&!string.IsNullOrEmpty(station.WorkOwner)&&(same?station.Progress>previousProgress:station.Progress>0);
            Ratio=greens?(item.Kind==KitchenItemKind.ChoppedGreens?1:Mathf.Clamp01(station.Progress/(3*(research?.65f:1)))):0;
            int pieces=Mathf.FloorToInt(Ratio*6);
            bool held=PrepChop.HeldAt(station.InstanceId);   // up close in the chopper's hands instead
            for(int n=0;n<6;n++){whole[n].SetActive(greens&&!held&&n>=pieces);cut[n].SetActive(greens&&!held&&n<pieces);}
            knife.gameObject.SetActive(!held);
            if(Active) {
                float before=phase; phase+=Mathf.Max(0,dt)*3.5f;
                float stroke=phase-Mathf.Floor(phase), lift=Mathf.Sin(stroke*Mathf.PI);
                knife.localPosition=new Vector3(-.025f+Mathf.Sin(phase*.7f)*.035f,.012f+lift*.14f,0);
                knife.localRotation=Quaternion.Euler(0,0,-12+lift*23);
                if(Mathf.FloorToInt(phase)>Mathf.FloorToInt(before)){StrokeCount++;sound.PlayOneShot(SoundFx.Chop,.7f);}
            } else {phase=0;Rest(); if(sound.isPlaying)sound.Stop();}
            previousItem=item?.Id??-1; previousProgress=station.Progress;
        }
        void Rest(){knife.localPosition=new Vector3(.13f,.016f,-.12f);knife.localRotation=Quaternion.Euler(90,0,0);}
    }

    public partial class RestaurantController {
        readonly HashSet<int> acceptedChops=new HashSet<int>();
        void AcceptedChop(int stationId) { acceptedChops.Add(stationId); }
        void LateUpdate() { if(Game&&Game.State!=null){TickChoppingFeedback(Time.deltaTime);TickGrillFeedback(Time.deltaTime);TickWashBinFeedback(Time.deltaTime);} }
        void TickChoppingFeedback(float dt) {
            var events=Game.State.Events;
            // Work can run after RestaurantController.Update. Consume its receipt at the end of this frame,
            // leaving every other service event for GameFeel's usual handler.
            if(events!=null)for(int n=events.Count-1;n>=0;n--)if(events[n].StartsWith("chop:")) {
                if(int.TryParse(events[n].Substring(5),out int id))AcceptedChop(id); events.RemoveAt(n);
            }
            bool paused=Game.Paused||ManagementPauses||PlacementActive;
            foreach(var station in Game.State.Kitchen.Stations)if(station.CatalogId=="prep_bench") {
                var obj=StationObject(station.InstanceId); if(!obj||!obj.activeInHierarchy)continue;
                var view=obj.GetComponent<ChoppingFeedback>(); if(!view)view=obj.AddComponent<ChoppingFeedback>();
                var item=Game.State.Kitchen.At(station.InstanceId);
                view.Present(station,item,acceptedChops.Contains(station.InstanceId),paused,dt,Game.State.FluxResearch);
                if(item!=null&&physicalItems.TryGetValue(item.Id,out var food)&&food) {
                    if(item.Kind==KitchenItemKind.RawGreens||item.Kind==KitchenItemKind.ChoppedGreens)food.SetActive(false);
                    else food.transform.localPosition=ChoppingFeedback.BoardTop(obj.transform);
                }
            }
            acceptedChops.Clear();
        }
    }
}
