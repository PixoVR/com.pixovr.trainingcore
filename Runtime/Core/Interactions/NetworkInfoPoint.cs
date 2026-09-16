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
    /// <summary>Single info point that participates in networked open/close sync.</summary>
    public class NetworkInfoPoint : MonoBehaviour
    {
        /// <summary>Toggle the point and notify the manager.</summary>
        public virtual bool Toggle()
        {
            if (NetworkInfoPointManager.Instance != null)
            {
                NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
                return true;
            }
            return false;
        }

        /// <summary>Open the point.</summary>
        public virtual void Open()
        {
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
        }

        /// <summary>Close the point.</summary>
        public virtual void Close()
        {
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastClose(GetGuid());
        }

        private string GetGuid() => gameObject.GetGuidString();
    }
}
