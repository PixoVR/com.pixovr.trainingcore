using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>Abstract lobby provider (lists/joins rooms).</summary>
    public abstract class LobbyBase
    {
        /// <summary>Get a lobby-listed room property.</summary>
        public virtual object GetPropertyForRoom(string roomName, string propertyName) => null;

        /// <summary>Available rooms.</summary>
        public abstract List<string> GetRooms();
    }

    /// <summary>
    /// Transport-agnostic network manager. Concrete transports (Photon) subclass this;
    /// public member names match the legacy plugin so call sites migrate mechanically.
    /// </summary>
    public abstract class NetworkManager : SingletonBehaviour<NetworkManager>
    {
        /// <summary>Fired when a room is joined.</summary>
        public UnityEngine.Events.UnityEvent OnRoomJoinedEvent;
        /// <summary>Fired when the room is exited.</summary>
        public UnityEngine.Events.UnityEvent OnRoomExitedEvent;
        /// <summary>Fired when a lobby is joined.</summary>
        public UnityEngine.Events.UnityEvent OnLobbyJoinedEvent;
        /// <summary>Fired when connected to the network backend.</summary>
        public UnityEngine.Events.UnityEvent OnConnectedEvent;
        /// <summary>Fired on disconnect; carries the reason.</summary>
        public UnityStringEvent OnDisconnectedEvent;
        /// <summary>Fired when a connection attempt fails.</summary>
        public UnityEngine.Events.UnityEvent OnConnectionFailedEvent;
        /// <summary>Fired when the master client switches.</summary>
        public UnityMultiuserEvent OnMasterClientSwitchedEvent;
        /// <summary>Fired when the instructor changes.</summary>
        public UnityMultiuserEvent OnInstructorChangedEvent;

        /// <summary>Current lobby provider.</summary>
        public LobbyBase CurrentLobby { get; protected set; }

        /// <summary>Instructor-facing controls for the current room.</summary>
        public InstructorControls InstructorControls { get; protected set; }

        /// <summary>Freeze behaviour applied to the local player.</summary>
        public IFreezeBehaviour freezeBehaviour;

        /// <summary>Step-list handler used when (re)joining an in-progress session.</summary>
        public IFlowEventHandler FlowEventHandler;

        /// <summary>Current connection state.</summary>
        public ConnectionState State { get; protected set; } = ConnectionState.Disconnected;

        /// <summary>True while connected.</summary>
        public virtual bool IsConnected => State == ConnectionState.Connected;

        /// <summary>True while inside a room.</summary>
        public virtual bool InRoom => CurrentRoom != null;

        /// <summary>True while a late-join resync is running.</summary>
        public bool IsSyncing { get; protected set; }

        /// <summary>The current room, or null.</summary>
        public Room CurrentRoom { get; protected set; }

        /// <summary>True when the local player is the room's master client.</summary>
        public abstract bool IsMasterClient { get; }

        /// <summary>Get the current master client player.</summary>
        public abstract MultiuserPlayer GetMasterClient { get; }

        /// <summary>True when the local player is the instructor.</summary>
        public bool IsInstructor()
        {
            var player = CurrentRoom?.GetLocalPlayer;
            return player != null && player.IsInstructor;
        }

        /// <summary>Install a lobby provider.</summary>
        public void SetLobbyProvider(LobbyBase lobby) => CurrentLobby = lobby;

        /// <summary>Connect to the backend.</summary>
        public abstract void AttemptConnection();

        /// <summary>Disconnect from the backend.</summary>
        public abstract void Disconnect();

        /// <summary>Join the lobby.</summary>
        public abstract void JoinLobby();

        /// <summary>Join a room by name.</summary>
        public abstract void JoinRoom(string roomName);

        /// <summary>Leave the current room.</summary>
        public abstract void LeaveRoom();

        /// <summary>Promote a player to instructor.</summary>
        public abstract void SetNewInstructor(MultiuserPlayer player);

        /// <summary>Subscribe to graph-execution events (wave 2 implementation calls this).</summary>
        public virtual void RegisterGraphListeners() { }

        /// <summary>Subscribe to network transport events.</summary>
        public virtual void RegisterNetworkListeners() { }

        /// <summary>Reset the local player's state on room exit.</summary>
        public virtual void ResetPlayer() { }

        /// <summary>Reset a remote player's controls/position.</summary>
        public virtual void ResetPlayer(MultiuserPlayer player) { }

        /// <summary>Replicate a spawned object to peers.</summary>
        public virtual void SyncSpawnedObject(GameObject spawned) { }

        /// <summary>Handle joining a room that is already in progress.</summary>
        public virtual void InProgressRoomJoined(string sceneToLoad) { }

        /// <summary>Start a late-join resync.</summary>
        public virtual void CatchUpResync() { }

        /// <summary>Request session data from the instructor.</summary>
        public virtual void RequestData() { }

        /// <summary>Download session data.</summary>
        public virtual void DownloadData() { }

        /// <summary>Apply downloaded step data.</summary>
        public virtual void ApplySteps() { }

        /// <summary>Begin a state sync broadcast.</summary>
        public virtual void Sync() { }

        /// <summary>End a state sync broadcast.</summary>
        public virtual void EndSync() => IsSyncing = false;

        /// <summary>Resets the shared random streams for a fresh session.</summary>
        public virtual void ResetRandoms()
        {
            if (RandomManager.InstanceExists)
                RandomManager.Instance.ResetRandoms();
        }

        /// <summary>Update <see cref="State"/> and fire <see cref="NetworkEvents.OnConnectionStateChanged"/>.</summary>
        protected void SetState(ConnectionState state)
        {
            State = state;
            NetworkEvents.OnConnectionStateChanged?.Invoke(state);
        }
    }
}
