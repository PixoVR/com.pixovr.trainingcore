using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>
    /// Mouse/keyboard stand-in for the XR hand interactors, used by SampleFirstPersonMain-style
    /// desktop scenes. Its transform sits at the mouse-ray hit point (or held-object hold point)
    /// like a virtual hand, so existing XRI behaviours work unchanged.
    /// </summary>
    [AddComponentMenu("PixoVR/TrainingCore/XRI/Desktop Mouse Interactor")]
    public class DesktopMouseInteractor : XRBaseInputInteractor
    {
        /// <summary>Camera the mouse ray is cast from; Camera.main when empty.</summary>
        [Tooltip("Camera the mouse ray is cast from; Camera.main when empty.")]
        public Camera RayCamera;

        /// <summary>Max distance of the hover ray.</summary>
        public float MaxDistance = 4f;

        /// <summary>Layers the hover ray tests.</summary>
        public LayerMask RaycastMask = ~0;

        /// <summary>Closest distance a held object may approach the camera.</summary>
        public float MinHoldDistance = 0.25f;

        /// <summary>Metres per mouse-wheel notch while holding an object.</summary>
        [Tooltip("Metres per mouse-wheel notch while holding an object.")]
        public float ScrollDistanceStep = 0.15f;

        /// <summary>Suppress hover/select while the pointer is over UI.</summary>
        public bool IgnoreWhenOverUI = true;

        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly List<IXRInteractable> unfilteredTargets = new List<IXRInteractable>();
        private IXRInteractable hoverTarget;
        private float holdDistance;

        /// <inheritdoc/>
        protected override void Awake()
        {
            base.Awake();
            selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
            activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
        }

        /// <inheritdoc/>
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            var mouse = Mouse.current;
            var cam = RayCamera != null ? RayCamera : Camera.main;
            if (mouse == null || cam == null)
            {
                hoverTarget = null;
                selectInput.QueueManualState(false, 0f);
                activateInput.QueueManualState(false, 0f);
                return;
            }

            Vector2 pos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(pos);
            bool overUI = IgnoreWhenOverUI && EventSystem.current != null &&
                          EventSystem.current.IsPointerOverGameObject();

            if (hasSelection)
            {
                holdDistance = Mathf.Max(MinHoldDistance,
                    holdDistance + mouse.scroll.ReadValue().y / 120f * ScrollDistanceStep);
                transform.SetPositionAndRotation(ray.GetPoint(holdDistance), cam.transform.rotation);
            }
            else
            {
                hoverTarget = null;
                float d = MaxDistance;
                if (!overUI)
                {
                    int n = Physics.RaycastNonAlloc(ray, hits, MaxDistance, RaycastMask,
                        QueryTriggerInteraction.Collide);
                    System.Array.Sort(hits, 0, n, HitDistanceComparer.Instance);
                    for (int i = 0; i < n; i++)
                    {
                        if (interactionManager != null &&
                            interactionManager.TryGetInteractableForCollider(hits[i].collider, out var interactable))
                        {
                            hoverTarget = interactable;
                            d = hits[i].distance;
                            break;
                        }
                        if (!hits[i].collider.isTrigger)
                        {
                            d = hits[i].distance;
                            break;
                        }
                    }
                }
                transform.SetPositionAndRotation(ray.GetPoint(d), cam.transform.rotation);
                holdDistance = d;
            }

            bool pressed = !overUI && mouse.leftButton.isPressed ||
                           hasSelection && mouse.leftButton.isPressed;
            selectInput.QueueManualState(pressed, pressed ? 1f : 0f);
            bool activate = hasSelection && mouse.middleButton.isPressed;
            activateInput.QueueManualState(activate, activate ? 1f : 0f);
        }

        /// <inheritdoc/>
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();
            unfilteredTargets.Clear();
            if (hoverTarget != null)
                unfilteredTargets.Add(hoverTarget);
            var filter = targetFilter;
            if (filter != null && filter.canProcess)
                filter.Process(this, unfilteredTargets, targets);
            else
                targets.AddRange(unfilteredTargets);
        }

        private sealed class HitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
