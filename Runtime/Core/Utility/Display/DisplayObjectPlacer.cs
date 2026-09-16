using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Places a display object relative to the camera using <see cref="PlacerSettings"/>.</summary>
    public class DisplayObjectPlacer : MonoBehaviour
    {
        /// <summary>Replace the placement settings.</summary>
        public void ApplySettings(Settings.PlacerSettings settings) => Settings = settings;

        /// <summary>Placement settings.</summary>
        public PlacerSettings Settings;

        /// <summary>Placement origin override.</summary>
        public Transform OriginTransform;

        /// <summary>Off-screen arrow (left).</summary>
        public ArrowPlacer OffScreenLeft;

        /// <summary>Off-screen arrow (right).</summary>
        public ArrowPlacer OffScreenRight;

        /// <summary>Show debug gizmos.</summary>
        public bool ShowGizmos;

        /// <summary>The object being placed.</summary>
        public GameObject PlacementObject;

        /// <summary>Live placement point.</summary>
        public Vector3 PlacementLocation { get; private set; }

        /// <summary>Live rotation.</summary>
        public Quaternion PlacementRotation { get; private set; }

        /// <summary>Seconds spent on placement.</summary>
        public float TotalPlacementTime => _totalPlacementTime;

        /// <summary>Whether the object is currently visible on screen.</summary>
        public bool ObjectOnScreen => _objectOnScreen;

        private float _totalPlacementTime;
        private bool _objectOnScreen = true;
        private bool _placementInitiated;
        private bool _placementFinished;

        /// <summary>Show and place the object in front of the camera.</summary>
        public virtual void ShowObject()
        {
            if (PlacementObject == null)
                return;
            var origin = OriginTransform != null
                ? OriginTransform
                : (Camera.main != null ? Camera.main.transform : transform);
            float dist = Settings != null ? Mathf.Max(Settings.TargetOriginDistance, Settings.MinimumDistanceFromCamera) : 2f;
            float height = Settings != null ? Settings.DisplayHeight : 0f;
            PlacementObject.SetActive(true);
            PlacementObject.transform.position = origin.position + origin.forward * dist + Vector3.up * height;
            PlacementObject.transform.rotation = Quaternion.LookRotation(origin.forward);
            PlacementLocation = PlacementObject.transform.position;
            PlacementRotation = PlacementObject.transform.rotation;
            _placementFinished = true;
        }

        /// <summary>Hide the object.</summary>
        public virtual void HideObject()
        {
            if (PlacementObject != null)
                PlacementObject.SetActive(false);
            _placementFinished = false;
            _placementInitiated = false;
        }

        /// <summary>Start interactive placement.</summary>
        public void StartPlacementMode()
        {
            _placementInitiated = true;
            _placementFinished = false;
        }

        /// <summary>Stop placement without finishing.</summary>
        public void StopPlacementMode() => _placementInitiated = false;

        private void Update()
        {
            if (!_placementInitiated)
                return;
            _totalPlacementTime += Time.deltaTime;
        }
    }
}
