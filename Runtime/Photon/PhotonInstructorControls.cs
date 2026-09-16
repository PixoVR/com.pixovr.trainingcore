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
    /// <summary>Photon-backed instructor controls.</summary>
    public class PhotonInstructorControls : NetworkInstructorControls
    {
        /// <inheritdoc/>
        public override void SetActiveState(MultiuserPlayer player, bool state) =>
            RaiseInstructorEvent("setActive", player?.Id.ToString() ?? "", state);

        /// <inheritdoc/>
        public override void ToggleActiveState(MultiuserPlayer player) =>
            RaiseInstructorEvent("toggleActive", player?.Id.ToString() ?? "", true);

        /// <inheritdoc/>
        public override void Remove(MultiuserPlayer player) =>
            RaiseInstructorEvent("remove", player?.Id.ToString() ?? "", true);

        /// <inheritdoc/>
        public override void SetAudioState(MultiuserPlayer player, bool muted) =>
            RaiseInstructorEvent("audio", player?.Id.ToString() ?? "", muted);

        /// <inheritdoc/>
        public override void SetAvatarState(MultiuserPlayer player, bool visible) =>
            RaiseInstructorEvent("avatar", player?.Id.ToString() ?? "", visible);

        /// <inheritdoc/>
        public override void SetPlayerSpotCheckStatus(MultiuserPlayer player, bool state) =>
            RaiseInstructorEvent("spotCheck", player?.Id.ToString() ?? "", state);

        /// <inheritdoc/>
        public override void SetValveNamesState(MultiuserPlayer player, bool state) =>
            RaiseInstructorEvent("valveNames", player?.Id.ToString() ?? "", state);

        /// <inheritdoc/>
        public override void SetAudioStateAll(bool muted) =>
            RaiseInstructorEvent("audioAll", "", muted);

        /// <inheritdoc/>
        public override void SetAvatarStateAll(bool visible) =>
            RaiseInstructorEvent("avatarAll", "", visible);

        /// <inheritdoc/>
        public override void SetHighlightState(MultiuserPlayer player, bool highlighted) =>
            RaiseInstructorEvent("highlight", player?.Id.ToString() ?? "", highlighted);

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
}