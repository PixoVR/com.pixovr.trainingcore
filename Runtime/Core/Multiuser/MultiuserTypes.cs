using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>High-level connection state.</summary>
    public enum ConnectionState
    {
        /// <summary>Not connected.</summary>
        Disconnected,
        /// <summary>Connection attempt in progress (legacy misspelling kept for migration parity).</summary>
        AtteptingConnection,
        /// <summary>Connected to the lobby.</summary>
        Connected
    }

    /// <summary>A connected user in a room.</summary>
    public class MultiuserPlayer
    {
        /// <summary>Transport-level user id.</summary>
        public string Id;
        /// <summary>Display name.</summary>
        public string Name;
        /// <summary>True when the player is the session instructor/lead.</summary>
        public bool IsInstructor;
        /// <summary>Object-visible state (instructor-controlled).</summary>
        public bool IsActive = true;
        /// <summary>Audio-muted state.</summary>
        public bool IsMuted;
        /// <summary>Hidden from the user list.</summary>
        public bool IsHidden;
        /// <summary>Highlighted in the user list.</summary>
        public bool IsHighlighted;
        /// <summary>Being spot-checked by the instructor.</summary>
        public bool IsSpotChecked;
        /// <summary>Valve-name overlay enabled.</summary>
        public bool HasValveNamesActive;
        /// <summary>Platform identifier.</summary>
        public string Platform;
    }

    /// <summary>Instructor-facing per-user controls.</summary>
    public interface InstructorControls
    {
        /// <summary>Set a user's object-visible state.</summary>
        void SetActiveState(string userId, bool state);
        /// <summary>Toggle a user's object-visible state.</summary>
        void ToggleActiveState(string userId);
        /// <summary>Remove a user from the session.</summary>
        void Remove(string userId);
        /// <summary>Set a user's audio-muted state.</summary>
        void SetAudioState(string userId, bool muted);
        /// <summary>Set a user's avatar-visible state.</summary>
        void SetAvatarState(string userId, bool visible);
        /// <summary>Toggle instructor spot-check on a user.</summary>
        void SetPlayerSpotCheckStatus(string userId, bool state);
        /// <summary>Enable/disable valve-name overlays for a user.</summary>
        void SetValveNamesState(string userId, bool state);
        /// <summary>Mute/unmute all users.</summary>
        void SetAudioStateAll(bool muted);
        /// <summary>Show/hide all avatars.</summary>
        void SetAvatarStateAll(bool visible);
        /// <summary>Set highlight state for a user.</summary>
        void SetHighlightState(string userId, bool highlighted);

        /// <summary>Fired when a user's active state changes.</summary>
        event Action<string, bool> OnActiveUserChanged;
        /// <summary>Fired when a user's audio state changes.</summary>
        event Action<string, bool> OnPlayerAudioStateChanged;
        /// <summary>Fired when a user's avatar state changes.</summary>
        event Action<string, bool> OnPlayerAvatarStateChanged;
        /// <summary>Fired when all users' audio state changes.</summary>
        event Action<bool> OnAudioStateAll;
        /// <summary>Fired when all users' avatar state changes.</summary>
        event Action<bool> OnAvatarStateAll;
    }

    /// <summary>Implementation-neutral instructor controls; concrete transports subclass this.</summary>
    public abstract class NetworkInstructorControls : InstructorControls
    {
        public abstract void SetActiveState(string userId, bool state);
        public abstract void ToggleActiveState(string userId);
        public abstract void Remove(string userId);
        public abstract void SetAudioState(string userId, bool muted);
        public abstract void SetAvatarState(string userId, bool visible);
        public abstract void SetPlayerSpotCheckStatus(string userId, bool state);
        public abstract void SetValveNamesState(string userId, bool state);
        public abstract void SetAudioStateAll(bool muted);
        public abstract void SetAvatarStateAll(bool visible);
        public abstract void SetHighlightState(string userId, bool highlighted);

        
        public event Action<string, bool> OnActiveUserChanged;
        
        public event Action<string, bool> OnPlayerAudioStateChanged;
        
        public event Action<string, bool> OnPlayerAvatarStateChanged;
        
        public event Action<bool> OnAudioStateAll;
        
        public event Action<bool> OnAvatarStateAll;

        /// <summary>Raise <see cref="OnActiveUserChanged"/>.</summary>
        protected void InvokeOnActiveUserChanged(string id, bool state) => OnActiveUserChanged?.Invoke(id, state);
        /// <summary>Raise <see cref="OnPlayerAudioStateChanged"/>.</summary>
        protected void InvokeOnPlayerAudioStateChanged(string id, bool state) => OnPlayerAudioStateChanged?.Invoke(id, state);
        /// <summary>Raise <see cref="OnPlayerAvatarStateChanged"/>.</summary>
        protected void InvokeOnPlayerAvatarStateChanged(string id, bool state) => OnPlayerAvatarStateChanged?.Invoke(id, state);
        /// <summary>Raise <see cref="OnAudioStateAll"/>.</summary>
        protected void InvokeOnAudioStateAll(bool state) => OnAudioStateAll?.Invoke(state);
        /// <summary>Raise <see cref="OnAvatarStateAll"/>.</summary>
        protected void InvokeOnAvatarStateAll(bool state) => OnAvatarStateAll?.Invoke(state);
    }

    /// <summary>Freeze/unfreeze behaviour attached to the local player rig.</summary>
    public interface IFreezeBehaviour
    {
        /// <summary>Freeze (true) or release (false) the local player's locomotion.</summary>
        void FreezePlayer(bool freeze);
    }

    /// <summary>Arguments passed to a step's interaction handler (multiuser path).</summary>
    public class StepInteractionArguments
    {
        /// <summary>Step number these arguments apply to.</summary>
        public string StepNumber;
        /// <summary>Originating event args.</summary>
        public InteractionEventArgs EventArgs;
        /// <summary>Serialized payload for custom data.</summary>
        public string Payload;
    }

    /// <summary>Network payload for a single replicated event (legacy wire shape).</summary>
    [Serializable]
    public class EventSyncData
    {
        /// <summary>Subject id the event targets.</summary>
        public string SubjectId;
        /// <summary>Event type discriminator (class name).</summary>
        public string EventType;
        /// <summary>Json-serialized event payload.</summary>
        public string Payload;
        /// <summary>Actor id that produced the event.</summary>
        public string ActorId;
        /// <summary>Unix-ms timestamp of the event.</summary>
        public long Timestamp;
        /// <summary>Serialized bytes for transport.</summary>
        public byte[] Data;
    }

    /// <summary>Observes step lifecycle updates pushed over the network.</summary>
    public interface IFlowEventHandler
    {
        /// <summary>Progress update for the current step.</summary>
        void OnStepUpdate();
        /// <summary>A step wants to execute with interaction arguments.</summary>
        void OnStepExecute(StepInteractionArguments arguments);
        /// <summary>Skip to the next step.</summary>
        void OnSkipToNextStep();
        /// <summary>Skip to the previous step.</summary>
        void OnSkipToPreviousStep();
        /// <summary>Skip to a specific step number.</summary>
        void OnSkipToStep(int stepNumber);
    }

    /// <summary>UnityEvent carrying a string.</summary>
    [Serializable]
    public class UnityStringEvent : UnityEngine.Events.UnityEvent<string> { }

    /// <summary>UnityEvent carrying a <see cref="MultiuserPlayer"/>.</summary>
    [Serializable]
    public class UnityMultiuserEvent : UnityEngine.Events.UnityEvent<MultiuserPlayer> { }

    /// <summary>Static accessors for the current connection/room.</summary>
    public static class NetworkEvents
    {
        /// <summary>Fired when the local connection state changes.</summary>
        public static Action<ConnectionState> OnConnectionStateChanged;
        /// <summary>Fired when the instructor changes.</summary>
        public static Action<MultiuserPlayer> OnInstructorChanged;
    }
}
