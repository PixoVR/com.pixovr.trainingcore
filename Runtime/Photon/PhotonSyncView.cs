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
    /// <summary>PUN component that republishes received RaiseEvent interaction payloads to the <see cref="EventBus"/>.</summary>
    public class PhotonSyncView : MonoBehaviourPun, IOnEventCallback, IPunObservable
    {
        private static PhotonSyncView active;

        private XRI.XRIValveBehaviour valve;

        private void Awake() => valve = GetComponent<XRI.XRIValveBehaviour>();

        /// <summary>Serialize a sibling valve's total rotation (owner writes, others apply).</summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (valve == null)
                return;
            if (stream.IsWriting)
                stream.SendNext(valve.TotalRotation);
            else
                valve.SetRotationFromNetwork((float)stream.ReceiveNext());
        }

        private void OnEnable()
        {
            if (active != null && active != this)
                return;
            active = this;
            PhotonNetwork.AddCallbackTarget(this);
            EventBus.Instance.OnPublished += SendInteraction;
        }

        private void OnDisable()
        {
            if (active != this)
                return;
            active = null;
            PhotonNetwork.RemoveCallbackTarget(this);
            EventBus.Instance.OnPublished -= SendInteraction;
        }

        /// <summary>Republish Photon events into the local event bus.</summary>
        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent.Code != PhotonEventSerializer.InteractionEventCode)
                return;
            var data = PhotonEventSerializer.DeserializeEventSyncData(photonEvent.CustomData as object[]);
            var args = PhotonEventSerializer.FromSyncData(data);
            if (args != null && !string.IsNullOrEmpty(args.SubjectId))
            {
                args.IsRemote = true;
                EventBus.Instance.Publish(args.SubjectId, args);
            }
        }

        /// <summary>Broadcast a local interaction event to the room.</summary>
        public void SendInteraction(InteractionEventArgs args)
        {
            if (!PhotonNetwork.InRoom)
                return;
            if (args is ValveTurnEventArgs)
                return;
            PhotonNetwork.RaiseEvent(PhotonEventSerializer.InteractionEventCode,
                PhotonEventSerializer.Serialize(args),
                new RaiseEventOptions { Receivers = ReceiverGroup.Others },
                SendOptions.SendReliable);
        }
    }
}