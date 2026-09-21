using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace PixoVR.TrainingCore.XRI
{
#pragma warning disable CS0618 // XRBaseController/XRRayInteractor deprecated in XRI 3.x; kept for upgrade parity.
    /// <summary>
    /// Switches a hand between three controller GameObjects: Base (direct interaction),
    /// Teleport (ray teleport) and Interface (UI ray). Teleport and Interface start disabled.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class ControllerModeManager : MonoBehaviour
    {
        public enum ControllerMode { None, Base, Teleport, Interface }

        /// <summary>Direct-interaction controller GameObject.</summary>
        public GameObject BaseController;

        /// <summary>Teleport ray controller GameObject.</summary>
        public GameObject TeleportController;

        /// <summary>UI/interface ray controller GameObject.</summary>
        public GameObject InterfaceController;

        /// <summary>Action that enters teleport mode.</summary>
        public InputActionReference TeleportModeActivate;

        /// <summary>Action that cancels teleport mode.</summary>
        public InputActionReference TeleportModeCancel;

        /// <summary>Action that enters interface mode.</summary>
        public InputActionReference InterfaceModeActivate;

        /// <summary>Anchor translate action, enabled only in Base mode.</summary>
        public InputActionReference TranslateAnchor;

        /// <summary>Anchor rotate action, enabled only in Base mode.</summary>
        public InputActionReference RotateAnchor;

        /// <summary>The currently active mode.</summary>
        public ControllerMode Mode { get; private set; } = ControllerMode.None;

        private XRBaseController baseController;
        private XRBaseInteractor baseInteractor;
        private XRInteractorLineVisual baseLineVisual;
        private XRBaseController teleportController;
        private XRBaseInteractor teleportInteractor;
        private XRInteractorLineVisual teleportLineVisual;
        private XRBaseController interfaceController;
        private XRBaseInteractor interfaceInteractor;
        private XRInteractorLineVisual interfaceLineVisual;

        protected void OnEnable()
        {
            FindComponents(TeleportController, ref teleportController, ref teleportInteractor, ref teleportLineVisual);
            FindComponents(InterfaceController, ref interfaceController, ref interfaceInteractor, ref interfaceLineVisual);
        }

        protected void Start()
        {
            TransitionTo(ControllerMode.Base);
        }

        protected void Update()
        {
            switch (Mode)
            {
                case ControllerMode.Base:
                    UpdateBase();
                    break;
                case ControllerMode.Teleport:
                    UpdateTeleport();
                    break;
            }
        }

        private void UpdateBase()
        {
            bool triggerTeleport = GetAction(TeleportModeActivate) is { triggered: true };
            bool cancelTeleport = GetAction(TeleportModeCancel) is { triggered: true };
            bool triggerInterface = GetAction(InterfaceModeActivate) is { triggered: true };

            if (triggerTeleport && !cancelTeleport)
            {
                TransitionTo(ControllerMode.Teleport);
                return;
            }

            if (triggerInterface)
                TransitionTo(ControllerMode.Interface);
        }

        private void UpdateTeleport()
        {
            InputAction teleportModeAction = GetAction(TeleportModeActivate);
            bool cancelTeleport = GetAction(TeleportModeCancel) is { triggered: true };
            bool releasedTeleport = teleportModeAction != null && teleportModeAction.phase == InputActionPhase.Waiting;

            if (cancelTeleport || releasedTeleport)
                TransitionTo(ControllerMode.Base);
        }

        /// <summary>
        /// Switches from Base to Interface mode, unless teleport mode is currently held.
        /// </summary>
        public void ExternalStartRay()
        {
            if (GetAction(TeleportModeActivate) is { phase: InputActionPhase.Performed })
                return;
            if (Mode == ControllerMode.Base)
                TransitionTo(ControllerMode.Interface);
        }

        /// <summary>
        /// Leaves Interface mode: to Teleport if the teleport action is held, otherwise to Base.
        /// </summary>
        public void ExternalEndRay()
        {
            if (Mode != ControllerMode.Interface)
                return;
            TransitionTo(GetAction(TeleportModeActivate) is { phase: InputActionPhase.Performed }
                ? ControllerMode.Teleport
                : ControllerMode.Base);
        }

        private void TransitionTo(ControllerMode next)
        {
            OnExitMode(Mode, next);
            ControllerMode previous = Mode;
            Mode = next;
            OnEnterMode(previous, next);
        }

        private void OnEnterMode(ControllerMode previous, ControllerMode mode)
        {
            if (mode != ControllerMode.Base)
                SetController(BaseController, BaseControllerComponent, baseInteractor, baseLineVisual, false);
            if (mode != ControllerMode.Teleport)
                SetController(TeleportController, teleportController, teleportInteractor, teleportLineVisual, false);
            if (mode != ControllerMode.Interface)
                SetController(InterfaceController, interfaceController, interfaceInteractor, interfaceLineVisual, false);

            switch (mode)
            {
                case ControllerMode.Base:
                    SetController(BaseController, baseController, baseInteractor, baseLineVisual, true);
                    EnableAction(TranslateAnchor);
                    EnableAction(RotateAnchor);
                    break;
                case ControllerMode.Teleport:
                    SetController(TeleportController, teleportController, teleportInteractor, teleportLineVisual, true);
                    break;
                case ControllerMode.Interface:
                    if (previous == ControllerMode.Base)
                    {
                        EnableAction(TeleportModeActivate);
                        EnableAction(TeleportModeCancel);
                    }
                    SetController(InterfaceController, interfaceController, interfaceInteractor, interfaceLineVisual, true);
                    break;
            }
        }

        private void OnExitMode(ControllerMode mode, ControllerMode next)
        {
            if (mode == ControllerMode.Base)
            {
                DisableAction(TranslateAnchor);
                DisableAction(RotateAnchor);
            }
        }

        private XRBaseController BaseControllerComponent
        {
            get
            {
                if (baseController == null && BaseController != null)
                    FindComponents(BaseController, ref baseController, ref baseInteractor, ref baseLineVisual);
                return baseController;
            }
        }

        private static void FindComponents(GameObject go, ref XRBaseController controller,
            ref XRBaseInteractor interactor, ref XRInteractorLineVisual lineVisual)
        {
            if (go == null)
                return;
            if (controller == null)
                controller = go.GetComponent<XRBaseController>();
            if (interactor == null)
                interactor = go.GetComponent<XRBaseInteractor>();
            if (interactor is XRRayInteractor && lineVisual == null)
                lineVisual = go.GetComponent<XRInteractorLineVisual>();
        }

        private static void SetController(GameObject go, XRBaseController controller,
            XRBaseInteractor interactor, XRInteractorLineVisual lineVisual, bool enable)
        {
            if (go == null)
                return;
            if (controller == null || interactor == null)
            {
                controller = go.GetComponent<XRBaseController>();
                interactor = go.GetComponent<XRBaseInteractor>();
                if (interactor is XRRayInteractor && lineVisual == null)
                    lineVisual = go.GetComponent<XRInteractorLineVisual>();
            }

            if (controller != null)
                controller.enableInputActions = enable;
            if (interactor != null)
                interactor.enabled = enable;
            if (interactor is XRRayInteractor && lineVisual != null)
                lineVisual.enabled = enable;
        }

        private static InputAction GetAction(InputActionReference reference) => reference != null ? reference.action : null;

        private static void EnableAction(InputActionReference reference)
        {
            InputAction action = GetAction(reference);
            if (action != null && !action.enabled)
                action.Enable();
        }

        private static void DisableAction(InputActionReference reference)
        {
            InputAction action = GetAction(reference);
            if (action != null && action.enabled)
                action.Disable();
        }
    }
#pragma warning restore CS0618
}
