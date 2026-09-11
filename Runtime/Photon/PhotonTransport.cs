using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Multiuser;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace PixoVR.TrainingCore.Photon
{
    /// <summary>PUN 2 implementation of the abstract <see cref="Multiuser.NetworkManager"/>.</summary>
    public class PhotonNetworkManager : Multiuser.NetworkManager, IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
    {
        /// <summary>PUN app version / game version string.</summary>
        public string GameVersion = "1.0";

        /// <summary>Default room name when none is supplied.</summary>
        public string DefaultRoomName = "default";

        /// <summary>Max players per room.</summary>
        public byte MaxPlayers = 20;

        /// <inheritdoc/>
        public override bool IsMasterClient => PhotonNetwork.IsMasterClient;

        /// <summary>Connection callbacks wired in OnEnable.</summary>
        protected virtual void OnEnable()
        {
            PhotonNetwork.AddCallbackTarget(this);
        }

        /// <summary>Cleanup.</summary>
        protected virtual void OnDisable()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        /// <inheritdoc/>
        public override MultiuserPlayer GetMasterClient() => ToPlayer(PhotonNetwork.MasterClient);

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
            if (PhotonNetwork.IsConnected)
                PhotonNetwork.JoinLobby();
        }

        /// <inheritdoc/>
        public override void JoinRoom(string roomName)
        {
            PhotonNetwork.JoinOrCreateRoom(string.IsNullOrEmpty(roomName) ? DefaultRoomName : roomName,
                new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = true, IsOpen = true },
                TypedLobby.Default);
        }

        /// <inheritdoc/>
        public override void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }

        /// <inheritdoc/>
        public override void SetNewInstructor(MultiuserPlayer player)
        {
            InstructorControls?.SetHighlightState(player?.Id, true);
            OnInstructorChangedEvent?.Invoke(player);
            NetworkEvents.OnInstructorChanged?.Invoke(player);
        }

        /// <summary>Map a Photon player to a <see cref="MultiuserPlayer"/>.</summary>
        public static MultiuserPlayer ToPlayer(Player player)
        {
            if (player == null)
                return null;
            return new MultiuserPlayer
            {
                Id = player.UserId ?? player.ActorNumber.ToString(),
                Name = player.NickName,
                IsInstructor = player.IsMasterClient,
                Platform = "photon"
            };
        }

        // ---- PUN callbacks → abstract events ----

        /// <summary>PUN connected.</summary>
        public void OnConnected()
        {
            SetState(ConnectionState.Connected);
            OnConnectedEvent?.Invoke();
        }

        /// <summary>PUN connected to master.</summary>
        public void OnConnectedToMaster() { }

        /// <summary>PUN disconnected.</summary>
        public void OnDisconnected(DisconnectCause cause)
        {
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
            OnRoomJoinedEvent?.Invoke();
            InProgressRoomJoined();
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
        public void OnPlayerEnteredRoom(Player newPlayer) { }

        /// <summary>PUN player left.</summary>
        public void OnPlayerLeftRoom(Player otherPlayer) { }

        /// <summary>PUN room props changed.</summary>
        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) { }

        /// <summary>PUN player props changed.</summary>
        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps) { }

        /// <summary>PUN master switch.</summary>
        public void OnMasterClientSwitched(Player newMasterClient)
        {
            var player = ToPlayer(newMasterClient);
            OnMasterClientSwitchedEvent?.Invoke(player);
        }
    }

    /// <summary>PUN room property bag.</summary>
    public class PhotonRoom : Multiuser.Room
    {
        /// <inheritdoc/>
        public override string RoomName => PhotonNetwork.CurrentRoom?.Name;

        /// <inheritdoc/>
        public override GameModes.GameMode CurrentMode =>
            Enum.TryParse(GetProperty<string>("GameMode") ?? "", out GameModes.GameMode m)
                ? m : GameModes.GameMode.Training;

        /// <inheritdoc/>
        public override List<MultiuserPlayer> GetNetworkPlayers() =>
            PhotonNetwork.PlayerList?.Select(PhotonNetworkManager.ToPlayer).ToList()
            ?? new List<MultiuserPlayer>();

        /// <inheritdoc/>
        public override MultiuserPlayer GetLocalPlayer() =>
            PhotonNetworkManager.ToPlayer(PhotonNetwork.LocalPlayer);

        /// <inheritdoc/>
        public override MultiuserPlayer GetPlayer(string id) =>
            GetNetworkPlayers().FirstOrDefault(p => p.Id == id);

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
        public override event Action<MultiuserPlayer> OnPlayerJoin { add { } remove { } }

        /// <inheritdoc/>
        public override event Action<MultiuserPlayer> OnPlayerExit { add { } remove { } }

        /// <inheritdoc/>
        public override event Action<MultiuserPlayer> OnMasterUserSwitched { add { } remove { } }
    }

    /// <summary>PUN lobby: lists visible rooms.</summary>
    public class PhotonLobby : LobbyBase
    {
        private readonly List<string> _rooms = new List<string>();

        /// <inheritdoc/>
        public override List<string> GetRooms() => _rooms;

        /// <summary>Refresh from a PUN room list update.</summary>
        public void UpdateRooms(List<RoomInfo> roomList)
        {
            _rooms.Clear();
            foreach (var room in roomList ?? new List<RoomInfo>())
                if (room != null && !room.RemovedFromList)
                    _rooms.Add(room.Name);
        }
    }

    /// <summary>Photon-backed instructor controls.</summary>
    public class PhotonInstructorControls : NetworkInstructorControls
    {
        /// <inheritdoc/>
        public override void SetActiveState(string userId, bool state) =>
            RaiseInstructorEvent("setActive", userId, state);

        /// <inheritdoc/>
        public override void ToggleActiveState(string userId) =>
            RaiseInstructorEvent("toggleActive", userId, true);

        /// <inheritdoc/>
        public override void Remove(string userId) =>
            RaiseInstructorEvent("remove", userId, true);

        /// <inheritdoc/>
        public override void SetAudioState(string userId, bool muted) =>
            RaiseInstructorEvent("audio", userId, muted);

        /// <inheritdoc/>
        public override void SetAvatarState(string userId, bool visible) =>
            RaiseInstructorEvent("avatar", userId, visible);

        /// <inheritdoc/>
        public override void SetPlayerSpotCheckStatus(string userId, bool state) =>
            RaiseInstructorEvent("spotCheck", userId, state);

        /// <inheritdoc/>
        public override void SetValveNamesState(string userId, bool state) =>
            RaiseInstructorEvent("valveNames", userId, state);

        /// <inheritdoc/>
        public override void SetAudioStateAll(bool muted) =>
            RaiseInstructorEvent("audioAll", null, muted);

        /// <inheritdoc/>
        public override void SetAvatarStateAll(bool visible) =>
            RaiseInstructorEvent("avatarAll", null, visible);

        /// <inheritdoc/>
        public override void SetHighlightState(string userId, bool highlighted) =>
            RaiseInstructorEvent("highlight", userId, highlighted);

        private static void RaiseInstructorEvent(string action, string userId, bool state)
        {
            if (!PhotonNetwork.InRoom)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.InstructorEventCode,
                new object[] { action, userId ?? "", state },
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable);
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

        /// <summary>EventSyncData → a generic InteractionEventArgs.</summary>
        public static InteractionEventArgs FromSyncData(EventSyncData data)
        {
            if (data == null)
                return null;
            return new GenericInteractionEventArgs(data.SubjectId, data.EventType, data.Payload);
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
                default:
                    return new byte[0];
            }
        }
    }

    /// <summary>PUN component that republishes received RaiseEvent interaction payloads to the <see cref="EventBus"/>.</summary>
    public class PhotonSyncView : MonoBehaviourPun, IOnEventCallback
    {
        /// <summary>Republish Photon events into the local event bus.</summary>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != PhotonEventSerializer.InteractionEventCode)
                return;
            var data = PhotonEventSerializer.DeserializeEventSyncData(photonEvent.CustomData as object[]);
            var args = PhotonEventSerializer.FromSyncData(data);
            if (args != null && !string.IsNullOrEmpty(args.SubjectId))
                EventBus.Instance.Publish(args.SubjectId, args);
        }

        /// <summary>Broadcast a local interaction event to the room.</summary>
        public void SendInteraction(InteractionEventArgs args)
        {
            if (!PhotonNetwork.InRoom)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.InteractionEventCode,
                PhotonEventSerializer.Serialize(args),
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);
        }
    }

    /// <summary>Freeze behaviour wired to the local player's XR rig.</summary>
    public class PhotonFreezeBehaviour : MonoBehaviour, IFreezeBehaviour
    {
        /// <summary>Objects disabled while frozen.</summary>
        public List<GameObject> DisableWhileFrozen = new List<GameObject>();

        /// <summary>Frozen state.</summary>
        public bool IsFrozen { get; private set; }

        /// <inheritdoc/>
        public void FreezePlayer(bool freeze)
        {
            IsFrozen = freeze;
            foreach (var go in DisableWhileFrozen)
                if (go != null)
                    go.SetActive(!freeze);
        }
    }
}
