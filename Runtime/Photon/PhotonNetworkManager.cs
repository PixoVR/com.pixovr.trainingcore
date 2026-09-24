using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Multiuser;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PixoVR.TrainingCore.Photon
{
    /// <summary>PUN 2 implementation of the abstract <see cref="Multiuser.NetworkManager"/>.</summary>
    public class PhotonNetworkManager : Multiuser.NetworkManager, IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
    {
        /// <summary>PUN app version / game version string.</summary>
        public string GameVersion = "1.0";

        /// <summary>Default room name when none is supplied.</summary>
        public string DefaultRoomName = "default";

        /// <summary>Max players per room.</summary>
        public byte MaxPlayers = 20;

        /// <inheritdoc/>
        public override bool IsMasterClient => PhotonNetwork.IsMasterClient;

        /// <summary>Queued lobby join while the master-server connection is pending.</summary>
        private bool joinLobbyPending;

        /// <summary>In-progress joiner waiting for the module graph before requesting catch-up.</summary>
        private bool pendingCatchUp;

        /// <summary>Connection callbacks wired in OnEnable.</summary>
        protected virtual void OnEnable()
        {
            var controls = GetComponent<NetworkInstructorControls>();
            if (controls != null)
                InstructorControls = controls;
            else
                Log.Warning("PhotonNetworkManager: no NetworkInstructorControls on object", LogCategory.Multiuser);
            FlowEventHandler = GetComponent<IFlowEventHandler>();
            if (FlowEventHandler == null)
                Log.Warning("PhotonNetworkManager: no IFlowEventHandler on object", LogCategory.Multiuser);
            Flow.GraphFlowManager.GraphStarted += OnGraphStartedForCatchUp;
            XRI.NetworkGrabManager.NetworkGrabbed += RelayGrab;
            XRI.NetworkGrabManager.NetworkReleased += RelayRelease;
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <summary>Cleanup.</summary>
        protected virtual void OnDisable()
        {
            Flow.GraphFlowManager.GraphStarted -= OnGraphStartedForCatchUp;
            XRI.NetworkGrabManager.NetworkGrabbed -= RelayGrab;
            XRI.NetworkGrabManager.NetworkReleased -= RelayRelease;
            XRI.LostObjectManager.ExternalOwnershipGate = null;
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void RelayGrab(XRI.NetworkGrabManager mgr)
        {
            if (!PhotonNetwork.InRoom || mgr == null)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.GrabSyncEventCode,
                new object[] { true, mgr.gameObject.GetGuidString(), PhotonNetwork.LocalPlayer.ActorNumber },
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);
        }

        private void RelayRelease(XRI.NetworkGrabManager mgr)
        {
            if (!PhotonNetwork.InRoom || mgr == null)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.GrabSyncEventCode,
                new object[] { false, mgr.gameObject.GetGuidString(), PhotonNetwork.LocalPlayer.ActorNumber },
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable);
        }

        /// <inheritdoc/>
        public override MultiuserPlayer GetMasterClient => ToPlayer(PhotonNetwork.MasterClient);

        /// <inheritdoc/>
        public override void AttemptConnection()
        {
            SetState(ConnectionState.AtteptingConnection);
            PhotonNetwork.GameVersion = GameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <inheritdoc/>
        public override void Disconnect()
        {
            if (PhotonNetwork.IsConnected)
                PhotonNetwork.Disconnect();
        }

        /// <inheritdoc/>
        public override void JoinLobby()
        {
            if (PhotonNetwork.IsConnectedAndReady)
                PhotonNetwork.JoinLobby();
            else
                joinLobbyPending = true;
        }

        /// <inheritdoc/>
        public override void JoinRoom(string roomName)
        {
            var properties = new Hashtable
            {
                [Multiuser.Room.SceneNameProperty] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                ["GameMode"] = GameModes.GameModeManager.CurrentMode.ToString()
            };
            PhotonNetwork.JoinOrCreateRoom(string.IsNullOrEmpty(roomName) ? DefaultRoomName : roomName,
                new RoomOptions
                {
                    MaxPlayers = MaxPlayers,
                    IsVisible = true,
                    IsOpen = true,
                    CustomRoomProperties = properties,
                    CustomRoomPropertiesForLobby = new[] { Multiuser.Room.SceneNameProperty, "GameMode" }
                },
                TypedLobby.Default);
        }

        /// <inheritdoc/>
        public override void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                InstructorControls?.SetActiveState(CurrentRoom?.GetLocalPlayer, true);
                PhotonNetwork.LeaveRoom();
            }
        }

        /// <inheritdoc/>
        public override void SetNewInstructor(MultiuserPlayer player)
        {
            InstructorControls?.SetHighlightState(player, true);
            OnInstructorChangedEvent?.Invoke(player);
            NetworkEvents.OnInstructorChanged?.Invoke(player);
        }

        /// <summary>OAuth token response payload (auth header fetch).</summary>
        public class TokenResponse
        {
            /// <summary>Access token.</summary>
            public string access_token;
            /// <summary>Token type.</summary>
            public string token_type;
            /// <summary>Expiry seconds.</summary>
            public int expires_in;
            /// <summary>Scope.</summary>
            public string scope;
        }

        /// <summary>State payload wrapper for RaiseEvent sync messages.</summary>
        public class StateHolder
        {
            /// <summary>Serialized state.</summary>
            public string State;
        }

        /// <summary>Map a Photon player to a <see cref="MultiuserPlayer"/>.</summary>
        public static MultiuserPlayer ToPlayer(Player player)
        {
            if (player == null)
                return null;
            return new MultiuserPlayer(player.ActorNumber, Data.PlayerPlatform.VR,
                name: player.NickName ?? "", instructor: player.IsMasterClient);
        }

        // ---- PUN callbacks → abstract events ----

        /// <summary>PUN connected.</summary>
        public void OnConnected() { }

        /// <summary>PUN connected to master.</summary>
        public void OnConnectedToMaster()
        {
            SetState(ConnectionState.Connected);
            OnConnectedEvent?.Invoke();
            if (joinLobbyPending)
            {
                joinLobbyPending = false;
                PhotonNetwork.JoinLobby();
            }
        }

        /// <summary>PUN disconnected.</summary>
        public void OnDisconnected(DisconnectCause cause)
        {
            CurrentRoom = null;
            SetState(ConnectionState.Disconnected);
            OnDisconnectedEvent?.Invoke(cause.ToString());
        }

        /// <summary>PUN region list.</summary>
        public void OnRegionListReceived(RegionHandler regionHandler) { }

        /// <summary>PUN custom auth response.</summary>
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }

        /// <summary>PUN auth failure.</summary>
        public void OnCustomAuthenticationFailed(string debugMessage) =>
            OnConnectionFailedEvent?.Invoke();

        /// <summary>PUN lobby joined.</summary>
        public void OnJoinedLobby()
        {
            CurrentLobby = new PhotonLobby();
            OnLobbyJoinedEvent?.Invoke();
        }

        /// <summary>PUN lobby left.</summary>
        public void OnLeftLobby() { }

        /// <summary>PUN create failed.</summary>
        public void OnCreateRoomFailed(short returnCode, string message) =>
            OnConnectionFailedEvent?.Invoke();

        /// <summary>PUN room created.</summary>
        public void OnCreatedRoom() { }

        /// <summary>PUN room joined.</summary>
        public void OnJoinedRoom()
        {
            CurrentRoom = new PhotonRoom();
            XRI.LostObjectManager.ExternalOwnershipGate = obj =>
                PhotonNetwork.InRoom && obj.GetComponent<PhotonView>() is var pv && pv != null && !pv.IsMine;
            OnRoomJoinedEvent?.Invoke();
        }

        /// <summary>PUN join failed.</summary>
        public void OnJoinRoomFailed(short returnCode, string message) =>
            OnConnectionFailedEvent?.Invoke();

        /// <summary>PUN random join failed → create.</summary>
        public void OnJoinRandomFailed(short returnCode, string message) => JoinRoom(DefaultRoomName);

        /// <summary>PUN room left.</summary>
        public void OnLeftRoom()
        {
            CurrentRoom = null;
            XRI.LostObjectManager.ExternalOwnershipGate = null;
            OnRoomExitedEvent?.Invoke();
        }

        /// <summary>PUN friend list.</summary>
        public void OnFriendListUpdate(List<FriendInfo> friendList) { }

        /// <summary>PUN lobby stats.</summary>
        public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }

        /// <summary>PUN room list.</summary>
        public void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (CurrentLobby is PhotonLobby lobby)
                lobby.UpdateRooms(roomList);
        }

        /// <summary>PUN player entered.</summary>
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            var player = (CurrentRoom as PhotonRoom)?.CachePlayer(newPlayer) ?? ToPlayer(newPlayer);
            if (CurrentRoom is PhotonRoom room)
                room.NotifyPlayerJoin(player);
            OnPlayerJoinedEvent?.Invoke(player);
            if (Interactions.NetworkInfoPointManager.Instance != null)
                Interactions.NetworkInfoPointManager.Instance.UpdateNewPlayerOfStatus();
        }

        /// <inheritdoc/>
        public override void RequestOwnership(GameObject go, IEnumerable<MonoBehaviour> additional)
        {
            go?.GetComponent<PhotonView>()?.RequestOwnership();
            if (additional == null)
                return;
            foreach (var view in additional)
            {
                if (view is PhotonView pv)
                    pv.RequestOwnership();
            }
        }

        /// <inheritdoc/>
        public override void SendInfoPointState(string guid, string action, bool state)
        {
            if (!PhotonNetwork.InRoom)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.InfoPointEventCode,
                new object[] { guid, action, state },
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);
        }

        /// <summary>PUN RaiseEvent dispatch.</summary>
        public void OnEvent(EventData photonEvent)
        {
            var payload = photonEvent.CustomData as object[];
            switch (photonEvent.Code)
            {
                case PhotonEventSerializer.InfoPointEventCode:
                    if (payload != null && payload.Length >= 3
                        && payload[0] is string guid && payload[2] is bool state)
                        Interactions.NetworkInfoPointManager.Instance?.ReceiveSetInfoPointEnableState(guid, state);
                    break;
                case PhotonEventSerializer.InstructorEventCode:
                    if (photonEvent.Sender != PhotonNetwork.MasterClient?.ActorNumber)
                        break;
                    if (payload != null && payload.Length >= 3
                        && payload[0] is string action && payload[2] is bool flag)
                        (InstructorControls as PhotonInstructorControls)?.ReceiveInstructorEvent(action, payload[1] as string ?? "", flag);
                    break;
                case PhotonEventSerializer.GrabSyncEventCode:
                    if (payload != null && payload.Length >= 2
                        && payload[0] is bool held && payload[1] is string grabGuid)
                    {
                        var target = Identity.GuidRegistry.Resolve(grabGuid);
                        target?.GetComponent<XRI.NetworkGrabManager>()?.SetRemotelyHeld(held, photonEvent.Sender);
                    }
                    break;
                case PhotonEventSerializer.StepSyncEventCode:
                    if (payload != null && payload.Length >= 1 && payload[0] is string syncAction
                        && syncAction == "catchUp" && photonEvent.Sender != PhotonNetwork.MasterClient?.ActorNumber)
                        break;
                    HandleStepSync(payload, photonEvent.Sender);
                    break;
            }
        }

        private void HandleStepSync(object[] payload, int sender)
        {
            if (payload == null || payload.Length == 0 || !(payload[0] is string action))
                return;
            if (action == "catchUpRequest" && IsMasterClient && payload.Length >= 2
                && sender != PhotonNetwork.LocalPlayer.ActorNumber)
            {
                int requester = sender;
                var history = Events.EventBus.Instance.History;
                var events = new object[history.Count + 2];
                events[0] = "catchUp";
                events[1] = Flow.GraphFlowManager.Instance?.ActiveFlow?.CurrentSteps?.FirstOrDefault()?.GUID ?? "";
                for (int i = 0; i < history.Count; i++)
                    events[i + 2] = PhotonEventSerializer.Serialize(history[i]);
                PhotonNetwork.RaiseEvent(PhotonEventSerializer.StepSyncEventCode, events,
                    new RaiseEventOptions { TargetActors = new[] { requester } },
                    SendOptions.SendReliable);
            }
            else if (action == "catchUp" && payload.Length >= 2)
            {
                var stepGuid = payload[1] as string;
                Events.EventBus.Instance.ClearHistory();
                for (int i = 2; i < payload.Length; i++)
                {
                    var data = PhotonEventSerializer.DeserializeEventSyncData(payload[i] as object[]);
                    var args = PhotonEventSerializer.FromSyncData(data);
                    if (args != null && !string.IsNullOrEmpty(args.SubjectId))
                    {
                        args.IsRemote = true;
                        Events.EventBus.Instance.Publish(args.SubjectId, args);
                    }
                }
                if (!string.IsNullOrEmpty(stepGuid))
                    Flow.GraphFlowManager.Instance?.SkipToStep(stepGuid);
                EndSync();
            }
        }

        /// <inheritdoc/>
        public override void InProgressRoomJoined(string sceneToLoad)
        {
            if (!PhotonNetwork.InRoom)
                return;
            Sync();
            pendingCatchUp = true;
            if (PhotonNetwork.IsMasterClient)
            {
                pendingCatchUp = false;
                EndSync();
                return;
            }
        }

        private void OnGraphStartedForCatchUp()
        {
            if (!pendingCatchUp || !PhotonNetwork.InRoom)
                return;
            pendingCatchUp = false;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.StepSyncEventCode,
                new object[] { "catchUpRequest", PhotonNetwork.LocalPlayer.ActorNumber },
                new RaiseEventOptions { TargetActors = new[] { PhotonNetwork.MasterClient.ActorNumber } },
                SendOptions.SendReliable);
        }

        /// <inheritdoc/>
        public override void ResetPlayer(MultiuserPlayer player)
        {
            if (player == null)
                return;
            InstructorControls?.SetActiveState(player, true);
            InstructorControls?.SetAudioState(player, false);
            InstructorControls?.SetAvatarState(player, true);
            InstructorControls?.SetPlayerSpotCheckStatus(player, false);
            InstructorControls?.SetValveNamesState(player, false);
            InstructorControls?.SetHighlightState(player, false);
        }

        /// <inheritdoc/>
        public override void ResetPlayer() => ResetPlayer(CurrentRoom?.GetLocalPlayer);

        /// <summary>PUN player left.</summary>
        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            var player = (CurrentRoom as PhotonRoom)?.CachePlayer(otherPlayer) ?? ToPlayer(otherPlayer);
            if (CurrentRoom is PhotonRoom room)
                room.NotifyPlayerExit(player);
            OnPlayerLeftEvent?.Invoke(player);
            if (player != null)
                foreach (var mgr in XRI.NetworkGrabManager.Instances)
                    mgr.ClearRemoteHoldFor(player.Id);
        }

        /// <summary>PUN room props changed.</summary>
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }

        /// <summary>PUN player props changed.</summary>
        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        /// <summary>PUN master switch.</summary>
        public void OnMasterClientSwitched(Player newMasterClient)
        {
            var player = (CurrentRoom as PhotonRoom)?.CachePlayer(newMasterClient) ?? ToPlayer(newMasterClient);
            if (CurrentRoom is PhotonRoom room)
            {
                foreach (var p in room.GetNetworkPlayers())
                    p.IsInstructor = false;
                if (player != null)
                    player.IsInstructor = true;
                room.NotifyMasterSwitched(player);
            }
            OnMasterClientSwitchedEvent?.Invoke(player);
        }
    }

    /// <summary>PUN room property bag.</summary>
    public class PhotonRoom : Multiuser.Room
    {
        private readonly Dictionary<int, MultiuserPlayer> players = new Dictionary<int, MultiuserPlayer>();
        private Action<Multiuser.Room, MultiuserPlayer> onPlayerJoin;
        private Action<Multiuser.Room, MultiuserPlayer> onPlayerExit;
        private Action<MultiuserPlayer> onMasterSwitched;

        /// <summary>Cached player entry; creates one if absent.</summary>
        internal MultiuserPlayer CachePlayer(Player player)
        {
            if (player == null)
                return null;
            if (!players.TryGetValue(player.ActorNumber, out var mp) || mp == null)
            {
                mp = PhotonNetworkManager.ToPlayer(player);
                players[player.ActorNumber] = mp;
            }
            return mp;
        }

        /// <summary>Raise <see cref="OnPlayerJoin"/>.</summary>
        internal void NotifyPlayerJoin(MultiuserPlayer player) => onPlayerJoin?.Invoke(this, player);

        /// <summary>Raise <see cref="OnPlayerExit"/> and drop the cached entry.</summary>
        internal void NotifyPlayerExit(MultiuserPlayer player)
        {
            onPlayerExit?.Invoke(this, player);
            if (player != null)
                players.Remove(player.Id);
        }

        /// <summary>Raise <see cref="OnMasterUserSwitched"/>.</summary>
        internal void NotifyMasterSwitched(MultiuserPlayer player) => onMasterSwitched?.Invoke(player);

        /// <inheritdoc/>
        public override string RoomName => PhotonNetwork.CurrentRoom?.Name;

        /// <inheritdoc/>
        public override GameModes.GameMode CurrentMode =>
            Enum.TryParse(GetProperty<string>("GameMode") ?? "", out GameModes.GameMode m)
                ? m : GameModes.GameMode.Training;

        /// <inheritdoc/>
        public override List<MultiuserPlayer> GetNetworkPlayers() =>
            PhotonNetwork.PlayerList?.Select(CachePlayer).Where(p => p != null).ToList()
            ?? new List<MultiuserPlayer>();

        /// <inheritdoc/>
        public override MultiuserPlayer GetLocalPlayer =>
            CachePlayer(PhotonNetwork.LocalPlayer);

        /// <inheritdoc/>
        public override MultiuserPlayer GetPlayer(int playerId) =>
            players.TryGetValue(playerId, out var mp) ? mp
                : GetNetworkPlayers().FirstOrDefault(p => p.Id == playerId);

        /// <inheritdoc/>
        public override void RemovePlayer(int playerId)
        {
            if (players.TryGetValue(playerId, out var removed) && players.Remove(playerId))
                onPlayerExit?.Invoke(this, removed);
        }

        /// <inheritdoc/>
        public override void AddProperty(string key, object value) => SetProperty(key, value);

        /// <inheritdoc/>
        public override void RemoveProperty(string key)
        {
            PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable { [key] = null });
        }

        /// <inheritdoc/>
        public override void SetProperty(string key, object value)
        {
            PhotonNetwork.CurrentRoom?.SetCustomProperties(new Hashtable { [key] = value });
        }

        /// <inheritdoc/>
        public override object GetProperty(string key) =>
            PhotonNetwork.CurrentRoom?.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(key, out var v)
                ? v : null;

        /// <inheritdoc/>
        public override bool HasProperty(string key) =>
            PhotonNetwork.CurrentRoom?.CustomProperties?.ContainsKey(key) == true;

        /// <inheritdoc/>
        public override event Action<Multiuser.Room, MultiuserPlayer> OnPlayerJoin
        {
            add => onPlayerJoin += value;
            remove => onPlayerJoin -= value;
        }

        /// <inheritdoc/>
        public override event Action<Multiuser.Room, MultiuserPlayer> OnPlayerExit
        {
            add => onPlayerExit += value;
            remove => onPlayerExit -= value;
        }

        /// <inheritdoc/>
        public override event Action<MultiuserPlayer> OnMasterUserSwitched
        {
            add => onMasterSwitched += value;
            remove => onMasterSwitched -= value;
        }
    }

    /// <summary>PUN lobby: lists visible rooms.</summary>
    public class PhotonLobby : LobbyBase
    {
        private readonly List<string> _rooms = new List<string>();
        private readonly Dictionary<string, RoomInfo> _roomInfos = new Dictionary<string, RoomInfo>();

        /// <inheritdoc/>
        public override List<string> GetRooms() => _rooms;

        /// <inheritdoc/>
        public override object GetPropertyForRoom(string roomName, string propertyName) =>
            _roomInfos.TryGetValue(roomName, out var info) && info?.CustomProperties != null
            && info.CustomProperties.TryGetValue(propertyName, out var value)
                ? value : null;

        /// <summary>Refresh from a PUN room list update.</summary>
        public void UpdateRooms(List<RoomInfo> roomList)
        {
            foreach (var room in roomList ?? new List<RoomInfo>())
            {
                if (room == null)
                    continue;
                if (room.RemovedFromList)
                    _roomInfos.Remove(room.Name);
                else
                    _roomInfos[room.Name] = room;
            }
            _rooms.Clear();
            _rooms.AddRange(_roomInfos.Keys);
        }
    }


    /// <summary>Serializes <see cref="EventSyncData"/>/<see cref="InteractionEventArgs"/> to PUN event payloads.</summary>
    public static class PhotonEventSerializer
    {
        /// <summary>RaiseEvent code for interaction events.</summary>
        public const byte InteractionEventCode = 1;

        /// <summary>RaiseEvent code for instructor commands.</summary>
        public const byte InstructorEventCode = 2;

        /// <summary>RaiseEvent code for step sync.</summary>
        public const byte StepSyncEventCode = 3;

        /// <summary>RaiseEvent code for info-point state sync.</summary>
        public const byte InfoPointEventCode = 4;

        /// <summary>RaiseEvent code for grab/release state sync.</summary>
        public const byte GrabSyncEventCode = 5;

        /// <summary>InteractionEventArgs → object[] payload.</summary>
        public static object[] Serialize(InteractionEventArgs args)
        {
            var data = ToSyncData(args);
            return new object[]
            {
                data.SubjectId, data.EventType, data.Payload ?? "", data.ActorId ?? "",
                data.Timestamp, data.Data ?? new byte[0]
            };
        }

        /// <summary>object[] payload → InteractionEventArgs.</summary>
        public static EventSyncData DeserializeEventSyncData(object[] payload)
        {
            if (payload == null || payload.Length < 6)
                return null;
            return new EventSyncData
            {
                SubjectId = payload[0] as string,
                EventType = payload[1] as string,
                Payload = payload[2] as string,
                ActorId = payload[3] as string,
                Timestamp = payload[4] is long l ? l : Convert.ToInt64(payload[4]),
                Data = payload[5] as byte[]
            };
        }

        /// <summary>InteractionEventArgs → EventSyncData.</summary>
        public static EventSyncData ToSyncData(InteractionEventArgs args)
        {
            if (args == null)
                return null;
            return new EventSyncData
            {
                SubjectId = args.SubjectId,
                EventType = args.GetType().Name,
                Payload = args.ToString(),
                ActorId = PhotonNetwork.LocalPlayer?.UserId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Data = SerializeArgsBody(args)
            };
        }

        /// <summary>EventSyncData → the typed InteractionEventArgs.</summary>
        public static InteractionEventArgs FromSyncData(EventSyncData data)
        {
            if (data == null)
                return null;
            float floatBody() => data.Data != null && data.Data.Length >= 4 ? BitConverter.ToSingle(data.Data, 0) : 0f;
            switch (data.EventType)
            {
                case nameof(GrabInteractionEventArgs):
                    return new GrabInteractionEventArgs(null) { SubjectId = data.SubjectId };
                case nameof(TapInteractionEventArgs):
                    return new TapInteractionEventArgs(null, floatBody()) { SubjectId = data.SubjectId };
                case nameof(SnapInteractionEventArgs):
                    int snapId = data.Data != null && data.Data.Length >= 4 ? BitConverter.ToInt32(data.Data, 0) : 0;
                    int snapzoneId = data.Data != null && data.Data.Length >= 8 ? BitConverter.ToInt32(data.Data, 4) : 0;
                    var snappable = Interactions.SnappableRegistry.SnappableList
                        .FirstOrDefault(s => s != null && s.SnapId == snapId);
                    var snapzone = UnityEngine.Object.FindObjectsOfType<Interactions.Snapzone>()
                        .FirstOrDefault(z => z != null && z.SnapzoneID == snapzoneId);
                    return new SnapInteractionEventArgs(null, snappable, snapzone) { SubjectId = data.SubjectId };
                case nameof(UseInteractionEventArgs):
                    return new UseInteractionEventArgs(null, null, floatBody()) { SubjectId = data.SubjectId };
                case nameof(ValveTurnEventArgs):
                    return new ValveTurnEventArgs(null, floatBody(), true) { SubjectId = data.SubjectId };
                case nameof(TeleportEventArgs):
                    return new TeleportEventArgs(null, null, Vector3.zero, Quaternion.identity)
                    {
                        SubjectId = data.SubjectId,
                        teleportedObjectGuid = data.Data != null ? System.Text.Encoding.UTF8.GetString(data.Data) : null
                    };
                case nameof(GazeInteractionEventArgs):
                    return new GazeInteractionEventArgs(null, floatBody()) { SubjectId = data.SubjectId };
                case nameof(QuestionInteractionEventArgs):
                    return new QuestionInteractionEventArgs(data.SubjectId,
                        data.Data != null && data.Data.Length > 0 && data.Data[0] != 0);
                default:
                    return new GenericInteractionEventArgs(data.SubjectId, data.EventType, data.Payload);
            }
        }

        private static byte[] SerializeArgsBody(InteractionEventArgs args)
        {
            switch (args)
            {
                case TapInteractionEventArgs tap:
                    return BitConverter.GetBytes(tap.TapDuration);
                case ValveTurnEventArgs valve:
                    return BitConverter.GetBytes(valve.RotationAmount);
                case TeleportEventArgs teleport:
                    return System.Text.Encoding.UTF8.GetBytes(teleport.teleportedObjectGuid ?? "");
                case UseInteractionEventArgs use:
                    return BitConverter.GetBytes(use.Duration);
                case GazeInteractionEventArgs gaze:
                    return BitConverter.GetBytes(gaze.GazeDuration);
                case SnapInteractionEventArgs snap:
                    var body = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(snap.SnappedObject != null ? snap.SnappedObject.SnapId : 0), 0, body, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(snap.Snapzone != null ? snap.Snapzone.SnapzoneID : 0), 0, body, 4, 4);
                    return body;
                case QuestionInteractionEventArgs question:
                    return new byte[] { question.Correct ? (byte)1 : (byte)0 };
                default:
                    return new byte[0];
            }
        }
    }


    /// <summary>Freeze behaviour that syncs scene freezing over Photon (RaiseEvent 180).</summary>
    public class PhotonFreezeBehaviour : IFreezeBehaviour, IOnEventCallback
    {
        private const int FreezeEvents = 180;
        private readonly MonoBehaviour parent;
        private readonly IFreezeBehaviour internalFreezeBehaviour;
        private bool isFrozen;
        private bool initiatedFreeze;
        private readonly bool hasTimeout;
        private readonly float timeoutDuration;

        /// <summary>Create with a backend freeze behaviour.</summary>
        public PhotonFreezeBehaviour(MonoBehaviour creator, IFreezeBehaviour freezeBehaviour)
        {
            parent = creator;
            internalFreezeBehaviour = freezeBehaviour;
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <summary>Create with a backend freeze behaviour and auto-unfreeze timeout.</summary>
        public PhotonFreezeBehaviour(MonoBehaviour creator, IFreezeBehaviour freezeBehaviour, float timeout)
            : this(creator, freezeBehaviour)
        {
            hasTimeout = true;
            timeoutDuration = timeout;
        }

        /// <summary>Sends the unfreeze event if this user initiated a freeze and is being destroyed.</summary>
        ~PhotonFreezeBehaviour()
        {
            if (isFrozen && initiatedFreeze)
                Unfreeze();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <inheritdoc/>
        public override void Freeze()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.InRoom)
            {
                PhotonNetwork.RaiseEvent(FreezeEvents, true, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
                initiatedFreeze = true;
            }
            FreezeInternal();
        }

        private void FreezeInternal()
        {
            if (isFrozen)
                return;
            if (internalFreezeBehaviour != null)
            {
                internalFreezeBehaviour.IsJoiningUser = IsJoiningUser;
                internalFreezeBehaviour.Freeze();
            }
            isFrozen = true;
            if (hasTimeout && parent != null)
                parent.StartCoroutine(AutoUnfreeze());
        }

        private System.Collections.IEnumerator AutoUnfreeze()
        {
            yield return new WaitForSecondsRealtime(timeoutDuration);
            UnfreezeInternal(true);
        }

        /// <inheritdoc/>
        public override void Unfreeze()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.InRoom)
            {
                PhotonNetwork.RaiseEvent(FreezeEvents, false, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
                initiatedFreeze = false;
            }
            UnfreezeInternal();
        }

        private void UnfreezeInternal(bool timeout = false)
        {
            if (!isFrozen)
                return;
            if (internalFreezeBehaviour != null)
            {
                internalFreezeBehaviour.IsJoiningUser = IsJoiningUser;
                internalFreezeBehaviour.Unfreeze();
            }
            isFrozen = false;
        }

        /// <summary>Photon event callback.</summary>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != FreezeEvents)
                return;
            if (photonEvent.CustomData is bool freeze)
            {
                if (freeze)
                    FreezeInternal();
                else
                    UnfreezeInternal();
            }
        }
    }
}