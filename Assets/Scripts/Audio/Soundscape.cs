using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse
{
    /// <summary>Original sample-synthesized ambience and spatial cues; no downloaded recordings.
    /// Transient voices are pooled and gently limited. A listener belongs to the local player camera.</summary>
    public sealed class Soundscape : MonoBehaviour
    {
        static Soundscape instance;
        readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        readonly AudioSource[] voices=new AudioSource[28];
        AudioSource roomTone, rain, unease;
        float haunting, surge, nextCreak, lastRattle=-100;
        int cursor;
        public static Soundscape Create()
        {
            if(instance!=null)return instance;
            var go=new GameObject("VEIL HOUSE / original soundscape");
            instance=go.AddComponent<Soundscape>();
            instance.Initialize();
            go.AddComponent<ApplianceAmbience>();
            return instance;
        }
        void Initialize()
        {
            var synth=new SoundSynthesis();
            clips["room"]=synth.Generate("Room tone / distant house",12,SoundSynthesis.Kind.Room,true);
            clips["rain"]=synth.Generate("Rain beyond the windows",10,SoundSynthesis.Kind.Rain,true);
            clips["unease"]=synth.Generate("Unsettled foundation",12,SoundSynthesis.Kind.Unease,true);
            clips["footstep"]=synth.Generate("Leather heel on old timber",.22f,SoundSynthesis.Kind.Footstep);
            clips["door"]=synth.Generate("Door hinge and latch",1.12f,SoundSynthesis.Kind.Door);
            clips["creak"]=synth.Generate("House settling",1.9f,SoundSynthesis.Kind.Creak);
            clips["thud"]=synth.Generate("Falling household object",.35f,SoundSynthesis.Kind.Thud);
            clips["throw"]=synth.Generate("Object thrown through the air",.48f,SoundSynthesis.Kind.Throw);
            clips["rattle"]=synth.Generate("Glass and wood tremor",.82f,SoundSynthesis.Kind.Rattle);
            clips["light"]=synth.Generate("Old electrical switch",.44f,SoundSynthesis.Kind.Light);
            clips["tv"]=synth.Generate("Television interference",2.0f,SoundSynthesis.Kind.Television);
            clips["radio"]=synth.Generate("Radio between stations",2.5f,SoundSynthesis.Kind.Radio);
            clips["water"]=synth.Generate("Tap and running basin",2.1f,SoundSynthesis.Kind.Water);
            clips["pulse"]=synth.Generate("Paranormal pressure wave",2.0f,SoundSynthesis.Kind.Pulse);
            clips["knock"]=synth.Generate("Three knocks",1.4f,SoundSynthesis.Kind.Knock);
            clips["blackout"]=synth.Generate("Power surge",1.7f,SoundSynthesis.Kind.Blackout);
            clips["ui"]=synth.Generate("Journal paper and soft chime",.32f,SoundSynthesis.Kind.UI);
            clips["reveal"]=synth.Generate("The truth / resolving bell",3.2f,SoundSynthesis.Kind.Reveal);
            for(int i=0;i<voices.Length;i++) {
                var child=new GameObject("Spatial voice "+(i+1));child.transform.SetParent(transform,false);
                voices[i]=child.AddComponent<AudioSource>();
                voices[i].playOnAwake=false;voices[i].spatialBlend=1;voices[i].dopplerLevel=0;
                voices[i].rolloffMode=AudioRolloffMode.Logarithmic;voices[i].minDistance=1.7f;voices[i].maxDistance=23;
                voices[i].reverbZoneMix=.18f;voices[i].priority=100;
            }
            roomTone=Loop("House air",clips["room"],.23f);
            rain=Loop("Weather beyond the walls",clips["rain"],.11f);
            unease=Loop("Low paranormal presence",clips["unease"],.012f);
            nextCreak=Time.time+UnityEngine.Random.Range(12,22);
        }
        AudioSource Loop(string name,AudioClip clip,float volume)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>();source.clip=clip;source.loop=true;source.playOnAwake=false;
            source.spatialBlend=0;source.volume=volume;source.priority=210;source.Play();return source;
        }
        public void SetHaunting(float intensity) {haunting=Mathf.Clamp01(intensity);}
        public static void PlayAt(Vector3 position,string cue)
        {
            if(instance==null || string.IsNullOrEmpty(cue))return;
            instance.Play(position,cue);
        }
        void Play(Vector3 position,string cue)
        {
            string key=cue.ToLowerInvariant();float gain=.6f;bool screen=false;
            bool applianceLoop=key.EndsWith("_loop",StringComparison.Ordinal);
            if(applianceLoop)key=key.Substring(0,key.Length-5);
            switch(key) {
                case "step":case "walk":case "footsteps":key="footstep";break;
                case "step_soft":key="footstep";gain=.23f;break;
                case "door_open":case "door_close":case "cabinet":case "open":case "close":key="door";break;
                case "drop":case "impact":case "hit":case "fall":key="thud";break;
                case "lift":case "move":case "grab":case "pickup":key="throw";gain=.35f;break;
                case "shake":case "tremor":key="rattle";break;
                case "flicker":case "switch":case "toggle":key="light";break;
                case "television":case "static":key="tv";break;
                case "tap":case "faucet":key="water";break;
                case "ability":case "power":case "shockwave":case "poltergeist":case "scream":key="pulse";break;
                case "whisper":key="radio";gain=.37f;break;
                case "lightsout":key="blackout";break;
                case "click":case "journal":case "join":case "start":case "select":case "menu":key="ui";screen=true;break;
                case "finish":case "results":key="reveal";screen=true;break;
            }
            if(key=="footstep" && gain>.3f)gain=.38f;
            if(key=="creak")gain=.30f;
            if(key=="ui") {screen=true;gain=.28f;}
            if(key=="reveal") {screen=true;gain=.46f;}
            if(key=="tv"||key=="radio"||key=="water")gain=applianceLoop?.24f:.43f;
            if(key=="pulse"||key=="blackout") {gain=.69f;surge=1;}
            // A room-wide force may affect dozens of objects on the same frame. One nearby
            // rattle plus the pressure cue conveys that event without summing identical attacks.
            if(key=="rattle") {
                if(Time.time-lastRattle<.065f)return;
                lastRattle=Time.time;
            }
            if(!clips.TryGetValue(key,out AudioClip clip))clip=clips["thud"];
            AudioSource source=null;
            for(int i=0;i<voices.Length;i++) {
                int index=(cursor+i)%voices.Length;
                if(!voices[index].isPlaying) {source=voices[index];cursor=(index+1)%voices.Length;break;}
            }
            if(source==null) {source=voices[cursor];cursor=(cursor+1)%voices.Length;source.Stop();}
            source.transform.position=position;source.spatialBlend=screen?0:1;
            source.minDistance=key=="pulse"?3:1.7f;source.maxDistance=key=="pulse"?32:23;
            source.volume=gain;source.pitch=screen?1:UnityEngine.Random.Range(.94f,1.065f);
            source.clip=clip;source.Play();
        }
        void Update()
        {
            surge=Mathf.MoveTowards(surge,0,Time.deltaTime*.13f);
            if(unease!=null)unease.volume=Mathf.Lerp(unease.volume,.012f+haunting*.14f+surge*.09f,Time.deltaTime*1.5f);
            if(Time.time<nextCreak)return;
            nextCreak=Time.time+UnityEngine.Random.Range(17,31);
            var listener=FindFirstObjectByType<AudioListener>();
            if(listener==null)return;
            var point=listener.transform.position+new Vector3(UnityEngine.Random.Range(-9,9),UnityEngine.Random.Range(0,2),UnityEngine.Random.Range(-9,9));
            Play(point,"creak");
        }
        void OnDestroy()
        {
            if(instance==this)instance=null;
            foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);
            clips.Clear();
        }
    }
}


