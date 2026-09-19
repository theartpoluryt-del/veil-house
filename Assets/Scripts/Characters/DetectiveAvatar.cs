using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse
{
    /// <summary>Original low-poly detective, generated as a genuinely skinned, humanoid-mapped mesh.
    /// The bind pose is a T pose; local bone animation supplies idle/walk/run/crouch/reach.</summary>
    public sealed class DetectiveAvatar : MonoBehaviour
    {
        public bool HasHumanoidRig { get; private set; }
        public Animator Animator { get; private set; }
        Transform hips, spine, chest, neck, head, leftArm, rightArm, leftForearm, rightForearm;
        Transform leftThigh, rightThigh, leftCalf, rightCalf, leftFoot, rightFoot;
        SkinnedMeshRenderer body;
        Mesh ownedMesh;
        readonly List<Transform> bones = new List<Transform>();
        readonly Dictionary<string, Transform> named = new Dictionary<string, Transform>();
        readonly Dictionary<Transform, Vector3> bindPositions = new Dictionary<Transform, Vector3>();
        Material[] ownedMaterials;
        Avatar humanoidAvatar;
        float cycle, smoothedSpeed, crouchAmount, reachAmount;
        int lastStepIndex=-1;
        bool hasPose;
        int appearanceIndex;

        public static DetectiveAvatar Create(Transform parent, int appearance)
        {
            var go = new GameObject("Detective " + appearance);
            go.transform.SetParent(parent, false);
            var avatar = go.AddComponent<DetectiveAvatar>();
            avatar.Build(appearance);
            return avatar;
        }

        Transform Bone(string name, Transform parent, Vector3 position)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = transform.TransformPoint(position);
            t.rotation = transform.rotation;
            bones.Add(t); named.Add(name, t); bindPositions[t] = t.localPosition;
            return t;
        }
        int Index(Transform t) { return bones.IndexOf(t); }
        int Index(string name) { return Index(named[name]); }

        void Build(int appearance)
        {
            appearanceIndex = Mathf.Abs(appearance) % 6;
            hips = Bone("Hips", transform, new Vector3(0, .89f, 0));
            spine = Bone("Spine", hips, new Vector3(0, 1.07f, 0));
            chest = Bone("Chest", spine, new Vector3(0, 1.28f, 0));
            var upperChest = Bone("UpperChest", chest, new Vector3(0, 1.39f, 0));
            neck = Bone("Neck", upperChest, new Vector3(0, 1.49f, 0));
            head = Bone("Head", neck, new Vector3(0, 1.59f, 0));
            var ls = Bone("LeftShoulder", upperChest, new Vector3(-.13f, 1.40f, 0));
            var rs = Bone("RightShoulder", upperChest, new Vector3(.13f, 1.40f, 0));
            leftArm = Bone("LeftUpperArm", ls, new Vector3(-.245f, 1.40f, 0));
            rightArm = Bone("RightUpperArm", rs, new Vector3(.245f, 1.40f, 0));
            leftForearm = Bone("LeftLowerArm", leftArm, new Vector3(-.55f, 1.40f, 0));
            rightForearm = Bone("RightLowerArm", rightArm, new Vector3(.55f, 1.40f, 0));
            var lh = Bone("LeftHand", leftForearm, new Vector3(-.79f, 1.40f, 0));
            var rh = Bone("RightHand", rightForearm, new Vector3(.79f, 1.40f, 0));
            leftThigh = Bone("LeftUpperLeg", hips, new Vector3(-.115f, .86f, 0));
            rightThigh = Bone("RightUpperLeg", hips, new Vector3(.115f, .86f, 0));
            leftCalf = Bone("LeftLowerLeg", leftThigh, new Vector3(-.115f, .47f, 0));
            rightCalf = Bone("RightLowerLeg", rightThigh, new Vector3(.115f, .47f, 0));
            leftFoot = Bone("LeftFoot", leftCalf, new Vector3(-.115f, .085f, .015f));
            rightFoot = Bone("RightFoot", rightCalf, new Vector3(.115f, .085f, .015f));
            Bone("LeftToes", leftFoot, new Vector3(-.115f, .035f, .18f));
            Bone("RightToes", rightFoot, new Vector3(.115f, .035f, .18f));
            BuildMaterials(appearanceIndex);
            var mesh = new DetectiveMesh(ownedMaterials.Length);
            BuildTorso(mesh); BuildLimbs(mesh); BuildFace(mesh, appearanceIndex); BuildAccessories(mesh, appearanceIndex);
            body = gameObject.AddComponent<SkinnedMeshRenderer>();
            ownedMesh = mesh.Create(bones, transform);
            body.sharedMesh = ownedMesh;
            body.bones = bones.ToArray(); body.rootBone = hips; body.sharedMaterials = ownedMaterials;
            body.localBounds = new Bounds(new Vector3(0, .92f, 0), new Vector3(2.2f, 2.4f, 1.2f));
            body.updateWhenOffscreen = true;
            body.quality = SkinQuality.Bone2;
            BuildHumanoid();
            Animate(0, false, false);
        }

        void BuildMaterials(int appearance)
        {
            Color[] coats = { new Color(.18f,.23f,.22f), new Color(.28f,.20f,.15f), new Color(.18f,.22f,.30f), new Color(.30f,.26f,.19f), new Color(.24f,.17f,.22f), new Color(.15f,.24f,.27f) };
            Color[] skins = { new Color(.73f,.51f,.36f), new Color(.50f,.31f,.20f), new Color(.85f,.65f,.49f), new Color(.37f,.22f,.15f), new Color(.68f,.44f,.29f), new Color(.81f,.60f,.43f) };
            ownedMaterials = new[] {
                Mat("Wool trench", coats[appearance], .10f),
                Mat("Lapels and cuffs", coats[appearance] * .72f, .10f),
                Mat("Cotton and eye whites", new Color(.68f,.66f,.56f), .08f),
                Mat("Trousers", new Color(.075f,.087f,.095f), .04f),
                Mat("Waxed leather", new Color(.063f,.041f,.028f), .27f),
                Mat("Skin", skins[appearance], .06f),
                Mat("Hair and eyes", new Color(.043f,.03f,.023f), .12f),
                Mat("Aged brass", new Color(.64f,.44f,.19f), .35f, .52f),
                Mat("Wine scarf", new Color(.32f,.068f,.075f), .10f),
                Mat("Lip and face shadow", Color.Lerp(skins[appearance], new Color(.21f,.065f,.041f), .4f), .04f)
            };
        }
        static Material Mat(string name, Color color, float gloss, float metallic = 0)
        {
            var result = new Material(Shader.Find("Standard"));
            result.name = name; result.color = color;
            result.SetFloat("_Glossiness", gloss); result.SetFloat("_Metallic", metallic);
            return result;
        }

        void BuildTorso(DetectiveMesh m)
        {
            int h = Index(hips), s = Index(spine), c = Index(chest);
            // Irregular elliptical sections create shoulders, tailored waist and the long coat skirt.
            m.Vertical(new[] {
                new DetectiveMesh.Section(.65f, .245f, .155f, h),
                new DetectiveMesh.Section(.77f, .257f, .163f, h),
                new DetectiveMesh.Section(.96f, .211f, .132f, h),
                new DetectiveMesh.Section(1.13f, .207f, .130f, s),
                new DetectiveMesh.Section(1.30f, .237f, .146f, c),
                new DetectiveMesh.Section(1.40f, .259f, .126f, c),
                new DetectiveMesh.Section(1.46f, .115f, .092f, c)
            }, Vector3.zero, 12, 0);
            // The front panels, seams and asymmetric overlapping lapels are authored polygons.
            m.Polygon(new[] { V(-.071f,1.445f,.097f),V(.071f,1.445f,.097f),V(.061f,1.19f,.135f),V(-.062f,1.19f,.135f) }, c, 2);
            m.Polygon(new[] { V(-.105f,1.47f,.09f),V(-.215f,1.38f,.112f),V(-.091f,1.245f,.158f),V(-.031f,1.30f,.16f) }, c, 1);
            m.Polygon(new[] { V(.105f,1.47f,.09f),V(.215f,1.38f,.112f),V(.071f,1.22f,.158f),V(.011f,1.29f,.16f) }, c, 1);
            m.Box(V(0,1.01f,.018f), new Vector3(.431f,.045f,.278f), h, 4);
            m.Box(V(.028f,1.012f,.165f), new Vector3(.061f,.053f,.015f), h, 7);
            m.Box(V(.028f,1.012f,.175f), new Vector3(.036f,.031f,.008f), h, 4);
            for (int side = -1; side <= 1; side += 2) {
                m.Box(V(side*.16f,.855f,.148f), new Vector3(.105f,.019f,.019f), h, 1);
                m.Box(V(side*.155f,.812f,.148f), new Vector3(.105f,.075f,.011f), h, 0);
                for (int i = 0; i < 3; i++) m.Ellipsoid(V(side*.071f,1.19f-i*.076f,.150f), new Vector3(.010f,.010f,.0045f), s, 7, 8, 4);
            }
            // Neck, collar and loosely hanging scarf.
            m.Vertical(new[] {new DetectiveMesh.Section(1.445f,.055f,.055f,Index(neck)),new DetectiveMesh.Section(1.54f,.052f,.05f,Index(neck))},V(0,0,0),10,5);
            m.Vertical(new[] {new DetectiveMesh.Section(1.448f,.091f,.085f,c),new DetectiveMesh.Section(1.494f,.069f,.066f,c)},Vector3.zero,10,8);
            m.Polygon(new[] {V(-.014f,1.472f,.086f),V(.045f,1.47f,.086f),V(.022f,1.195f,.157f),V(-.045f,1.21f,.157f)}, c, 8);
        }

        void BuildLimbs(DetectiveMesh m)
        {
            foreach (int side in new[] {-1,1}) {
                string prefix = side < 0 ? "Left" : "Right";
                int upper = Index(prefix + "UpperArm"), lower = Index(prefix + "LowerArm"), hand = Index(prefix+"Hand");
                m.Tube(new[] { V(side*.23f,1.40f,0),V(side*.30f,1.40f,0),V(side*.50f,1.40f,0),V(side*.55f,1.40f,0) },new[]{.104f,.10f,.077f,.073f},new[]{upper,upper,upper,lower},10,0);
                m.Tube(new[] { V(side*.55f,1.40f,0),V(side*.70f,1.40f,0),V(side*.787f,1.40f,0) },new[]{.074f,.068f,.055f},new[]{lower,lower,lower},10,0);
                m.Tube(new[] {V(side*.753f,1.40f,0),V(side*.796f,1.40f,0)},new[]{.059f,.058f},new[]{lower,lower},10,1);
                m.Ellipsoid(V(side*.848f,1.399f,.005f),new Vector3(.067f,.041f,.048f),hand,5,8,5);
                m.Ellipsoid(V(side*.817f,1.375f,.044f),new Vector3(.033f,.026f,.025f),hand,5,7,4);
                // Fingertip separations are subtle, modeled ridges rather than floating cylinders.
                for (int finger=0;finger<3;finger++) m.Box(V(side*.889f,1.389f,.026f-finger*.022f),new Vector3(.002f,.025f,.004f),hand,9);
                int thigh = Index(prefix+"UpperLeg"), calf = Index(prefix+"LowerLeg"), foot = Index(prefix+"Foot");
                float x=side*.115f;
                m.Vertical(new[] {new DetectiveMesh.Section(.46f,.079f,.081f,calf),new DetectiveMesh.Section(.57f,.086f,.087f,thigh),new DetectiveMesh.Section(.83f,.092f,.094f,thigh),new DetectiveMesh.Section(.89f,.091f,.092f,thigh)},V(x,0,0),10,3);
                m.Vertical(new[] {new DetectiveMesh.Section(.16f,.061f,.066f,calf),new DetectiveMesh.Section(.30f,.067f,.073f,calf),new DetectiveMesh.Section(.46f,.079f,.081f,calf)},V(x,0,0),10,3);
                m.Vertical(new[] {new DetectiveMesh.Section(.07f,.068f,.084f,foot),new DetectiveMesh.Section(.14f,.069f,.068f,calf),new DetectiveMesh.Section(.245f,.065f,.066f,calf)},V(x,0,0),10,4);
                m.Ellipsoid(V(x,.075f,.095f),new Vector3(.079f,.062f,.155f),foot,4,10,5);
                m.Box(V(x,.024f,.082f),new Vector3(.149f,.037f,.282f),foot,4);
                m.Box(V(x,.025f,-.031f),new Vector3(.13f,.046f,.095f),foot,1);
                for(int lace=0;lace<3;lace++) m.Box(V(x,.139f,.038f+lace*.022f),new Vector3(.049f,.007f,.007f),foot,1);
            }
        }

        void BuildFace(DetectiveMesh m,int appearance)
        {
            int b=Index(head);
            // A shaped jaw / cheek / temple / brow profile; it is intentionally not a sphere head.
            m.Vertical(new[] {
                new DetectiveMesh.Section(1.479f,.043f,.046f,b),
                new DetectiveMesh.Section(1.500f,.069f,.067f,b),
                new DetectiveMesh.Section(1.542f,.084f,.077f,b),
                new DetectiveMesh.Section(1.592f,.092f,.081f,b),
                new DetectiveMesh.Section(1.648f,.088f,.079f,b),
                new DetectiveMesh.Section(1.695f,.085f,.076f,b),
                new DetectiveMesh.Section(1.737f,.061f,.057f,b),
                new DetectiveMesh.Section(1.750f,.026f,.022f,b)
            },V(0,0,.012f),12,5);
            // Ears, sculpted projecting bridge, nostrils and lower lip.
            m.Ellipsoid(V(-.094f,1.583f,.005f),new Vector3(.018f,.032f,.022f),b,5,7,4);
            m.Ellipsoid(V(.094f,1.583f,.005f),new Vector3(.018f,.032f,.022f),b,5,7,4);
            m.Polygon(new[]{V(-.014f,1.633f,.091f),V(.014f,1.633f,.091f),V(.020f,1.572f,.12f),V(-.020f,1.572f,.12f)},b,5);
            m.Polygon(new[]{V(-.020f,1.572f,.12f),V(.020f,1.572f,.12f),V(.015f,1.563f,.09f),V(-.015f,1.563f,.09f)},b,9);
            m.Polygon(new[]{V(-.014f,1.633f,.091f),V(-.020f,1.572f,.12f),V(-.027f,1.571f,.077f)},b,5);
            m.Polygon(new[]{V(.014f,1.633f,.091f),V(.027f,1.571f,.077f),V(.020f,1.572f,.12f)},b,5);
            m.Box(V(0,1.535f,.087f),new Vector3(.041f,.006f,.008f),b,9);
            for(int side=-1;side<=1;side+=2) {
                m.Ellipsoid(V(side*.04f,1.616f,.079f),new Vector3(.027f,.012f,.013f),b,9,8,4);
                m.Ellipsoid(V(side*.04f,1.619f,.090f),new Vector3(.022f,.007f,.004f),b,2,8,4);
                m.Ellipsoid(V(side*.037f,1.619f,.094f),new Vector3(.006f,.007f,.003f),b,6,7,4);
                m.Box(V(side*.04f,1.639f,.088f),new Vector3(.048f,.009f,.008f),b,6);
                m.Box(V(side*.081f,1.627f,-.001f),new Vector3(.013f,.079f,.074f),b,6);
            }
            if(appearance%2==1) {
                m.Polygon(new[]{V(-.05f,1.551f,.085f),V(.05f,1.551f,.085f),V(.035f,1.536f,.09f),V(-.035f,1.536f,.09f)},b,6);
                m.Polygon(new[]{V(-.065f,1.527f,.067f),V(0,1.505f,.084f),V(.065f,1.527f,.067f),V(.037f,1.488f,.064f),V(-.037f,1.488f,.064f)},b,6);
            }
            m.Vertical(new[]{new DetectiveMesh.Section(1.682f,.088f,.079f,b),new DetectiveMesh.Section(1.731f,.071f,.063f,b),new DetectiveMesh.Section(1.757f,.026f,.026f,b)},V(0,0,.003f),12,6);
        }

        void BuildAccessories(DetectiveMesh m,int appearance)
        {
            int h=Index(head),c=Index(chest),hip=Index(hips);
            // Fedora has a raised crown, dented top and a wide modeled brim.
            m.Vertical(new[]{new DetectiveMesh.Section(1.718f,.161f,.137f,h),new DetectiveMesh.Section(1.731f,.157f,.134f,h)},V(0,0,.005f),16,1);
            m.Vertical(new[]{new DetectiveMesh.Section(1.731f,.106f,.094f,h),new DetectiveMesh.Section(1.759f,.102f,.09f,h),new DetectiveMesh.Section(1.846f,.086f,.071f,h),new DetectiveMesh.Section(1.858f,.074f,.061f,h)},V(0,0,-.003f),12,0);
            m.Vertical(new[]{new DetectiveMesh.Section(1.734f,.108f,.096f,h),new DetectiveMesh.Section(1.761f,.104f,.093f,h)},V(0,0,-.003f),12,4);
            m.Box(V(0,1.857f,-.004f),new Vector3(.035f,.006f,.09f),h,1);
            // Shoulder tabs and narrow bag strap, badge, notebook satchel.
            for(int side=-1;side<=1;side+=2) {
                m.Box(V(side*.185f,1.447f,0),new Vector3(.092f,.011f,.045f),c,1);
                m.Ellipsoid(V(side*.157f,1.456f,0),new Vector3(.01f,.004f,.01f),c,7,7,4);
            }
            m.Polygon(new[]{V(.168f,1.404f,.13f),V(.196f,1.399f,.135f),V(-.145f,1.047f,.15f),V(-.178f,1.049f,.15f)},c,4);
            m.Box(V(-.253f,.846f,.044f),new Vector3(.09f,.225f,.185f),hip,4);
            m.Box(V(-.26f,.924f,.054f),new Vector3(.098f,.079f,.191f),hip,1);
            m.Box(V(-.264f,.845f,.145f),new Vector3(.035f,.048f,.012f),hip,7);
            m.Ellipsoid(V(-.125f,1.285f,.155f),new Vector3(.025f,.031f,.005f),c,7,6,4);
            m.Ellipsoid(V(-.125f,1.285f,.162f),new Vector3(.011f,.014f,.003f),c,1,6,4);
        }

        void BuildHumanoid()
        {
            var human=new List<HumanBone>();
            foreach(var entry in named) {
                string humanName=null;
                // Use the running editor's canonical names, avoiding display-name assumptions.
                foreach(string candidate in HumanTrait.BoneName) {
                    if(string.Equals(candidate.Replace(" ",""),entry.Key,StringComparison.OrdinalIgnoreCase)) {
                        humanName=candidate;break;
                    }
                }
                if(humanName==null) continue;
                human.Add(new HumanBone {boneName=entry.Key,humanName=humanName,limit=new HumanLimit{useDefaultValues=true}});
            }
            var skeleton=new List<SkeletonBone> {new SkeletonBone{name=gameObject.name,position=Vector3.zero,rotation=Quaternion.identity,scale=Vector3.one}};
            foreach(var b in bones) skeleton.Add(new SkeletonBone{name=b.name,position=b.localPosition,rotation=b.localRotation,scale=b.localScale});
            var description=new HumanDescription {human=human.ToArray(),skeleton=skeleton.ToArray(),armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,feetSpacing=0,hasTranslationDoF=false};
            humanoidAvatar=AvatarBuilder.BuildHumanAvatar(gameObject,description);
            humanoidAvatar.name="VEIL HOUSE original detective humanoid";
            HasHumanoidRig=humanoidAvatar.isValid && humanoidAvatar.isHuman;
            Animator=gameObject.AddComponent<Animator>();
            if(HasHumanoidRig) Animator.avatar=humanoidAvatar;
            Animator.applyRootMotion=false; Animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            // No AnimatorController is needed: the pose layer below animates the skinned skeleton directly.
        }

        public void Animate(float speed,bool crouch,bool interacting)
        {
            if(body==null) return;
            float dt=Mathf.Min(Time.deltaTime,.08f);
            if(!hasPose) {crouchAmount=crouch?1:0;hasPose=true;}
            smoothedSpeed=Mathf.Lerp(smoothedSpeed,Mathf.Max(0,speed),1-Mathf.Exp(-dt*11));
            crouchAmount=Mathf.MoveTowards(crouchAmount,crouch?1:0,dt*5);
            reachAmount=Mathf.MoveTowards(reachAmount,interacting?1:0,dt*6);
            float gait=Mathf.Clamp01(smoothedSpeed/1.2f),running=Mathf.Clamp01((smoothedSpeed-2.5f)/2);
            cycle+=dt*Mathf.Lerp(1.25f,2.15f,running)*Mathf.PI*2*Mathf.Lerp(.45f,1,gait)*(1-.25f*crouchAmount);
            float wave=Mathf.Sin(cycle),other=-wave;
            float amplitude=Mathf.Lerp(26,43,running)*gait*(1-crouchAmount*.4f);
            float breathe=Mathf.Sin(Time.time*1.65f+appearanceIndex)*.006f;
            hips.localPosition=bindPositions[hips]+new Vector3(0,breathe+Mathf.Abs(wave)*.018f*gait-.30f*crouchAmount,0);
            hips.localRotation=Quaternion.Euler(0,wave*3*gait,Mathf.Cos(cycle)*1.4f*gait);
            spine.localRotation=Quaternion.Euler(crouchAmount*20+running*6,0,0);
            chest.localRotation=Quaternion.Euler(-crouchAmount*7,-wave*3*gait,0);
            head.localRotation=Quaternion.Euler(-crouchAmount*9,Mathf.Sin(Time.time*.56f+appearanceIndex)*2*(1-gait),0);
            leftThigh.localRotation=Quaternion.Euler(-wave*amplitude-55*crouchAmount,0,-1);
            rightThigh.localRotation=Quaternion.Euler(-other*amplitude-55*crouchAmount,0,1);
            leftCalf.localRotation=Quaternion.Euler(Mathf.Max(0,wave)*amplitude*.95f+105*crouchAmount,0,0);
            rightCalf.localRotation=Quaternion.Euler(Mathf.Max(0,other)*amplitude*.95f+105*crouchAmount,0,0);
            leftFoot.localRotation=Quaternion.Euler(-Mathf.Max(0,wave)*amplitude*.3f-50*crouchAmount,0,0);
            rightFoot.localRotation=Quaternion.Euler(-Mathf.Max(0,other)*amplitude*.3f-50*crouchAmount,0,0);
            leftArm.localRotation=Quaternion.Euler(wave*amplitude*.7f+running*8,0,77-crouchAmount*6);
            rightArm.localRotation=Quaternion.Slerp(Quaternion.Euler(other*amplitude*.7f+running*8,0,-77+crouchAmount*6),Quaternion.Euler(-65,0,-30),reachAmount);
            // The bind-pose forearms point along X, so local Y supplies anatomical elbow flexion.
            leftForearm.localRotation=Quaternion.Euler(0,12+running*42,7);
            rightForearm.localRotation=Quaternion.Euler(0,-12-running*42-reachAmount*32,-7);
            int stepIndex=Mathf.FloorToInt(cycle/Mathf.PI);
            if(smoothedSpeed>.3f && stepIndex!=lastStepIndex) {
                Soundscape.PlayAt(transform.position,crouch?"step_soft":"footstep");
            }
            lastStepIndex=stepIndex;
        }

        public void SetVisible(bool visible) {if(body!=null) body.enabled=visible;}
        static Vector3 V(float x,float y,float z) {return new Vector3(x,y,z);}
        void OnDestroy()
        {
            if(ownedMesh!=null) Destroy(ownedMesh);
            if(humanoidAvatar!=null) Destroy(humanoidAvatar);
            if(ownedMaterials!=null) foreach(var material in ownedMaterials) if(material!=null) Destroy(material);
        }
    }
}


