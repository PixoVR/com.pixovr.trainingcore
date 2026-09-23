using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Single info point that participates in networked open/close sync.</summary>
    public class NetworkInfoPoint : MonoBehaviour
    {
        private InfoPointBase infoPoint;

        /// <summary>The wrapped info point.</summary>
        public InfoPointBase InfoPoint =>
            infoPoint != null ? infoPoint : infoPoint = GetComponent<InfoPointBase>();

        private void Awake() => infoPoint = GetComponent<InfoPointBase>();

        private void OnEnable()
        {
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.RegisterInfoPoint(GetGuid(), InfoPoint);
        }

        /// <summary>Toggle the point and notify the manager.</summary>
        public virtual bool Toggle()
        {
            bool open = InfoPoint != null && InfoPoint.Toggle();
            if (NetworkInfoPointManager.Instance != null)
            {
                if (open)
                    NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
                else
                    NetworkInfoPointManager.Instance.BroadCastClose(GetGuid());
            }
            return open;
        }

        /// <summary>Open the point.</summary>
        public virtual void Open()
        {
            InfoPoint?.Open();
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
        }

        /// <summary>Close the point.</summary>
        public virtual void Close()
        {
            InfoPoint?.Close();
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastClose(GetGuid());
        }

        private string GetGuid() => gameObject.GetGuidString();
    }
}
