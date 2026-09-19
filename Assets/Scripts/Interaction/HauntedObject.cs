using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse {
    /// <summary>Only the host advances physics or changes an interaction. Clients interpolate snapshots.</summary>
    public sealed class HauntedObject : MonoBehaviour {
        public static readonly Dictionary<int,HauntedObject> All = new Dictionary<int,HauntedObject>();
        public static Action<Vector3,string> Sound;
        public int Id {get;private set;}
        public string DisplayName;
        public HauntKind Kind;
        public float Cost=2;
        public Rigidbody Body;
        public bool Active;
        public int Holder=-1;
        public Transform Hinge;
        public Light[] Lamps;
        public Renderer[] EmissiveParts;
        public GameObject ActiveVisual;
        public float OpenAngle=94;
        bool authority=true,received;
        Vector3 targetPosition,holdPosition,homePosition;
        Quaternion targetRotation,homeRotation;
        float shakeUntil,knockUntil,phase,shutUntil;
        bool lastVisual;
        MaterialPropertyBlock visualBlock;
        static int nextId;

        public static void ResetRegistry() {All.Clear();nextId=0;}
        public static float Validate(InteractionRequest r) {
            HauntedObject obj;
            if(r==null||!All.TryGetValue(r.objectId,out obj)||GameSession.I==null)return -1;
            PlayerState player=GameSession.I.Players.Find(p=>p.id==r.playerId);
            if(player==null)return -1;
            bool ghost=player.role==PlayerRole.Ghost;
            if(r.action=="release")return ghost&&obj.Holder==r.playerId?0:-1;
            float reach=ghost?7:3.2f;
            if((obj.transform.position-player.Position).sqrMagnitude>reach*reach)return -1;
            bool door=obj.Kind==HauntKind.Door||obj.Kind==HauntKind.Cabinet;
            if(!ghost&&(!door||(r.action!="toggle"&&r.action!="open"&&r.action!="close")))return -1;
            if(r.action=="toggle"||r.action=="open"||r.action=="close")return obj.Kind==HauntKind.Prop?-1:ghost?2:0;
            if(!ghost)return -1;
            if(r.action=="knock")return 1;
            if(obj.Kind!=HauntKind.Prop)return -1;
            if(r.action=="shake")return 3;
            if(r.action=="hold") {
                if(obj.Holder>=0&&obj.Holder!=r.playerId)return -1;
                if((r.Target-player.Position).sqrMagnitude>25||r.Target.y<.15f||r.Target.y>3.2f)return -1;
                return 2;
            }
            if(r.action=="move")return obj.Holder==r.playerId&&(r.Target-player.Position).sqrMagnitude<25&&r.Target.y>.15f&&r.Target.y<3.2f?0:-1;
            if(r.action=="release"||r.action=="throw")return obj.Holder==r.playerId?(r.action=="throw"?5:0):-1;
            return -1;
        }
        public HauntedObject Initialize(string label,HauntKind kind,bool active=false) {
            Id=++nextId;DisplayName=label;Kind=kind;Active=active;All[Id]=this;
            Body=GetComponent<Rigidbody>();homePosition=transform.position;homeRotation=transform.rotation;
            targetPosition=homePosition;targetRotation=homeRotation;phase=Id*1.739f;
            return this;
        }
        public void ResetHomePose(){homePosition=transform.position;homeRotation=transform.rotation;targetPosition=homePosition;targetRotation=homeRotation;}
        public void SetAuthority(bool value) {
            authority=value;
            if(Body) {Body.isKinematic=!value || Holder>=0;Body.interpolation=value?RigidbodyInterpolation.Interpolate:RigidbodyInterpolation.None;}
        }
        public ObjectState Capture() {return new ObjectState{id=Id,Position=transform.position,Rotation=transform.rotation,active=Active,holder=Holder};}
        public void Apply(ObjectState state) {
            if(authority)return;
            targetPosition=state.Position;targetRotation=state.Rotation;Active=state.active;Holder=state.holder;
            if(!received) {transform.SetPositionAndRotation(targetPosition,targetRotation);received=true;}
        }
        public void ServerAct(InteractionRequest r) {
            if(!authority)return;
            switch(r.action) {
                case "toggle": case "open": case "close":
                    Active=r.action=="open"||r.action=="toggle"&&!Active;
                    Emit(Kind==HauntKind.Door||Kind==HauntKind.Cabinet?"door":Kind==HauntKind.Light?"switch":Kind.ToString().ToLowerInvariant());break;
                case "hold":
                    foreach(var other in All.Values)if(other!=this&&other.Holder==r.playerId)other.ReleasePlayer(r.playerId);
                    Holder=r.playerId;holdPosition=r.Target;
                    if(Body) {Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;Body.isKinematic=true;}
                    Emit("pickup");break;
                case "move": if(Holder==r.playerId)holdPosition=r.Target;break;
                case "release":
                    Holder=-1;if(Body)Body.isKinematic=false;break;
                case "throw":
                    Holder=-1;
                    if(Body) {Body.isKinematic=false;Body.linearVelocity=Vector3.ClampMagnitude(r.Direction,1)*8+Vector3.up*1.5f;Body.angularVelocity=new Vector3(3,2,4);}
                    Emit("throw");break;
                case "shake":
                    shakeUntil=Time.time+1.4f;
                    if(Body&&!Body.isKinematic)Body.AddForce(Vector3.up*2.4f,ForceMode.VelocityChange);
                    Emit("rattle");break;
                case "knock": knockUntil=Time.time+.4f;Emit("knock");break;
            }
            RefreshVisuals();
        }
        public void ForceImpulse(Vector3 origin,float strength) {
            if(!authority||!Body||Kind!=HauntKind.Prop)return;
            Holder=-1;Body.isKinematic=false;
            Vector3 direction=(transform.position-origin).normalized+Vector3.up*.85f;
            Body.AddForce(direction*strength,ForceMode.VelocityChange);Body.AddTorque(new Vector3(1,.7f,-.4f)*strength,ForceMode.VelocityChange);
            Emit("rattle");
        }
        public void SuppressLight(float duration) {shutUntil=Time.time+duration;}
        public void ReleasePlayer(int id) {if(Holder==id){Holder=-1;if(Body&&authority)Body.isKinematic=false;}}
        void Emit(string cue) {if(Sound!=null)Sound(transform.position,cue);}
        void OnDestroy() {if(All.ContainsKey(Id)&&All[Id]==this)All.Remove(Id);}
        void FixedUpdate() {
            if(!authority||!Body)return;
            if(Holder>=0) {
                Body.MovePosition(Vector3.Lerp(Body.position,holdPosition,Time.fixedDeltaTime*13));
                Body.MoveRotation(Quaternion.Slerp(Body.rotation,Quaternion.Euler(Mathf.Sin(Time.time*1.5f+phase)*4,phase*15,0),Time.fixedDeltaTime*2));
            } else if(Time.time<shakeUntil) {
                Body.AddForce(new Vector3(Mathf.Sin(Time.time*35+phase),.15f,Mathf.Cos(Time.time*31+phase))*22,ForceMode.Acceleration);
                Body.AddTorque(new Vector3(1,0,.7f)*Mathf.Sin(Time.time*33)*14,ForceMode.Acceleration);
            }
            if(Body.position.y<-.8f||Mathf.Abs(Body.position.x)>17||Mathf.Abs(Body.position.z)>16) {
                Body.position=homePosition;Body.rotation=homeRotation;Body.linearVelocity=Vector3.zero;Body.angularVelocity=Vector3.zero;
            }
        }
        void Update() {
            if(!authority&&received) {
                transform.position=Vector3.Lerp(transform.position,targetPosition,Time.deltaTime*15);
                transform.rotation=Quaternion.Slerp(transform.rotation,targetRotation,Time.deltaTime*15);
            }
            if(Hinge)Hinge.localRotation=Quaternion.Slerp(Hinge.localRotation,Quaternion.Euler(0,Active?OpenAngle:0,0),Time.deltaTime*5.5f);
            if(Lamps!=null) {
                for(int i=0;i<Lamps.Length;i++)if(Lamps[i]) {
                    Lamps[i].enabled=Active&&Time.time>=shutUntil;
                    if(Kind==HauntKind.Television)Lamps[i].intensity=.7f+.18f*Mathf.Sin(Time.time*22+phase);
                }
            }
            if(lastVisual!=Active)RefreshVisuals();
            if(ActiveVisual) {
                if(Kind==HauntKind.Water)ActiveVisual.transform.localScale=new Vector3(.027f,.16f+Mathf.Sin(Time.time*25)*.01f,.027f);
                else if(Kind==HauntKind.Television) {
                    var rend=ActiveVisual.GetComponent<Renderer>();
                    if(rend&&rend.sharedMaterial) {if(visualBlock==null)visualBlock=new MaterialPropertyBlock();rend.GetPropertyBlock(visualBlock);visualBlock.SetVector("_MainTex_ST",new Vector4(1,1,0,Time.time*2));rend.SetPropertyBlock(visualBlock);}
                }
            }
        }
        void RefreshVisuals() {
            lastVisual=Active;
            if(ActiveVisual)ActiveVisual.SetActive(Active);
            if(EmissiveParts!=null)foreach(var r in EmissiveParts)if(r) {
                if(visualBlock==null)visualBlock=new MaterialPropertyBlock();r.GetPropertyBlock(visualBlock);visualBlock.SetColor("_EmissionColor",Active?new Color(1,.72f,.40f)*2.8f:Color.black);r.SetPropertyBlock(visualBlock);
            }
        }
        void OnCollisionEnter(Collision c) {if(authority&&Time.time>1&&c.relativeVelocity.sqrMagnitude>2.5f)Emit("impact");}
    }

    public static class WorldEffects {
        public static void Blackout(float seconds=12) {foreach(var obj in HauntedObject.All.Values)if(obj.Kind==HauntKind.Light||obj.Kind==HauntKind.Television)obj.SuppressLight(seconds);}
        public static void Poltergeist(Vector3 at,float radius=8) {foreach(var obj in HauntedObject.All.Values)if((obj.transform.position-at).sqrMagnitude<radius*radius)obj.ForceImpulse(at,3.2f);}
    }
}
