# Дом по ту сторону / VEIL HOUSE

Unity 6.3 (6000.3.24f1), Windows, built-in rendering. Original stylized nocturnal manor, legible warm/cool lighting, detailed authored procedural meshes, Russian UI.

## Implementation sequence
1. Data contracts, curated compatible stories, private journals, deterministic scoring.
2. Authoritative TCP host, 2–7 player lobby, role allocation, private story delivery, session/reconnect failure handling.
3. Furnished house and host-simulated interactive objects; energy costs and strong abilities.
4. First-person detective and flying ghost, humanoid detectives, spatial sound, full UI flow.
5. Unity compilation, Windows build, actual multi-process integration checks, captured render inspection and corrections.

## Modules
- Core: contracts and session orchestration.
- Networking: bounded framed TCP transport; all Unity work on main thread.
- Investigation: JSON catalog, validated combinations, private journal timestamps, scoring.
- World / Interaction: authored house, stable object IDs, physics authority and replicated snapshots.
- Characters: original rigged detective mesh and locomotion animation.
- Audio: original synthesized spatial ambience and interaction cues.
- Player: first-person control and input routing.
- UI: main menu, lobby, ghost dossier, personal journal, results, tutorial.
- Editor / Testing: scene creation, build, automated runtime probes and screenshots.

## Networking contract
GameSession provides I, IsHost, LocalId, Phase, Players, Status, TimeRemaining, LocalPlayer, Catalog, KnownStory, Journal, Scores, ChatLog, Port; Host(name,port), Join(name,address,port), Disconnect(), SetRole(role), StartMatch(), FinishMatch(), ReturnToLobby(), SubmitAnswer(category,option), SendPose(position,yaw,pitch,crouch,speed), RequestInteraction(objectId,action,target,direction), SendChat(text). Events: MatchStarted, MatchEnded; delegate Func<InteractionRequest,float> ValidateInteraction returns energy cost or -1; Action<InteractionRequest> InteractionAccepted; Func<ObjectState[]> CaptureWorld; Action<ObjectState[]> ApplyWorld; Action<int,string> WorldEvent. Core validates role, energy and throttle, world validates range/object/command. Ghost story never included in general snapshots. World snapshots 10Hz, player poses 15Hz. Detective chat only, no ghost free text. Direct IP/LAN/VPN; no cloud relay or built-in voice.

WorldBuilder.Build() returns GameObject, SpawnPoint(int) returns Vector3, RoomAt(Vector3) returns Russian room name. HauntedObject.All dictionary<int,HauntedObject>; Id, DisplayName, Kind, Cost; Capture(), Apply(ObjectState), ServerAct(InteractionRequest), SetAuthority(bool), static ResetRegistry(); Body Rigidbody; public bool Active; audio via event System.Action<Vector3,string> Sound.

DetectiveAvatar.Create(Transform parent,int appearance) returns DetectiveAvatar; Animate(float speed,bool crouch,bool interacting), SetVisible(bool). Soundscape has static Create() and PlayAt(Vector3,string), plus SetHaunting(float) optional.

The playable build and source are deliverables. Test reports state tested boundaries honestly; no claim of WAN/voice/AAA assets. All third-party dependencies and asset provenance documented.
