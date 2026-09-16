using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;

namespace PixoVR.TrainingCore.Multiuser
{
    /// <summary>Implementation-neutral instructor controls; concrete transports subclass this.</summary>
    public abstract class NetworkInstructorControls : MonoBehaviour, InstructorControls
    {
        public abstract void SetActiveState(MultiuserPlayer player, bool targetState);
        public abstract void ToggleActiveState(MultiuserPlayer player);
        public abstract void Remove(MultiuserPlayer player);
        public abstract void SetAudioState(MultiuserPlayer player, bool state);
        public abstract void SetAvatarState(MultiuserPlayer player, bool state);
        public abstract void SetPlayerSpotCheckStatus(MultiuserPlayer player, bool state);
        public abstract void SetValveNamesState(MultiuserPlayer player, bool state);
        public abstract void SetAudioStateAll(bool muted);
        public abstract void SetAvatarStateAll(bool visible);
        public abstract void SetHighlightState(MultiuserPlayer player, bool state);

        
        public event Action<MultiuserPlayer, bool> OnActiveUserChanged;
        
        public event Action<MultiuserPlayer, bool> OnPlayerAudioStateChanged;
        
        public event Action<MultiuserPlayer, bool> OnPlayerAvatarStateChanged;
        
        public event Action<bool> OnAudioStateAll;
        
        public event Action<bool> OnAvatarStateAll;

        /// <summary>Raise <see cref="OnActiveUserChanged"/>.</summary>
        protected void InvokeOnActiveUserChanged(MultiuserPlayer player, bool state) => OnActiveUserChanged?.Invoke(player, state);
        /// <summary>Raise <see cref="OnPlayerAudioStateChanged"/>.</summary>
        protected void InvokeOnPlayerAudioStateChanged(MultiuserPlayer player, bool state) => OnPlayerAudioStateChanged?.Invoke(player, state);
        /// <summary>Raise <see cref="OnPlayerAvatarStateChanged"/>.</summary>
        protected void InvokeOnPlayerAvatarStateChanged(MultiuserPlayer player, bool state) => OnPlayerAvatarStateChanged?.Invoke(player, state);
        /// <summary>Raise <see cref="OnAudioStateAll"/>.</summary>
        protected void InvokeOnAudioStateAll(bool state) => OnAudioStateAll?.Invoke(state);
        /// <summary>Raise <see cref="OnAvatarStateAll"/>.</summary>
        protected void InvokeOnAvatarStateAll(bool state) => OnAvatarStateAll?.Invoke(state);
    }
}