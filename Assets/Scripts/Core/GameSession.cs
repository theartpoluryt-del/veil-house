using System;
using System.Collections.Generic;
using UnityEngine;

namespace VeilHouse {
    /// <summary>Main-thread session authority. Transport, catalog rules and world simulation
    /// remain independent; no Unity API is used by network worker threads.</summary>
    public sealed class GameSession : MonoBehaviour {
        public static GameSession I {get; private set;}
        public bool IsHost {get; private set;}
        public bool IsTraining {get; private set;}
        public int LocalId {get; private set;}=-1;
        public int Port=7777;
        public float MatchDuration=480f;
        public GamePhase Phase {get; private set;}=GamePhase.Menu;
        public string Status {get; private set;}="Готово";
        public float TimeRemaining {get; private set;}
        public List<PlayerState> Players {get;}=new List<PlayerState>();
        public List<JournalEntry> Journal {get;}=new List<JournalEntry>();
        public List<ScoreRow> Scores {get;}=new List<ScoreRow>();
        public List<string> ChatLog {get;}=new List<string>();
        public StoryCatalog Catalog {get; private set;}
        public PlayerState LocalPlayer { get {return FindPlayer(LocalId);} }
        public PlayerRole LocalRole {get {return LocalPlayer!=null?LocalPlayer.role:PlayerRole.Detective;} }
        public DeathStory KnownStory {get {return LocalRole==PlayerRole.Ghost || Phase==GamePhase.Results ? knownStory : null;} }
        public event Action MatchStarted,MatchEnded;
        public event Action<int> PlayerInteracted;
        public Func<InteractionRequest,float> ValidateInteraction;
        public Action<InteractionRequest> InteractionAccepted;
        public Func<ObjectState[]> CaptureWorld;
        public Action<ObjectState[]> ApplyWorld;
        public Action<int,string> WorldEvent;

        LanTransport transport;
        StoryCatalog fullCatalog;
        DeathStory serverStory,knownStory;
        string joiningName;
        float nextSnapshot,nextHeartbeat,nextPose,elapsed,roundDuration;
        readonly Dictionary<int,List<JournalEntry>> journals=new Dictionary<int,List<JournalEntry>>();
        readonly Dictionary<int,float> pendingHello=new Dictionary<int,float>();
        readonly Dictionary<int,float> scheduledKick=new Dictionary<int,float>();
        readonly Dictionary<string,float> throttles=new Dictionary<string,float>();
        readonly Dictionary<int,float> lastPose=new Dictionary<int,float>();

