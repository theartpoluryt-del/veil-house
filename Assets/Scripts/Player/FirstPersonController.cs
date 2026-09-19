using UnityEngine;

namespace VeilHouse {
 public sealed class FirstPersonController : MonoBehaviour {
  public Camera View; public HauntedObject Focus; public float Sensitivity=1.6f;
  public bool Ghost=>GameSession.I.LocalPlayer!=null&&GameSession.I.LocalPlayer.role==PlayerRole.Ghost;
  public Light Flashlight=>flashlight;
  public int HeldId=-1; public Vector3 Position=>body.transform.position;
  public bool Crouching; public float MoveSpeed;
  GameObject body; CharacterController motor; Light flashlight;
  float yaw,pitch,vertical,sendAt,holdAt,walkPhase,pickupAt; bool wasActive;
  public void Initialize(Camera camera) {
   View=camera;body=new GameObject("Local investigator controller");
   motor=body.AddComponent<CharacterController>();motor.height=1.8f;motor.radius=.27f;motor.center=new Vector3(0,.9f,0);motor.stepOffset=.24f;motor.skinWidth=.035f;
   var beam=new GameObject("Investigator torch");beam.transform.SetParent(View.transform,false);beam.transform.localPosition=new Vector3(.16f,-.13f,.1f);
   flashlight=beam.AddComponent<Light>();flashlight.type=LightType.Spot;flashlight.spotAngle=52;flashlight.innerSpotAngle=24;flashlight.range=15;flashlight.intensity=3.8f;flashlight.color=new Color(.91f,.95f,1);flashlight.shadows=LightShadows.Soft;flashlight.shadowBias=.015f;flashlight.shadowNormalBias=.08f;flashlight.shadowNearPlane=.05f;flashlight.renderMode=LightRenderMode.ForcePixel;flashlight.enabled=false;
  }
  public void Spawn(Vector3 position) {motor.enabled=false;body.transform.position=position;motor.enabled=true;yaw=0;pitch=0;vertical=0;HeldId=-1;}
  public void Teleport(Vector3 position,float facing=0) {motor.enabled=false;body.transform.position=position;motor.enabled=true;yaw=facing;pitch=0;}
  public void ProbeStep(Vector3 destination){motor.enabled=true;Vector3 delta=destination-body.transform.position;delta.y=0;motor.Move(Vector3.ClampMagnitude(delta,1)*Time.deltaTime*4+Vector3.down*Time.deltaTime*3);}
  void Update() {
   var s=GameSession.I; bool active=s.Phase==GamePhase.Investigation;
   bool input=active&&!RuntimeDirector.I.UI.BlocksMovement&&!RuntimeProbe.Running;
   Cursor.lockState=input?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!input;
   if(!active){flashlight.enabled=false;Focus=null;wasActive=false;return;}
   if(!wasActive){wasActive=true;sendAt=0;}
   if(input) {
    yaw+=Input.GetAxisRaw("Mouse X")*Sensitivity;pitch=Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*Sensitivity,-82,82);
    float x=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),z=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);
    Crouching=!Ghost&&(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.C));
    float speed=Ghost?4.2f:Crouching?1.8f:Input.GetKey(KeyCode.LeftShift)?5:3;
    Vector3 move=Quaternion.Euler(0,yaw,0)*Vector3.ClampMagnitude(new Vector3(x,0,z),1)*speed;
    MoveSpeed=move.magnitude;
    motor.enabled=!Ghost;
    if(Ghost){move.y=((Input.GetKey(KeyCode.Space)?1:0)-(Input.GetKey(KeyCode.LeftControl)?1:0))*2.4f;body.transform.position+=move*Time.deltaTime;var p=body.transform.position;p.x=Mathf.Clamp(p.x,-11.5f*HouseLayout.PlanScale,11.5f*HouseLayout.PlanScale);p.z=Mathf.Clamp(p.z,-10.5f*HouseLayout.PlanScale,10.5f*HouseLayout.PlanScale);p.y=Mathf.Clamp(p.y,.12f,1.05f);body.transform.position=p;}
    else {motor.height=Crouching?1.2f:1.8f;motor.center=new Vector3(0,motor.height/2,0);vertical=motor.isGrounded?-2:vertical-18*Time.deltaTime;motor.Move((move+Vector3.up*vertical)*Time.deltaTime);}
    if(Input.GetKeyDown(KeyCode.F)&&!Ghost)flashlight.enabled=!flashlight.enabled;
   } else MoveSpeed=0;
   walkPhase+=Time.deltaTime*MoveSpeed*2.5f;
   float bob=Ghost?Mathf.Sin(Time.time*1.5f)*.014f:Mathf.Sin(walkPhase)*.025f*Mathf.Clamp01(MoveSpeed);
   View.transform.position=body.transform.position+Vector3.up*((Crouching?1.06f:1.65f)+bob);
   View.transform.rotation=Quaternion.Euler(pitch,yaw,Ghost?Mathf.Sin(Time.time*.7f)*.15f:0);
   Focus=null;
   if(Physics.Raycast(View.transform.position,View.transform.forward,out var hit,Ghost?7:3.2f,~0,QueryTriggerInteraction.Ignore)) Focus=hit.collider.GetComponentInParent<HauntedObject>();
   if(input){
    if(Input.GetKeyDown(KeyCode.E)) {
     if(HeldId>=0)SendHeld("release");
     else if(Focus!=null) {
      if(Focus.Kind==HauntKind.Prop&&Ghost){HeldId=Focus.Id;pickupAt=Time.unscaledTime;SendHeld("hold");}
      else if(Focus.Kind!=HauntKind.Prop&&(Ghost||Focus.Kind==HauntKind.Door||Focus.Kind==HauntKind.Cabinet))s.RequestInteraction(Focus.Id,"toggle",hit.point,View.transform.forward);
     }
    }
    if(Ghost) {
     if(HeldId>=0&&(Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.F)))SendHeld("throw");
     if(HeldId>=0&&Input.GetMouseButtonDown(1))SendHeld("release");
     if(Input.GetKeyDown(KeyCode.R)&&Focus!=null)s.RequestInteraction(Focus.Id,"shake",Focus.transform.position,View.transform.forward);
     if(Input.GetKeyDown(KeyCode.Q))s.RequestInteraction(-1,"poltergeist",Position,View.transform.forward);
     if(Input.GetKeyDown(KeyCode.C))s.RequestInteraction(-1,"blackout",Position,View.transform.forward);
    }
   }
   if(HeldId>=0&&Time.unscaledTime-pickupAt>.7f&&(!HauntedObject.All.TryGetValue(HeldId,out var held)||held.Holder!=s.LocalId))HeldId=-1;
   if(HeldId>=0&&Time.unscaledTime>holdAt) {holdAt=Time.unscaledTime+.08f;SendHeld("move");}
   if(Time.unscaledTime>sendAt){sendAt=Time.unscaledTime+.066f;s.SendPose(Position,yaw,pitch,Crouching,MoveSpeed);}
  }
  void SendHeld(string action) {
   Vector3 target=View.transform.position+View.transform.forward*2.15f;target.y=Mathf.Clamp(target.y,.3f,2.65f);
   GameSession.I.RequestInteraction(HeldId,action,target,View.transform.forward);

  }
 }
}

