using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse
{
    /// <summary>Keeps active household appliances audible on host and clients using replicated state.
    /// Polling is allocation-free; one-shot voices remain owned by Soundscape's bounded pool.</summary>
    public sealed class ApplianceAmbience : MonoBehaviour
    {
        readonly Dictionary<int,float> nextCue=new Dictionary<int,float>();
        float nextPoll;
        void Update()
        {
            float now=Time.time;
            if(now<nextPoll)return;
            nextPoll=now+.16f;
            foreach(var pair in HauntedObject.All) {
                HauntedObject appliance=pair.Value;
                if(appliance==null)continue;
                bool supported=appliance.Kind==HauntKind.Television||appliance.Kind==HauntKind.Radio||appliance.Kind==HauntKind.Water;
                if(!supported)continue;
                if(!appliance.Active) {nextCue.Remove(pair.Key);continue;}
                if(!nextCue.TryGetValue(pair.Key,out float next)) {nextCue[pair.Key]=now+.55f;continue;}
                if(now<next)continue;
                string cue=appliance.Kind==HauntKind.Television?"tv_loop":appliance.Kind==HauntKind.Radio?"radio_loop":"water_loop";
                Soundscape.PlayAt(appliance.transform.position,cue);
                nextCue[pair.Key]=now+(appliance.Kind==HauntKind.Radio?2.55f:2.23f);
            }
        }
    }
}
