using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse {
    public enum GamePhase { Menu, Lobby, Investigation, Results }
    public enum PlayerRole { Detective, Ghost }
    public enum HauntKind { Prop, Door, Cabinet, Light, Television, Radio, Water }
    [Serializable] public class PlayerState {
        public int id; public string name; public PlayerRole role;
        public float x,y,z,yaw,pitch,speed; public bool crouch; public float energy=100;
        public Vector3 Position { get { return new Vector3(x,y,z); } set { x=value.x;y=value.y;z=value.z; } }
    }
    [Serializable] public class StoryOption { public string id,label; }
    [Serializable] public class StoryCategory { public string id,label; public StoryOption[] options; }
    [Serializable] public class StoryAnswer { public string categoryId,optionId; }
    [Serializable] public class DeathStory { public string id,title,narrative; public StoryAnswer[] answers; }
    [Serializable] public class StoryCatalog { public StoryCategory[] categories; public DeathStory[] stories; }
    [Serializable] public class JournalEntry { public string categoryId,optionId; public float submittedAt; }
    [Serializable] public class ScoreRow { public int playerId; public string name; public int correct; public float tieTime; public JournalEntry[] entries; }
    [Serializable] public class ObjectState {
        public int id; public float x,y,z,qx,qy,qz,qw=1; public bool active; public int holder=-1;
        public Vector3 Position { get { return new Vector3(x,y,z); } set { x=value.x;y=value.y;z=value.z; } }
        public Quaternion Rotation { get { return new Quaternion(qx,qy,qz,qw); } set { qx=value.x;qy=value.y;qz=value.z;qw=value.w; } }
    }
    [Serializable] public class InteractionRequest {
        public int playerId,objectId; public string action; public float x,y,z,dx,dy,dz;
        public Vector3 Target { get { return new Vector3(x,y,z); } set { x=value.x;y=value.y;z=value.z; } }
        public Vector3 Direction { get { return new Vector3(dx,dy,dz); } set { dx=value.x;dy=value.y;dz=value.z; } }
    }
}