        void Awake() {
            if(I!=null && I!=this) {Destroy(gameObject);return;}
            I=this; DontDestroyOnLoad(gameObject);
            try { fullCatalog=InvestigationRules.LoadCatalog(); Catalog=InvestigationRules.PublicCatalog(fullCatalog); }
            catch(Exception e) {Status=e.Message;Debug.LogException(e);}
            Application.runInBackground=true;
        }
        void OnDestroy() {if(I==this) I=null;transport?.Dispose();}
        void OnApplicationQuit() {transport?.Dispose();}
        public void Host(string name,int port) {
            Disconnect();
            if(fullCatalog==null) {Status="Каталог историй не загружен";return;}
            if(!ValidPort(port)) {Status="Порт должен быть от 1024 до 65535";return;}
            try {
                transport=new LanTransport();transport.Host(port);
                IsHost=true;Port=port;LocalId=0;Phase=GamePhase.Lobby;
                Players.Add(new PlayerState {id=0,name=CleanName(name),role=PlayerRole.Detective});
                Status="Лобби открыто · порт "+port;nextSnapshot=0;
            } catch(Exception e) {transport?.Dispose();transport=null;Status="Не удалось открыть порт: "+e.Message;}
        }
        public void Join(string name,string address,int port) {
            Disconnect();
            if(!ValidPort(port) || string.IsNullOrWhiteSpace(address)) {Status="Введите IP-адрес и порт 1024–65535";return;}
            Port=port;joiningName=CleanName(name);Status="Подключение к "+address.Trim()+":"+port+"…";
            transport=new LanTransport();transport.Join(address.Trim(),port);
        }
        public void Disconnect() {
            transport?.Dispose();transport=null;IsHost=false;IsTraining=false;LocalId=-1;
            Phase=GamePhase.Menu;TimeRemaining=0;Players.Clear();Journal.Clear();Scores.Clear();ChatLog.Clear();
            journals.Clear();pendingHello.Clear();scheduledKick.Clear();throttles.Clear();lastPose.Clear();
            serverStory=null;knownStory=null;Status="Соединение закрыто";nextPose=nextHeartbeat=nextSnapshot=0;
        }
        public void StartTraining(PlayerRole role) {
            Disconnect();
            if(fullCatalog==null) {Status="Каталог историй не загружен";return;}
            IsHost=true;IsTraining=true;LocalId=0;Phase=GamePhase.Lobby;
            Players.Add(new PlayerState {id=0,name="Исследователь",role=role});
            StartMatch();
        }
        public void SetRole(PlayerRole role) {
            if(Phase!=GamePhase.Lobby) return;
            if(IsHost) SetServerRole(LocalId,role); else SendServer(new SessionPacket {type="role",role=(int)role});
        }
        void SetServerRole(int id,PlayerRole role) {
            if(Phase!=GamePhase.Lobby || (role!=PlayerRole.Detective && role!=PlayerRole.Ghost)) return;
            var player=FindPlayer(id);if(player==null)return;
            if(role==PlayerRole.Ghost) foreach(var other in Players) if(other.id!=id && other.role==PlayerRole.Ghost) {
                Tell(id,"Роль Призрака уже занята. Попросите игрока сменить роль.");return;
            }
            player.role=role;nextSnapshot=0;
        }
        public void StartMatch() {
            if(!IsHost || Phase!=GamePhase.Lobby) return;
            if(!IsTraining && (Players.Count<2 || Players.Count>7)) {Status="Для матча нужны 2–7 игроков";return;}
            if(fullCatalog==null) return;
            bool ghost=false;foreach(var p in Players) if(p.role==PlayerRole.Ghost) ghost=true;
            if(!ghost && !IsTraining) Players[0].role=PlayerRole.Ghost;
            journals.Clear();Journal.Clear();Scores.Clear();ChatLog.Clear();throttles.Clear();lastPose.Clear();
            foreach(var p in Players) {p.energy=100;p.speed=0;if(p.role==PlayerRole.Detective) journals[p.id]=new List<JournalEntry>();}
            roundDuration=Mathf.Clamp(MatchDuration,10f,7200f);TimeRemaining=roundDuration;elapsed=0;
            serverStory=fullCatalog.stories[UnityEngine.Random.Range(0,fullCatalog.stories.Length)];
            knownStory=LocalRole==PlayerRole.Ghost?serverStory:null;
            Phase=GamePhase.Investigation;Status=IsTraining?"Учебное расследование":"Расследование началось";
            MatchStarted?.Invoke();
            foreach(var p in Players) if(p.id!=LocalId) Send(p.id,new SessionPacket {
                type="start",phase=(int)Phase,players=Players.ToArray(),remaining=TimeRemaining,duration=roundDuration,
                story=p.role==PlayerRole.Ghost?serverStory:null,training=IsTraining
            });
            nextSnapshot=0;
        }
        public void FinishMatch() {
            if(!IsHost || Phase!=GamePhase.Investigation)return;
            Phase=GamePhase.Results;TimeRemaining=0;knownStory=serverStory;
            Scores.Clear();Scores.AddRange(InvestigationRules.Score(Players,journals,serverStory));
            Status="История раскрыта";
            Broadcast(new SessionPacket {type="results",phase=(int)Phase,players=Players.ToArray(),story=serverStory,scores=Scores.ToArray(),duration=roundDuration});
            MatchEnded?.Invoke();nextSnapshot=0;
        }
        public void ReturnToLobby() {
            if(!IsHost || Phase!=GamePhase.Results) return;
            Phase=GamePhase.Lobby;knownStory=null;serverStory=null;TimeRemaining=0;
            Scores.Clear();Journal.Clear();journals.Clear();ChatLog.Clear();Status="Готовы к новой истории";
            Broadcast(new SessionPacket {type="lobby",phase=(int)Phase,players=Players.ToArray()});nextSnapshot=0;
        }
        public void SubmitAnswer(string category,string option) {
            if(Phase!=GamePhase.Investigation || LocalRole!=PlayerRole.Detective)return;
            if(IsHost) ServerAnswer(LocalId,category,option);else SendServer(new SessionPacket {type="answer",category=category,option=option});
        }
        void ServerAnswer(int id,string category,string option) {
            var p=FindPlayer(id);List<JournalEntry> journal;
            if(Phase!=GamePhase.Investigation || p==null || p.role!=PlayerRole.Detective || !journals.TryGetValue(id,out journal))return;
            if(!InvestigationRules.Submit(journal,Catalog,category,option,elapsed))return;
            if(id==LocalId) {Journal.Clear();Journal.AddRange(journal);} else Send(id,new SessionPacket {type="journal",entries=journal.ToArray()});
        }
        public void SendPose(Vector3 position,float yaw,float pitch,bool crouch,float speed) {
            if(Phase!=GamePhase.Investigation || Time.unscaledTime<nextPose)return;
            nextPose=Time.unscaledTime+1f/15f;
            var packet=new SessionPacket {type="pose",x=position.x,y=position.y,z=position.z,yaw=yaw,pitch=pitch,crouch=crouch,speed=speed};
            if(IsHost) ServerPose(LocalId,packet);else {
                // Locally predicted pose keeps cameras and avatar animation independent of RTT.
                var p=LocalPlayer;if(p!=null)ApplyPose(p,packet);
                SendServer(packet);
            }
        }
        void ServerPose(int id,SessionPacket packet) {
            if(Phase!=GamePhase.Investigation || !Finite(packet.x) || !Finite(packet.y) || !Finite(packet.z) || !Finite(packet.yaw) || !Finite(packet.pitch) || !Finite(packet.speed))return;
            if(Mathf.Abs(packet.x)>500 || Mathf.Abs(packet.y)>100 || Mathf.Abs(packet.z)>500)return;
            var p=FindPlayer(id);if(p==null)return;
            float last;float now=Time.unscaledTime;
            if(lastPose.TryGetValue(id,out last)) {
                if(now-last<0.025f)return;
                float limit=(p.role==PlayerRole.Ghost?22f:12f)*Mathf.Min(now-last,1.5f)+2.5f;
                if(Vector3.Distance(p.Position,new Vector3(packet.x,packet.y,packet.z))>limit)return;
            }
            lastPose[id]=now;ApplyPose(p,packet);
        }
        static void ApplyPose(PlayerState player,SessionPacket packet) {
            player.Position=new Vector3(packet.x,packet.y,packet.z);player.yaw=Mathf.Repeat(packet.yaw,360f);
            player.pitch=Mathf.Clamp(packet.pitch,-89f,89f);player.crouch=packet.crouch;player.speed=Mathf.Clamp(packet.speed,0,20);
        }
        public void RequestInteraction(int objectId,string action,Vector3 target,Vector3 direction) {
            if(Phase!=GamePhase.Investigation)return;
            var request=new InteractionRequest {playerId=LocalId,objectId=objectId,action=action,Target=target,Direction=direction};
            if(IsHost) ServerInteraction(LocalId,request);else SendServer(new SessionPacket {type="interact",interaction=request});
        }
        void ServerInteraction(int id,InteractionRequest request) {
            var p=FindPlayer(id);
            if(Phase!=GamePhase.Investigation || p==null || request==null || ValidateInteraction==null || InteractionAccepted==null)return;
            if(p.role!=PlayerRole.Ghost && request.action!="toggle" && request.action!="open" && request.action!="close")return;
            if(!Finite(request.x)||!Finite(request.y)||!Finite(request.z)||!Finite(request.dx)||!Finite(request.dy)||!Finite(request.dz))return;
            request.playerId=id; // Never trust the identity supplied by the remote client.
            float minimum,cooldown;
            switch(request.action) {
                case "toggle": case "open": case "close": minimum=p.role==PlayerRole.Ghost?2:0;cooldown=.28f;break;
                case "hold": minimum=2;cooldown=.3f;break;
                case "move": minimum=.15f;cooldown=.055f;break;
                case "release": minimum=0;cooldown=.025f;break;
                case "throw": minimum=5;cooldown=.45f;break;
                case "shake": minimum=3;cooldown=.65f;break;
                case "knock": minimum=1;cooldown=.5f;break;
                case "poltergeist": minimum=35;cooldown=15;break;
                case "blackout": minimum=28;cooldown=12;break;
                default:return;
            }
            string key=id+":"+request.action;float next;
            if(throttles.TryGetValue(key,out next) && Time.unscaledTime<next){if(request.action=="poltergeist"||request.action=="blackout")Tell(id,"Воздействие восстанавливается: "+Mathf.CeilToInt(next-Time.unscaledTime)+" с");return;}
            float worldCost;
            try {worldCost=ValidateInteraction(request);} catch(Exception e) {Debug.LogException(e);return;}
            if(!Finite(worldCost) || worldCost<0)return;
            float cost=Mathf.Max(minimum,worldCost);
            if(p.energy+0.0001f<cost) {Tell(id,"Недостаточно паранормальной энергии");return;}
            p.energy=Mathf.Max(0,p.energy-cost);throttles[key]=Time.unscaledTime+cooldown;
            try {InteractionAccepted(request);} catch(Exception e) {p.energy=Mathf.Min(100,p.energy+cost);Debug.LogException(e);return;}
            if(request.action!="move") {
                WorldEvent?.Invoke(request.objectId,request.action);
                PlayerInteracted?.Invoke(id);
                Broadcast(new SessionPacket {type="event",eventPlayer=request.objectId,eventActor=id,eventName=request.action});
            }
        }
        public void SendChat(string text) {
            if(Phase==GamePhase.Menu || LocalRole==PlayerRole.Ghost)return;
            if(IsHost) ServerChat(LocalId,text);else SendServer(new SessionPacket {type="chat",text=text});
        }
        void ServerChat(int id,string text) {
            var p=FindPlayer(id);if(p==null || p.role==PlayerRole.Ghost || string.IsNullOrWhiteSpace(text))return;
            string key=id+":chat";float last;
            if(throttles.TryGetValue(key,out last) && Time.unscaledTime<last)return;
            throttles[key]=Time.unscaledTime+.7f;text=CleanText(text,180);
            string line=p.name+": "+text;AddChat(line);Broadcast(new SessionPacket {type="chatline",text=line});
        }
        void Update() {
            LanTransport.Event ev;int limit=192;
            while(transport!=null && limit-->0 && transport.TryDequeue(out ev)) HandleTransport(ev);
            if(!IsHost) {
                if(Phase==GamePhase.Investigation) TimeRemaining=Mathf.Max(0,TimeRemaining-Time.unscaledDeltaTime);
                if(transport!=null && LocalId>=0 && Time.unscaledTime>=nextHeartbeat) {nextHeartbeat=Time.unscaledTime+2;SendServer(new SessionPacket {type="ping"});}
                return;
            }
            if(Phase==GamePhase.Investigation) {
                elapsed+=Time.unscaledDeltaTime;TimeRemaining=Mathf.Max(0,roundDuration-elapsed);
                foreach(var p in Players) if(p.role==PlayerRole.Ghost)p.energy=Mathf.Min(100,p.energy+5f*Time.unscaledDeltaTime);
                if(TimeRemaining<=0)FinishMatch();
            }
            if(transport!=null) {
                float now=Time.unscaledTime;
                var expire=new List<int>();foreach(var item in pendingHello)if(now-item.Value>10)expire.Add(item.Key);
                foreach(int id in expire){pendingHello.Remove(id);transport.Kick(id,"Истекло время подключения");}
                expire.Clear();foreach(var item in scheduledKick)if(now>=item.Value)expire.Add(item.Key);
                foreach(int id in expire){scheduledKick.Remove(id);transport.Kick(id,"Вход отклонён");}
                if(now>=nextSnapshot) {nextSnapshot=now+(Phase==GamePhase.Investigation?.1f:.5f);Snapshot();}
            }
        }
        void HandleTransport(LanTransport.Event ev) {
            if(ev.kind==LanTransport.EventKind.Connected) {
                if(IsHost) pendingHello[ev.peer]=Time.unscaledTime;
                else SendServer(new SessionPacket {type="hello",name=joiningName,protocol=1});
                return;
            }
            if(ev.kind==LanTransport.EventKind.Error) {if(IsHost)Status=ev.text;else {Disconnect();Status=ev.text;}return;}
            if(ev.kind==LanTransport.EventKind.Disconnected) {
                if(IsHost) {
                    pendingHello.Remove(ev.peer);scheduledKick.Remove(ev.peer);
                    var p=FindPlayer(ev.peer);if(p==null)return;
                    bool ghost=p.role==PlayerRole.Ghost;Players.Remove(p);Status=p.name+" покинул дом";
                    int detectives=0;foreach(var member in Players)if(member.role==PlayerRole.Detective)detectives++;
                    if(Phase==GamePhase.Investigation && (ghost || detectives==0))FinishMatch();
                    nextSnapshot=0;
                } else {string reason=Status.StartsWith("Вход отклонён")?Status:"Связь с ведущим потеряна. "+ev.text;Disconnect();Status=reason;}
                return;
            }
            try {
                var packet=JsonUtility.FromJson<SessionPacket>(ev.text);
                if(packet==null || string.IsNullOrEmpty(packet.type))return;
                if(IsHost) HandleServerPacket(ev.peer,packet);else HandleClientPacket(packet);
            } catch(Exception ex) {Debug.LogWarning("Invalid session packet: "+ex.Message);if(IsHost)transport?.Kick(ev.peer,"Некорректное сообщение");}
        }
        void HandleServerPacket(int peer,SessionPacket packet) {
            if(packet.type=="hello") {
                if(FindPlayer(peer)!=null)return;
                if(packet.protocol!=1 || Phase!=GamePhase.Lobby || Players.Count>=7) {
                    Send(peer,new SessionPacket {type="error",text=packet.protocol!=1?"Несовместимая версия игры":Phase!=GamePhase.Lobby?"Расследование уже идёт. Дождитесь нового лобби.":"Лобби заполнено"});
                    pendingHello.Remove(peer);scheduledKick[peer]=Time.unscaledTime+.4f;return;
                }
                pendingHello.Remove(peer);Players.Add(new PlayerState {id=peer,name=CleanName(packet.name),role=PlayerRole.Detective});
                Send(peer,new SessionPacket {type="welcome",id=peer,phase=(int)Phase,players=Players.ToArray(),catalog=Catalog});nextSnapshot=0;return;
            }
            if(FindPlayer(peer)==null || scheduledKick.ContainsKey(peer))return;
            switch(packet.type) {
                case "role":SetServerRole(peer,(PlayerRole)packet.role);break;
                case "answer":ServerAnswer(peer,packet.category,packet.option);break;
                case "pose":ServerPose(peer,packet);break;
                case "interact":ServerInteraction(peer,packet.interaction);break;
                case "chat":ServerChat(peer,packet.text);break;
                case "ping":break;
            }
        }
        void HandleClientPacket(SessionPacket packet) {
            switch(packet.type) {
                case "welcome":
                    LocalId=packet.id;Catalog=packet.catalog??Catalog;UpdatePlayers(packet.players);Phase=GamePhase.Lobby;Status="Подключено к лобби";break;
                case "snapshot":
                    UpdatePlayers(packet.players);TimeRemaining=packet.remaining;
                    if(packet.objects!=null && Phase==GamePhase.Investigation)ApplyWorld?.Invoke(packet.objects);break;
                case "start":
                    UpdatePlayers(packet.players);knownStory=LocalRole==PlayerRole.Ghost?packet.story:null;
                    roundDuration=packet.duration;MatchDuration=roundDuration;TimeRemaining=packet.remaining;
                    IsTraining=packet.training;Journal.Clear();Scores.Clear();ChatLog.Clear();Phase=GamePhase.Investigation;
                    Status="Расследование началось";nextPose=0;MatchStarted?.Invoke();break;
                case "results":
                    UpdatePlayers(packet.players);knownStory=packet.story;Scores.Clear();if(packet.scores!=null)Scores.AddRange(packet.scores);
                    Phase=GamePhase.Results;TimeRemaining=0;Status="История раскрыта";MatchEnded?.Invoke();break;
                case "lobby":
                    UpdatePlayers(packet.players);knownStory=null;Phase=GamePhase.Lobby;Journal.Clear();Scores.Clear();ChatLog.Clear();Status="Готовы к новой истории";break;
                case "journal":Journal.Clear();if(packet.entries!=null)Journal.AddRange(packet.entries);break;
                case "chatline":AddChat(packet.text);break;
                case "event":WorldEvent?.Invoke(packet.eventPlayer,packet.eventName);PlayerInteracted?.Invoke(packet.eventActor);break;
                case "notice":Status=packet.text;break;
                case "error":Status="Вход отклонён: "+packet.text;break;
            }
        }
        void UpdatePlayers(PlayerState[] players) {
            if(players==null)return;
            // Preserve local prediction while accepting authoritative roles and energy.
            PlayerState predicted=Phase==GamePhase.Investigation?LocalPlayer:null;
            Players.Clear();Players.AddRange(players);
            if(predicted!=null) {var p=LocalPlayer;if(p!=null){p.Position=predicted.Position;p.yaw=predicted.yaw;p.pitch=predicted.pitch;p.crouch=predicted.crouch;p.speed=predicted.speed;}}
        }
        void Snapshot() {
            ObjectState[] objects=null;
            if(Phase==GamePhase.Investigation && CaptureWorld!=null)objects=CaptureWorld();
            Broadcast(new SessionPacket {type="snapshot",phase=(int)Phase,players=Players.ToArray(),remaining=TimeRemaining,objects=objects});
        }
        void SendServer(SessionPacket packet) {Send(0,packet);}
        void Send(int peer,SessionPacket packet) {transport?.Send(peer,JsonUtility.ToJson(packet));}
        void Broadcast(SessionPacket packet) {
            if(transport==null)return;string text=JsonUtility.ToJson(packet);
            foreach(var p in Players)if(p.id!=LocalId)transport.Send(p.id,text);
        }
        void Tell(int id,string text) {if(id==LocalId)Status=text;else Send(id,new SessionPacket {type="notice",text=text});}
        void AddChat(string text) {if(string.IsNullOrEmpty(text))return;ChatLog.Add(text);while(ChatLog.Count>40)ChatLog.RemoveAt(0);}
        public PlayerState FindPlayer(int id) {foreach(var p in Players)if(p.id==id)return p;return null;}
        static bool ValidPort(int port) {return port>=1024 && port<=65535;}
        static bool Finite(float value) {return !float.IsNaN(value) && !float.IsInfinity(value);}
        static string CleanName(string value) {string name=CleanText(value??"",24).Trim();return name.Length==0?"Детектив":name;}
        static string CleanText(string value,int length) {
            if(value==null)return "";var result=new System.Text.StringBuilder();
            foreach(char c in value) {if(!char.IsControl(c) && c!='<' && c!='>')result.Append(c);if(result.Length>=length)break;}return result.ToString();
        }
    }
}
