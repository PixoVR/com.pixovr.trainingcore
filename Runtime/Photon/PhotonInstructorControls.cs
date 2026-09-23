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
            RaiseInstructorEvent("toggleActive", player?.Id.ToString() ?? "", player == null || !player.IsActive);

        /// <inheritdoc/>
        public override void Remove(MultiuserPlayer player)
        {
            RaiseInstructorEvent("remove", player?.Id.ToString() ?? "", true);
            if (PhotonNetwork.IsMasterClient && player != null)
            {
                var photonPlayer = PhotonNetwork.CurrentRoom?.GetPlayer(player.Id);
                if (photonPlayer != null)
                    PhotonNetwork.CloseConnection(photonPlayer);
            }
        }

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

        /// <summary>Apply an instructor event received over the network.</summary>
        public void ReceiveInstructorEvent(string action, string userId, bool state)
        {
            var room = Multiuser.NetworkManager.Instance?.CurrentRoom;
            var player = int.TryParse(userId, out var id) ? room?.GetPlayer(id) : null;
            switch (action)
            {
                case "setActive":
                case "toggleActive":
                    ApplyActiveState(player, state);
                    break;
                case "remove":
                    if (player != null && player.IsLocal())
                        Multiuser.NetworkManager.Instance?.LeaveRoom();
                    break;
                case "audio":
                    ApplyAudioState(player, state);
                    break;
                case "avatar":
                    ApplyAvatarState(player, state);
                    break;
                case "spotCheck":
                    if (player != null)
                        player.IsSpotChecked = state;
                    break;
                case "valveNames":
                    if (player != null)
                        player.HasValveNamesActive = state;
                    break;
                case "highlight":
                    if (player != null)
                        player.IsHighlighted = state;
                    break;
                case "audioAll":
                    foreach (var p in room?.GetNetworkPlayers() ?? new List<MultiuserPlayer>())
                        if (!p.IsInstructor)
                            p.IsMuted = state;
                    InvokeOnAudioStateAll(state);
                    break;
                case "avatarAll":
                    foreach (var p in room?.GetNetworkPlayers() ?? new List<MultiuserPlayer>())
                        if (!p.IsInstructor)
                            p.IsHidden = !state;
                    InvokeOnAvatarStateAll(state);
                    break;
            }
        }

        private void ApplyActiveState(MultiuserPlayer player, bool state)
        {
            if (player == null)
                return;
            player.IsActive = state;
            if (player.IsLocal())
            {
                var controls = FindObjectsOfType<MonoBehaviour>(true);
                foreach (var c in controls)
                    if (c is XRI.IPlayerInteractionControls ipc)
                        ipc.SetInteractionState(state);
            }
            InvokeOnActiveUserChanged(player, state);
        }

        private void ApplyAudioState(MultiuserPlayer player, bool state)
        {
            if (player == null)
                return;
            player.IsMuted = state;
            InvokeOnPlayerAudioStateChanged(player, state);
        }

        private void ApplyAvatarState(MultiuserPlayer player, bool state)
        {
            if (player == null)
                return;
            player.IsHidden = !state;
            InvokeOnPlayerAvatarStateChanged(player, state);
        }
    }
}