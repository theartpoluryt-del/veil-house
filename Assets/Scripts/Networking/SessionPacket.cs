using System;

namespace VeilHouse {
    // A deliberately versioned, plain-data wire format. Unity objects never cross threads.
    [Serializable] internal sealed class SessionPacket {
        public string type;
        public int protocol=1,id,role,phase,eventPlayer,eventActor;
        public string name,text,category,option,eventName;
        public float remaining,duration,x,y,z,yaw,pitch,speed;
        public bool crouch,training;
        public PlayerState[] players;
        public ObjectState[] objects;
        public StoryCatalog catalog;
        public DeathStory story;
        public JournalEntry[] entries;
        public ScoreRow[] scores;
        public InteractionRequest interaction;
    }
}
