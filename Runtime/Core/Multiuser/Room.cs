using System;
using System.Collections.Generic;

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>Abstract room: wraps the transport room and its player list + properties.</summary>
    public abstract class Room
    {
        /// <summary>Room property storing the scene to load.</summary>
        public const string SceneNameProperty = "CurrentScene";

        /// <summary>Display/joinable name of the room.</summary>
        public abstract string RoomName { get; }

        /// <summary>Room-configured game mode.</summary>
        public abstract GameModes.GameMode CurrentMode { get; }

        /// <summary>All connected players.</summary>
        public abstract List<MultiuserPlayer> GetNetworkPlayers();

        /// <summary>The local player.</summary>
        public abstract MultiuserPlayer GetLocalPlayer();

        /// <summary>Find a player by id.</summary>
        public abstract MultiuserPlayer GetPlayer(string id);

        /// <summary>Add a room property.</summary>
        public abstract void AddProperty(string key, object value);

        /// <summary>Remove a room property.</summary>
        public abstract void RemoveProperty(string key);

        /// <summary>Set a room property (adds when missing).</summary>
        public abstract void SetProperty(string key, object value);

        /// <summary>Get a room property.</summary>
        public abstract object GetProperty(string key);

        /// <summary>Get a typed room property.</summary>
        public T GetProperty<T>(string key)
        {
            var value = GetProperty(key);
            return value is T typed ? typed : default;
        }

        /// <summary>Whether a room property exists.</summary>
        public abstract bool HasProperty(string key);

        /// <summary>Fired when a player joins.</summary>
        public abstract event Action<MultiuserPlayer> OnPlayerJoin;
        /// <summary>Fired when a player leaves.</summary>
        public abstract event Action<MultiuserPlayer> OnPlayerExit;
        /// <summary>Fired when the instructor/master switches.</summary>
        public abstract event Action<MultiuserPlayer> OnMasterUserSwitched;
    }
}
