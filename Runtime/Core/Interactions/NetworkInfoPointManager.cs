using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Identity;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Tracks open info points and relays open/close state across the session.</summary>
    public class NetworkInfoPointManager : Utility.SingletonBehaviour<NetworkInfoPointManager>
    {
        /// <summary>Only one point may be open at once.</summary>
        public bool OnlyAllowSinglePointOpen;

        /// <summary>Disable only the point's children rather than the whole object.</summary>
        public bool DisableInfoPointChildrenOnly;

        private readonly HashSet<string> registered = new HashSet<string>();
        private readonly Dictionary<string, bool> openStates = new Dictionary<string, bool>();

        /// <summary>Raised when a point's enable state is applied; guid, enabled.</summary>
        public event Action<string, bool> OnInfoPointStateChanged;

        /// <summary>Register a point by guid.</summary>
        public void RegisterInfoPoint(string guid) => registered.Add(guid);

        /// <summary>Unregister a point by guid.</summary>
        public void UnregisterInfoPoint(string guid)
        {
            registered.Remove(guid);
            openStates.Remove(guid);
        }

        /// <summary>Open a point and broadcast it.</summary>
        public void BroadCastOpen(string guid) => SetState(guid, true);

        /// <summary>Close a point and broadcast it.</summary>
        public void BroadCastClose(string guid) => SetState(guid, false);

        /// <summary>Open a video content point and broadcast it.</summary>
        public void BroadCastVideoOpen(string guid) => SetState(guid, true);

        /// <summary>Close a video content point and broadcast it.</summary>
        public void BroadCastVideoClose(string guid) => SetState(guid, false);

        /// <summary>Open a picture content point and broadcast it.</summary>
        public void BroadCastPictureOpen(string guid) => SetState(guid, true);

        /// <summary>Close a picture content point and broadcast it.</summary>
        public void BroadCastPictureClose(string guid) => SetState(guid, false);

        /// <summary>Disable a point and broadcast it.</summary>
        public void BroadCastDisableInfoPoint(string guid) => SetState(guid, false);

        /// <summary>Apply an enable state received from the network.</summary>
        public void ReceiveSetInfoPointEnableState(string guid, bool state) => SetState(guid, state, broadcast: false);

        /// <summary>Push all known point states to a newly joined player.</summary>
        public void UpdateNewPlayerOfStatus()
        {
            foreach (var pair in openStates)
                OnInfoPointStateChanged?.Invoke(pair.Key, pair.Value);
        }

        private void SetState(string guid, bool state, bool broadcast = true)
        {
            if (OnlyAllowSinglePointOpen && state)
            {
                foreach (var key in openStates.Keys.ToList())
                    openStates[key] = false;
            }
            openStates[guid] = state;
            OnInfoPointStateChanged?.Invoke(guid, state);
        }
    }
}
