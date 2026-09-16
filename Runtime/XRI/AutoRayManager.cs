using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Interactions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>
    /// Enables the pointer ray when the user points at UI or a ray interactable (replaces AutoRayManager).
    /// </summary>
    public class AutoRayManager : MonoBehaviour, IUIInteractor
    {
        /// <summary>The target controller manager.</summary>
        public MonoBehaviour ControllerManager;

        /// <summary>Tracked pointer object for the pointer.</summary>
        public Transform RayRoot;

        /// <summary>Layers to react to.</summary>
        public LayerMask UiLayers;

        /// <summary>Auto ray checking distance.</summary>
        public float AutoRayDistance = 10f;

        private bool hittingUi;

        /// <summary>True while the ray is hitting UI.</summary>
        public bool HittingUi => hittingUi;

        private Ray ray;
        private RaycastHit[] hits = new RaycastHit[10];
        private int hitCounts;
        private bool uiHit;
        private XRUIInputModule xrUiInputModule;

        private void Start()
        {
            xrUiInputModule = FindObjectOfType<XRUIInputModule>();
            xrUiInputModule?.RegisterInteractor(this);
        }

        private void Update()
        {
            if (RayRoot == null)
                return;
            ray.origin = RayRoot.position;
            ray.direction = RayRoot.forward;
            hitCounts = Physics.RaycastNonAlloc(ray, hits, AutoRayDistance, UiLayers);
            uiHit = hitCounts > 0;
            if (TryGetUIModel(out var model))
                uiHit = uiHit || model.currentRaycast.isValid;
            hittingUi = uiHit;
        }

        /// <summary>Manually updates the XR UI input module with this pointer's state.</summary>
        public void UpdateUIModel(ref TrackedDeviceModel model)
        {
            if (RayRoot == null)
                return;
            var position = RayRoot.position;
            model.position = position;
            model.orientation = RayRoot.rotation;
            model.select = true;
            model.raycastLayerMask = UiLayers;
            var raycastPoints = model.raycastPoints;
            raycastPoints.Clear();
            raycastPoints.Add(position);
            raycastPoints.Add(position + RayRoot.forward * AutoRayDistance);
        }

        /// <summary>Reads this pointer's UI model from the input module.</summary>
        public bool TryGetUIModel(out TrackedDeviceModel model)
        {
            if (xrUiInputModule != null)
                return xrUiInputModule.GetTrackedDeviceModel(this, out model);
            model = new TrackedDeviceModel(-1);
            return false;
        }
    }
}
