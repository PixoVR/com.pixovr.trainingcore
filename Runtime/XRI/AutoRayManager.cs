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
        public ControllerModeManager ControllerManager;

        /// <summary>Tracked pointer object for the pointer.</summary>
        public Transform RayRoot;

        /// <summary>Layers to react to.</summary>
        public LayerMask UiLayers;

        /// <summary>Auto ray checking distance.</summary>
        public float AutoRayDistance = 10f;

        /// <summary>Logs ray/UI-hit diagnostics.</summary>
        [Tooltip("Logs ray/UI-hit diagnostics.")]
        public bool LogDiagnostics = true;

        private bool hittingUi;

        /// <summary>True while the ray is hitting UI.</summary>
        public bool HittingUi => hittingUi;

        private Ray ray;
        private RaycastHit[] hits = new RaycastHit[10];
        private int hitCounts;
        private bool uiHit;
        private XRUIInputModule xrUiInputModule;
        private float nextHeartbeat;

        private void Start()
        {
            xrUiInputModule = FindObjectOfType<XRUIInputModule>();
            xrUiInputModule?.RegisterInteractor(this);
            if (LogDiagnostics)
                Debug.Log($"[XRI Diag] AutoRayManager '{name}': RayRoot={RayRoot?.name ?? "null"} UiLayers={UiLayers.value} AutoRayDistance={AutoRayDistance} ControllerManager={(ControllerManager != null ? ControllerManager.name : "null")} XRUIInputModule={(xrUiInputModule != null)}");
        }

        private void Update()
        {
            if (RayRoot == null)
                return;
            ray.origin = RayRoot.position;
            ray.direction = RayRoot.forward;
            hitCounts = Physics.RaycastNonAlloc(ray, hits, AutoRayDistance, UiLayers);
            bool hit = hitCounts > 0;
            bool uiModelValid = false;
            if (TryGetUIModel(out var model))
            {
                uiModelValid = model.currentRaycast.isValid;
                hit = hit || uiModelValid;
            }
            if (hit && !uiHit)
            {
                if (LogDiagnostics)
                    Debug.Log($"[XRI Diag] AutoRayManager '{name}': UI hit, requesting Interface (physHit={(hitCounts > 0 ? hits[0].collider.name + " layer=" + LayerMask.LayerToName(hits[0].collider.gameObject.layer) + " dist=" + hits[0].distance : "none")} uiModelValid={uiModelValid})");
                ControllerManager?.ExternalStartRay();
                uiHit = ControllerManager != null &&
                    ControllerManager.Mode == ControllerModeManager.ControllerMode.Interface;
                if (LogDiagnostics)
                    Debug.Log($"[XRI Diag] AutoRayManager '{name}': after ExternalStartRay mode={(ControllerManager != null ? ControllerManager.Mode.ToString() : "null")}");
            }
            else if (!hit && uiHit)
            {
                if (LogDiagnostics)
                    Debug.Log($"[XRI Diag] AutoRayManager '{name}': ray left UI");
                ControllerManager?.ExternalEndRay();
                uiHit = false;
            }
            hittingUi = uiHit;

            if (LogDiagnostics && Time.time >= nextHeartbeat)
            {
                nextHeartbeat = Time.time + 2f;
                bool ok = Physics.Raycast(ray, out var anyHit, AutoRayDistance);
                Debug.Log($"[XRI Diag] AutoRayManager '{name}': origin={ray.origin} dir={ray.direction} maskedHits={hitCounts} anyHit={(ok ? anyHit.collider.name + " layer=" + LayerMask.LayerToName(anyHit.collider.gameObject.layer) + " dist=" + anyHit.distance : "none")} mode={ControllerManager?.Mode}");
            }
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
