using System;
using System.Linq;
using UnityEngine;

namespace VeilHouse {
 public sealed class GameUI : MonoBehaviour {
  const float W=1600,H=900;
  readonly Color ink=new Color(.045f,.071f,.078f),panel=new Color(.055f,.085f,.09f,.97f),cream=new Color(.9f,.87f,.79f),gold=new Color(.73f,.59f,.36f),muted=new Color(.53f,.6f,.59f),teal=new Color(.41f,.73f,.69f);
  Font editorial,regular; Texture2D white,shade,vignette;
  GUIStyle label,serif,button,input;
  public bool JournalOpen,PauseOpen,ChatOpen,HelpOpen;
  public bool BlocksMovement=>JournalOpen||PauseOpen||ChatOpen||HelpOpen;
  string playerName="Детектив",address="127.0.0.1",port="7777",chat="";
  bool joining,training; readonly System.Collections.Generic.Dictionary<string,Vector2> journalScroll=new System.Collections.Generic.Dictionary<string,Vector2>();
  float volume=.65f; bool initialized;
  GameSession S=>GameSession.I;
  RuntimeDirector D=>RuntimeDirector.I;
  public void ClosePanels(){JournalOpen=PauseOpen=ChatOpen=HelpOpen=false;}
  void Awake(){playerName=PlayerPrefs.GetString("detectiveName","Детектив");volume=PlayerPrefs.GetFloat("volume",.65f);AudioListener.volume=volume;}
  void Init() {
   if(initialized)return;initialized=true;
   regular=Resources.Load<Font>("Interface"); editorial=Resources.Load<Font>("Editorial");
   if(regular==null)regular=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if(editorial==null)editorial=regular;
   white=Texture2D.whiteTexture;
   label=new GUIStyle{font=regular,fontSize=20,wordWrap=true,richText=false,normal={textColor=cream}};
   serif=new GUIStyle(label){font=editorial};
   button=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=18};
   input=new GUIStyle(label){padding=new RectOffset(18,12,10,8),alignment=TextAnchor.MiddleLeft,clipping=TextClipping.Clip,normal={textColor=cream,background=white},focused={textColor=cream,background=white}};
   shade=new Texture2D(128,1,TextureFormat.RGBA32,false); for(int x=0;x<128;x++)shade.SetPixel(x,0,new Color(ink.r,ink.g,ink.b,Mathf.Lerp(.97f,0,Mathf.Pow(x/127f,2.7f))));shade.Apply();
   vignette=new Texture2D(128,72,TextureFormat.RGBA32,false);for(int y=0;y<72;y++)for(int x=0;x<128;x++){float xx=(x-63.5f)/64, yy=(y-35.5f)/36;float a=Mathf.Clamp01((xx*xx+yy*yy-.4f)*.38f);vignette.SetPixel(x,y,new Color(.015f,.028f,.034f,a));}vignette.Apply();
  }
  void Update() {
   if(RuntimeProbe.Running)return;
   if(S.Phase!=GamePhase.Investigation)return;
   if(Input.GetKeyDown(KeyCode.Tab)&&!ChatOpen){JournalOpen=!JournalOpen;PauseOpen=HelpOpen=false;}
   if(Input.GetKeyDown(KeyCode.F1)){HelpOpen=!HelpOpen;PauseOpen=JournalOpen=false;}
   if(Input.GetKeyDown(KeyCode.Escape)){if(ChatOpen)ChatOpen=false;else if(JournalOpen||HelpOpen){JournalOpen=HelpOpen=false;}else PauseOpen=!PauseOpen;}
   if(Input.GetKeyDown(KeyCode.Return)&&!PauseOpen&&!JournalOpen&&!HelpOpen&&S.LocalPlayer!=null&&S.LocalPlayer.role==PlayerRole.Detective){if(ChatOpen){if(!string.IsNullOrWhiteSpace(chat))S.SendChat(chat);chat="";ChatOpen=false;}else ChatOpen=true;}
  }
  void OnGUI() {
   // Automated visual probes still render the UI, but never consume desktop input.
   if(RuntimeProbe.Running&&Event.current.type!=EventType.Layout&&Event.current.type!=EventType.Repaint)return;
   Init();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/W,Screen.height/H,1));
   GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,W,H),vignette);
   switch(S.Phase){case GamePhase.Menu:Menu();break;case GamePhase.Lobby:Lobby();break;case GamePhase.Investigation:Hud();break;case GamePhase.Results:Results();break;}
   if(S.Phase==GamePhase.Investigation){if(JournalOpen)Journal();if(PauseOpen)Pause();if(HelpOpen)Help();}
   if(Time.unscaledTime<D.ToastUntil&&!BlocksMovement&&S.Phase==GamePhase.Investigation){Box(new Rect(430,715,740,64),new Color(.04f,.08f,.09f,.92f));Text(new Rect(452,727,696,42),D.Toast,17,cream,TextAnchor.MiddleCenter);}
   if(!string.IsNullOrEmpty(S.Status)&&S.Status!=lastStatus){lastStatus=S.Status;statusUntil=Time.unscaledTime+6;}
   if(S.Phase==GamePhase.Investigation&&statusUntil>Time.unscaledTime&&!BlocksMovement&&!string.IsNullOrEmpty(S.Status)){Text(new Rect(430,675,740,38),S.Status,16,muted,TextAnchor.MiddleCenter);}
  }
  string lastStatus="";float statusUntil;
  void Text(Rect r,string t,int size=20,Color? c=null,TextAnchor align=TextAnchor.UpperLeft,bool title=false){var style=title?serif:label;style.fontSize=size;style.normal.textColor=c??cream;style.alignment=align;GUI.Label(r,t,style);}
  void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
  void Line(float x,float y,float w,Color? c=null){Box(new Rect(x,y,w,1),c??new Color(gold.r,gold.g,gold.b,.38f));}
  bool Button(Rect r,string text,bool primary=false,bool enabled=true){bool hover=r.Contains(Event.current.mousePosition);Box(r,primary?(hover?new Color(.83f,.69f,.46f):gold):(hover?new Color(.14f,.22f,.23f):new Color(.085f,.13f,.14f)));if(!primary){Line(r.x,r.y,r.width,new Color(.4f,.5f,.48f,.3f));Line(r.x,r.yMax-1,r.width,new Color(.4f,.5f,.48f,.3f));}button.normal.textColor=primary?ink:enabled?cream:muted;GUI.enabled=enabled;bool pressed=GUI.Button(r,text,button);GUI.enabled=true;if(pressed)Soundscape.PlayAt(D.View.transform.position,"ui");return pressed;}
  string Field(Rect r,string value,int max=32){GUI.backgroundColor=new Color(.09f,.14f,.15f);var answer=GUI.TextField(r,value,max,input);GUI.backgroundColor=Color.white;Line(r.x,r.yMax,r.width,gold*.6f);return answer;}
  void Brand(float x=64,float y=40){Box(new Rect(x,y,43,43),gold);Text(new Rect(x,y+3,43,38),"V",26,ink,TextAnchor.MiddleCenter,true);Text(new Rect(x+59,y+3,320,24),"VEIL HOUSE",17,cream);Text(new Rect(x+59,y+26,340,22),"БЮРО НЕОБЪЯСНИМЫХ ПРОИСШЕСТВИЙ",10,gold);}
  void Menu(){
   if(HelpOpen){Help();return;}
   GUI.DrawTexture(new Rect(0,0,1100,H),shade);Brand();
   if(!joining&&!training){Text(new Rect(1210,43,323,25),"ВАШЕ ИМЯ",11,gold,TextAnchor.MiddleRight);playerName=Field(new Rect(1210,77,323,44),playerName,20);}
   Text(new Rect(66,149,590,26),"ДЕЛО № 013     /     ОСЕНЬ, 1927",14,gold);
   Text(new Rect(62,198,685,208),"Дом по ту\nсторону",74,cream,TextAnchor.UpperLeft,true);
   Line(66,418, 78);
   Text(new Rect(66,444,485,70),"У каждого дома есть тайна.\nУ этого — голос.",25,cream,TextAnchor.UpperLeft,true);
   Text(new Rect(67,534,485,50),"Один Призрак. До шести Детективов.\nИстория, которую нельзя рассказать словами.",17,muted);
   if(!joining&&!training){
    if(Button(new Rect(66,613,414,57),"Создать расследование    →",true)){SaveName();S.Host(playerName,PortValue());}
    if(Button(new Rect(66,685,414,53),"Присоединиться"))joining=true;
    if(Button(new Rect(66,750,200,45),"Осмотреть дом"))training=true;
    if(Button(new Rect(280,750,200,45),"Как играть"))HelpOpen=true;
   } else if(joining) {
    Box(new Rect(645,236,520,455),panel);Text(new Rect(680,271,440,45),"Войти в дело",32,cream,title:true);
    Text(new Rect(680,336,220,22),"ВАШЕ ИМЯ",12,gold);playerName=Field(new Rect(680,365,450,46),playerName,20);
    Text(new Rect(680,429,260,22),"IP ХОСТА / АДРЕС VPN",12,gold);address=Field(new Rect(680,458,330,46),address,80);port=Field(new Rect(1022,458,108,46),port,5);
    if(Button(new Rect(680,533,450,55),"Подключиться",true)){SaveName();S.Join(playerName,address,PortValue());}
    if(Button(new Rect(680,607,450,40),"Назад")){S.Disconnect();joining=false;}
    Text(new Rect(680,706,500,65),S.Status,16,cream);
   } else {
    Box(new Rect(645,276,520,365),panel);Text(new Rect(680,313,440,47),"Познакомьтесь с домом",30,cream,title:true);
    Text(new Rect(680,378,440,66),"Учебный режим на одного игрока.\nИзучите управление и взаимодействия.",18,muted);
    if(Button(new Rect(680,466,218,58),"Я — Призрак",true))S.StartTraining(PlayerRole.Ghost);
    if(Button(new Rect(912,466,218,58),"Я — Детектив"))S.StartTraining(PlayerRole.Detective);
    if(Button(new Rect(680,550,450,45),"Назад"))training=false;
   }
   Text(new Rect(66,849,900,25),"2–7 ИГРОКОВ    ·    КООПЕРАТИВНОЕ РАССЛЕДОВАНИЕ    ·    UNITY 6",12,muted);
   Text(new Rect(1190,840,344,30),"ПРОТОТИП  /  "+Application.version,13,gold,TextAnchor.MiddleRight);
   if(!joining&&!training&&!string.IsNullOrWhiteSpace(S.Status))Text(new Rect(650,770,650,46),S.Status,17,cream);
   if(HelpOpen)Help();
  }
  int PortValue(){return int.TryParse(port,out int p)&&p>1023&&p<65536?p:7777;}
  void SaveName(){playerName=string.IsNullOrWhiteSpace(playerName)?"Детектив":playerName.Trim();PlayerPrefs.SetString("detectiveName",playerName);}
  void Lobby(){
   Box(new Rect(0,0,W,H),new Color(.02f,.04f,.05f,.73f));Brand();
   Text(new Rect(80,133,1000,58),"Прежде чем погаснет свет",42,cream,title:true);
   Text(new Rect(80,201,1100,35),"Выберите роль. Призрак знает историю; Детективы учатся слушать дом.",19,muted);
   Box(new Rect(80,270,935,422),panel);
   Text(new Rect(109,294,860,30),"УЧАСТНИКИ РАССЛЕДОВАНИЯ",13,gold);
   int row=0;foreach(var p in S.Players){float y=341+row*44;bool local=p.id==S.LocalId;Text(new Rect(109,y,52,32),(row+1).ToString("00"),19,gold,title:true);Text(new Rect(175,y,470,32),p.name+(local?"   ·   вы":""),20,cream);Text(new Rect(715,y,260,30),p.role==PlayerRole.Ghost?"ПРИЗРАК":"ДЕТЕКТИВ",14,p.role==PlayerRole.Ghost?teal:muted);Line(109,y+36,860,new Color(.3f,.4f,.4f,.15f));row++;}
   if(row<7)Text(new Rect(174,344+row*44,700,40),"Ожидаем других участников…",17,muted);
   Box(new Rect(1045,270,475,422),panel);Text(new Rect(1074,296,410,28),"ВАША РОЛЬ",13,gold);
   bool ghost=S.LocalPlayer!=null&&S.LocalPlayer.role==PlayerRole.Ghost;
   if(Button(new Rect(1074,352,416,61),"Детектив",!ghost))S.SetRole(PlayerRole.Detective);
   if(Button(new Rect(1074,427,416,61),"Призрак",ghost))S.SetRole(PlayerRole.Ghost);
   Text(new Rect(1074,515,416,132),ghost?"Ваша речь — предметы, свет и звуки.\nНаправляйте чужие догадки и берегите паранормальную энергию.":"Исследуйте комнаты. Обсуждайте события и ведите личный журнал. Точность и время верных ответов определят победителя.",18,muted);
   if(S.IsHost){if(Button(new Rect(1074,731,416, 60),"Начать расследование    →",true,S.IsTraining||S.Players.Count>=2))S.StartMatch();}
   else Text(new Rect(1074,739,416,60),"Хозяин лобби начнёт расследование",19,cream);
   if(Button(new Rect(80,731,220,58),"Выйти из лобби"))S.Disconnect();
   Text(new Rect(330,738,655,60),S.Status,16,cream);
   Text(new Rect(80,838,1450,28),(S.IsHost?"IP ХОСТА: "+LocalAddress():"ПОДКЛЮЧЕНИЕ ПО IP")+"  ·  ПОРТ "+S.Port+"  ·  LAN / VPN  ·  "+S.Players.Count+" ИЗ 7 ИГРОКОВ",13,muted);
  }
  void Hud(){
   var p=S.LocalPlayer;if(p==null)return;bool ghost=p.role==PlayerRole.Ghost;
   Text(new Rect(42,30,480,25),"ДЕЛО № 013  /  "+(S.IsTraining?"УЧЕБНЫЙ РЕЖИМ":"РАССЛЕДОВАНИЕ"),12,gold);
   Text(new Rect(42,61,540,50),WorldBuilder.RoomAt(D.Controller.Position),30,cream,title:true);
   Box(new Rect(727,26,146,59),new Color(.035f,.06f,.07f,.8f));Text(new Rect(727,35,146,40),FormatTime(S.TimeRemaining),25,cream,TextAnchor.MiddleCenter,true);
   Text(new Rect(1120,31,437,24),ghost?"ПРИЗРАК  /  НЕВИДИМ ДЛЯ ДЕТЕКТИВОВ":"ДЕТЕКТИВ  /  "+p.name.ToUpperInvariant(),12,ghost?teal:gold,TextAnchor.MiddleRight);
   if(ghost){float energy=p.energy;Text(new Rect(1160,65,396,32),"ПАРАНОРМАЛЬНАЯ ЭНЕРГИЯ    "+Mathf.FloorToInt(energy),14,cream,TextAnchor.MiddleRight);Box(new Rect(1236,106,320,3),new Color(.2f,.3f,.3f,.5f));Box(new Rect(1236,106,320*Mathf.Clamp01(energy/100),3),teal);}
   else Text(new Rect(1236,65,320,32),S.Journal.Count+" / "+S.Catalog.categories.Length+" гипотез в журнале",15,muted,TextAnchor.MiddleRight);
   if(!BlocksMovement){
    Box(new Rect(798,447,4,4),new Color(.88f,.9f,.84f,.8f));
    var f=D.Controller.Focus;
    if(f!=null){Text(new Rect(530,561,540,35),f.DisplayName,24,cream,TextAnchor.MiddleCenter,true);string hint=f.Kind==HauntKind.Prop?(ghost?"E  поднять   ·   R  встряхнуть":"Наблюдайте: этот предмет может стать подсказкой"):(ghost?"E  воздействовать   ·   "+f.Cost+" энергии":f.Kind==HauntKind.Door||f.Kind==HauntKind.Cabinet?"E  открыть / закрыть":"Прислушайтесь к дому");Text(new Rect(490,607,620,28),hint,16,ghost?teal:cream,TextAnchor.MiddleCenter);}
    if(D.Controller.HeldId>=0){Text(new Rect(500,653,600,33),"ЛКМ / F  бросить    ·    E / ПКМ  отпустить",17,teal,TextAnchor.MiddleCenter);}
   }
   Box(new Rect(0,804,W,96),new Color(.025f,.046f,.055f,.87f));Line(36,804,1528,new Color(.6f,.5f,.34f,.2f));
   if(ghost){Key(42,829,"E","воздействовать");Key(305,829,"R","дрожь · 3");Key(548,829,"Q","полтергейст · 35");Key(860,829,"C","тьма · 28");Key(1110,829,"TAB","история");}
   else {Key(42,829,"TAB","личный журнал");Key(373,829,"F","фонарик");Key(643,829,"ENTER","чат детективов");Key(1000,829,"SHIFT","бег");}
   Key(1400,829,"F1","помощь",false);
   if(S.ChatLog.Count>0&&!JournalOpen&&!PauseOpen){int start=Math.Max(0,S.ChatLog.Count-4);for(int i=start;i<S.ChatLog.Count;i++)Text(new Rect(40,617+(i-start)*26,660,27),S.ChatLog[i],15,cream);}
   if(ChatOpen){Box(new Rect(32,737,650,52),panel);GUI.SetNextControlName("chat");chat=Field(new Rect(38,740,639,43),chat,160);GUI.FocusControl("chat");}
  }
  void Key(float x,float y,string key,string action,bool wide=true){Box(new Rect(x,y, key.Length>2?58:36,30),new Color(.13f,.2f,.21f));Text(new Rect(x,y+2,key.Length>2?58:36,25),key,12,gold,TextAnchor.MiddleCenter);Text(new Rect(x+(key.Length>2?70:48),y+4,wide?260:110,28),action,16,cream);}
  void Journal(){
   bool ghost=D.Controller.Ghost;Box(new Rect(0,0,W,H),new Color(.02f,.04f,.05f,.86f));
   Box(new Rect(65,71,1470,740),ghost?panel:new Color(.89f,.86f,.77f));Color fg=ghost?cream:ink, sub=ghost?muted:new Color(.36f,.4f,.38f);
   Text(new Rect(105,98,1030,30),ghost?"ТОЛЬКО ДЛЯ ГЛАЗ ПРИЗРАКА":"ЛИЧНЫЙ ЖУРНАЛ  /  "+S.LocalPlayer.name.ToUpperInvariant(),13,ghost?gold:sub);
   Text(new Rect(105,142,1190, 58),ghost?"Что случилось в этом доме":"Пять вопросов к тишине",38,fg,title:true);
   if(Button(new Rect(1360,98,136,42),"Tab  Закрыть"))JournalOpen=false;
   Line(105,216,1390,ghost?gold:new Color(.5f,.46f,.36f,.4f));
   var categories=S.Catalog.categories;
   for(int c=0;c<categories.Length;c++){
    float x=105+c*(1390f/categories.Length);float width=1390f/categories.Length-18;
    Text(new Rect(x,245,width,30),(c+1).ToString("00"),17,ghost?gold:sub,title:true);
    Text(new Rect(x,282,width,40),categories[c].label,24,fg,title:true);
    if(ghost){var answer=S.KnownStory?.answers.FirstOrDefault(a=>a.categoryId==categories[c].id);var option=categories[c].options.FirstOrDefault(o=>o.id==answer?.optionId);Text(new Rect(x,348,width,130),option?.label??"История поступает…",25,teal,title:true);}
    else {
     var chosen=S.Journal.FirstOrDefault(e=>e.categoryId==categories[c].id);
     Vector2 scroll;journalScroll.TryGetValue(categories[c].id,out scroll);float contentWidth=width-(categories[c].options.Length>9?20:0);
     scroll=GUI.BeginScrollView(new Rect(x,337,width,403),scroll,new Rect(0,0,contentWidth,categories[c].options.Length*43));
     int i=0;foreach(var opt in categories[c].options){bool selected=chosen!=null&&chosen.optionId==opt.id;var r=new Rect(0,i*43,contentWidth,37);bool hover=r.Contains(Event.current.mousePosition);Box(r,selected?new Color(.14f,.25f,.25f):hover?new Color(.77f,.75f,.66f):new Color(.84f,.81f,.72f));Text(new Rect(12,r.y+7,contentWidth-20,29),(selected?"●  ":"○  ")+opt.label,15,selected?cream:ink);if(GUI.Button(r,GUIContent.none,GUIStyle.none)){S.SubmitAnswer(categories[c].id,opt.id);Soundscape.PlayAt(D.View.transform.position,"ui");}i++;}
     GUI.EndScrollView();journalScroll[categories[c].id]=scroll;
    }
   }
   if(ghost){Text(new Rect(105,515,1280, 42),S.KnownStory?.title??"",28,cream,title:true);Text(new Rect(105,576,1260,100),S.KnownStory?.narrative??"",22,muted);Text(new Rect(105,724,1310,50),"Подберите предмет, комнату или звук для каждой детали истории. Прямые ответы недоступны.",17,gold);}
   else Text(new Rect(105,756,1350,35),"Выбор сохраняется сразу. Меняйте версию в любой момент. При равенстве очков важна скорость верных ответов.",15,sub);
  }
  void Pause(){
   Box(new Rect(0,0,W,H),new Color(.02f,.04f,.05f,.78f));Box(new Rect(515,151,570,606),panel);
   Text(new Rect(555,188,490, 54),"Пауза для размышлений",31,cream,title:true);Text(new Rect(555,251,490,40),"Сетевое расследование продолжается.",16,muted);
   if(Button(new Rect(555,310,490,52),"Вернуться в дом",true))PauseOpen=false;
   Text(new Rect(555,385,490,30),"Громкость",17,cream);volume=GUI.HorizontalSlider(new Rect(555,427,490,24),volume,0,1);AudioListener.volume=volume;
   Text(new Rect(555,466,490,30),"Чувствительность мыши",17,cream);D.Controller.Sensitivity=GUI.HorizontalSlider(new Rect(555,506,490,24),D.Controller.Sensitivity,.4f,4);
   if(S.IsHost&&Button(new Rect(555,559,490,51),"Завершить расследование")){S.FinishMatch();ClosePanels();}
   if(Button(new Rect(555,632,490, 49),"Покинуть дом")){PlayerPrefs.SetFloat("volume",volume);S.Disconnect();ClosePanels();}
  }
  void Help(){
   Box(new Rect(0,0,W,H),new Color(.02f,.04f,.05f,.92f));Text(new Rect(105,85,1190,68),"Научитесь слушать дом",43,cream,title:true);
   if(Button(new Rect(1320,92,180, 45),"Закрыть  /  F1"))HelpOpen=false;
   string[] headings={"01  Узнайте свою роль","02  Читайте знаки","03  Запишите версию"};
   string[] copy={"Призрак получает совместимую историю смерти. Только он знает убийцу, орудие, место, способ и мотив.\n\nДетективы свободно осматривают дом и обсуждают свои наблюдения.","Призрак двигает предметы, щёлкает светом, включает радио и воду. Направление, повторение и сочетания действий создают язык подсказок.\n\nЭнергия восстанавливается со временем.","Каждый Детектив ведёт собственный журнал: Tab. Выбор сохраняется немедленно и доступен для изменения.\n\nВ финале — по одному очку за верную деталь. При ничьей побеждает меньшая сумма времени фиксации верных ответов."};
   for(int i=0;i<3;i++){float x=105+i*480;Line(x,205,430);Text(new Rect(x,241,430,74),headings[i],25,gold,title:true);Text(new Rect(x,329,425,250),copy[i],20,cream);}
   Box(new Rect(105,623,1390,163),panel);
   Text(new Rect(136,647,1310,118),"W A S D — движение  ·  мышь — обзор  ·  E — взаимодействие  ·  Tab — журнал / история\nДетектив: Shift — бег, Ctrl — присесть, F — фонарик, Enter — текстовый чат\nПризрак: Space / Ctrl — вверх / вниз, ЛКМ / F — бросить, ПКМ — отпустить, R — дрожь, Q — полтергейст, C — тьма\nEsc — меню  ·  Голосовая связь — через привычный вам внешний сервис",17,muted);
  }
  void Results(){
   Box(new Rect(0,0,W,H),new Color(.025f,.045f,.05f,.94f));Brand();
   Text(new Rect(80,128,1400, 60),"Тишина больше не хранит тайну",41,cream,title:true);
   Text(new Rect(82,205,1390,75),S.KnownStory?.narrative??"Расследование завершено.",21,muted);
   var cats=S.Catalog.categories;
   for(int c=0;c<cats.Length;c++){float cw=1440f/cats.Length,x=80+c*cw;var a=S.KnownStory?.answers.FirstOrDefault(v=>v.categoryId==cats[c].id);string answer=cats[c].options.FirstOrDefault(o=>o.id==a?.optionId)?.label??"—";Box(new Rect(x,314,cw-16,111),panel);Text(new Rect(x+17,331,cw-50,27),cats[c].label.ToUpperInvariant(),12,gold);Text(new Rect(x+17,372,cw-46,40),answer,22,cream,title:true);}
   Text(new Rect(80,463,1410,34),"РЕЗУЛЬТАТЫ ДЕТЕКТИВОВ",13,gold);Text(new Rect(1150,463,350,34),"ОЧКИ     /     ВРЕМЯ ФИКСАЦИИ",12,muted,TextAnchor.MiddleRight);
   int rank=0;foreach(var score in S.Scores){float y=514+rank*44;Text(new Rect(80,y,56,35),(rank+1).ToString("00"),22,gold,title:true);Text(new Rect(145,y,710,35),score.name,21,cream);Text(new Rect(970,y,230,35),score.correct+" / "+cats.Length,22,score.correct==cats.Length?teal:cream,TextAnchor.MiddleRight);Text(new Rect(1260,y,240,35),score.tieTime.ToString("0.0")+" с",20,muted,TextAnchor.MiddleRight);Line(80,y+38,1420,new Color(.3f,.4f,.4f,.17f));rank++;}
   if(S.Scores.Count==0)Text(new Rect(80,528,1300,60),"Учебное дело закрыто. Соберите Детективов для совместного расследования.",21,muted);
   if(S.IsHost&&Button(new Rect(1047,807,453,58),"Новое расследование    →",true))S.ReturnToLobby();
   if(Button(new Rect(80,807,245,58),"В главное меню"))S.Disconnect();
   if(!S.IsHost)Text(new Rect(1047,817,453,60),"Ожидаем решения хозяина лобби",18,muted);
  }
  public static string FormatTime(float t){int s=Mathf.Max(0,Mathf.CeilToInt(t));return (s/60).ToString("00")+":"+(s%60).ToString("00");}
  string cachedAddress;
  string LocalAddress(){if(cachedAddress!=null)return cachedAddress;try{cachedAddress=string.Join(" / ",System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()).Where(a=>a.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork&&!System.Net.IPAddress.IsLoopback(a)).Select(a=>a.ToString()).Take(2));}catch{cachedAddress="127.0.0.1";}return cachedAddress;}
 }
}



