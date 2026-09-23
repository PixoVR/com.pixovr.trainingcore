using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PixoVR.TrainingCore.Flow;
using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>Lifecycle status of a training session on the platform.</summary>
    public enum SessionStatus
    {
        /// <summary>Session created but not yet scheduled.</summary>
        Created,
        /// <summary>Session scheduled, waiting to start.</summary>
        Waiting,
        /// <summary>Session currently running.</summary>
        Active,
        /// <summary>Session finished.</summary>
        Complete
    }

    /// <summary>Contract for a platform backend session (authentication + progress reporting).</summary>
    public interface IPlatformSession
    {
        /// <summary>True while connected to the backend.</summary>
        bool IsConnected { get; }

        /// <summary>Logged-in user id.</summary>
        string UserId { get; }

        /// <summary>Active platform session id.</summary>
        string SessionId { get; }

        /// <summary>Fired on successful connect.</summary>
        event Action Connected;
        /// <summary>Fired on disconnect.</summary>
        event Action Disconnected;
        /// <summary>Fired when a connection attempt fails.</summary>
        event Action ConnectionFailed;
        /// <summary>Fired when the backend reports no active session.</summary>
        event Action NoActiveSession;
        /// <summary>Session status update: session name, status, module scene.</summary>
        event Action<string, SessionStatus, string> StatusUpdated;
        /// <summary>Fired when the user cancels a login/connect flow.</summary>
        event Action Cancelled;
        /// <summary>Fired when session details have been refreshed from the backend.</summary>
        event Action SessionRefreshed;

        /// <summary>Badge id of the connected user (empty when unknown).</summary>
        string ConnectedBadgeId { get; set; }
        /// <summary>Backend id of the connected user.</summary>
        string ConnectedUserId { get; set; }
        /// <summary>Module currently in progress (mirrors <see cref="SessionContext.Module"/>).</summary>
        string CurrentModuleName { get; set; }
        /// <summary>Module selected to start (mirrors <see cref="SessionContext.Module"/>).</summary>
        string SelectedModuleName { get; set; }
        /// <summary>Display nickname of the connected student.</summary>
        string StudentNickname { get; set; }
        /// <summary>Backend base address (informational, e.g. for linking hosted images).</summary>
        string ServerBaseAddress { get; }
        /// <summary>Token handed to the app by the hub/launcher; null when launched standalone.</summary>
        string LaunchToken { get; }
        /// <summary>Current connection state.</summary>
        Multiuser.ConnectionState State { get; }

        /// <summary>Record the platform session id to join/track.</summary>
        void SetSessionId(string sessionId);

        /// <summary>Send a named session event to the backend (best-effort; no-op when unsupported).</summary>
        Task SendEventAsync(string action, string target);

        /// <summary>Legacy alias for <see cref="RefreshSessionAsync"/>.</summary>
        Task RefreshSessionDetails();

        /// <summary>Username+password login.</summary>
        Task<bool> LoginAsync(string username, string password);

        /// <summary>PIN login.</summary>
        Task<bool> LoginWithPinAsync(int pin);

        /// <summary>Resume from a stored token.</summary>
        Task<bool> LoginWithTokenAsync(string token);

        /// <summary>Revalidate an expiring session.</summary>
        Task RefreshSessionAsync();

        /// <summary>Report the session status.</summary>
        Task SetStatusAsync(string sessionId, SessionStatus status, string moduleScene);

        /// <summary>Report that a module run started.</summary>
        Task ModuleStartedAsync(string mode, string scenario, string module);

        /// <summary>Report that a module run ended.</summary>
        Task ModuleEndedAsync(string mode, string scenario, string module, bool passed);

        /// <summary>Refresh the status of a session by id.</summary>
        Task GetStatusAsync(string sessionId);

        /// <summary>Report that a module's info/details display started.</summary>
        Task ModuleInfoStartedAsync(string sessionId, string module);

        /// <summary>Report that a module's info/details display ended.</summary>
        Task ModuleInfoEndedAsync(string sessionId, string module);

        /// <summary>The portal catalog (scenarios + scheduled sessions) for the connected user.</summary>
        UserScenarios GetUserScenarios();

        /// <summary>Report that a step started.</summary>
        Task StepStartedAsync(StepBase step);

        /// <summary>Report that a step completed.</summary>
        Task StepCompletedAsync(StepBase step);

        /// <summary>Report that a step failed.</summary>
        Task StepFailedAsync(StepBase step, string reason);

        /// <summary>Terminate the session.</summary>
        Task DisconnectAsync();
    }

    /// <summary>Base class for platform session providers. Awake registers <see cref="Instance"/>.</summary>
    public abstract class PlatformSessionBase : MonoBehaviour, IPlatformSession
    {
        /// <summary>Active session (the most recently installed provider).</summary>
        public static IPlatformSession Instance { get; protected set; }

        /// <summary>
        /// Returns the active session. When no provider has been installed (no
        /// ApexPlatformBootstrap in scene), falls back to a <see cref="NullPlatformSession"/>
        /// and logs a warning — the null provider exists for tests only.
        /// </summary>
        public static IPlatformSession EnsureActive()
        {
            if (Instance == null)
            {
                Utility.Log.Warning("No IPlatformSession installed — falling back to NullPlatformSession (tests only)");
                Instance = new NullPlatformSession();
            }
            return Instance;
        }

        /// <summary>Registers this provider as <see cref="Instance"/>.</summary>
        protected virtual void Awake() => Instance = this;

        /// <summary>Provider startup hook (catalog seeding etc.).</summary>
        protected virtual void Start()
        {
            GameModes.GameModeManager.OnModuleStart += ReportModuleStart;
            GameModes.GameModeManager.OnStepsStarted += ReportStepsStarted;
            GameModes.GameModeManager.OnStepComplete += ReportStepCompleted;
            GameModes.GameModeManager.OnFail += ReportStepFailed;
            GameModes.GameModeManager.OnModulePassed += ReportModulePassed;
            GameModes.GameModeManager.OnModuleEnd += ReportModuleEnd;
        }

        private bool lastModulePassed;
        private bool moduleReportActive;

        private bool CanReport => IsConnected && !string.IsNullOrEmpty(SessionId);

        private void ReportModuleStart(string moduleName, Graph.TrainingGraph graph)
        {
            if (CanReport)
                _ = ModuleStartedAsync(GameModes.GameModeManager.CurrentMode.ToString(), null, moduleName);
        }

        private void ReportStepsStarted(string flowName, List<Flow.StepBase> steps)
        {
            if (!CanReport || steps == null)
                return;
            foreach (var step in steps)
                _ = StepStartedAsync(step);
        }

        private void ReportStepCompleted(string flowName, Flow.StepBase step)
        {
            if (CanReport)
                _ = StepCompletedAsync(step);
        }

        private void ReportStepFailed(List<Flow.StepBase> steps, string reason, int handlerIndex)
        {
            if (!CanReport || steps == null)
                return;
            foreach (var step in steps)
                _ = StepFailedAsync(step, reason);
        }

        private void ReportModulePassed(string moduleName, Graph.TrainingGraph graph)
        {
            lastModulePassed = true;
            if (CanReport)
                _ = ModuleEndedAsync(GameModes.GameModeManager.CurrentMode.ToString(), null, moduleName, true);
        }

        private void ReportModuleEnd(string moduleName, Graph.TrainingGraph graph)
        {
            var passed = lastModulePassed;
            lastModulePassed = false;
            if (!passed && CanReport)
                _ = ModuleEndedAsync(GameModes.GameModeManager.CurrentMode.ToString(), null, moduleName, false);
        }

        /// <summary>Clears <see cref="Instance"/> when destroyed.</summary>
        protected virtual void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        /// <summary>See the interface/base contract.</summary>
        public abstract bool IsConnected { get; }
        /// <summary>See the interface/base contract.</summary>
        public abstract string UserId { get; }
        /// <summary>See the interface/base contract.</summary>
        public abstract string SessionId { get; }

        /// <summary>See the interface/base contract.</summary>
        public event Action Connected;
        /// <summary>See the interface/base contract.</summary>
        public event Action Disconnected;
        /// <summary>See the interface/base contract.</summary>
        public event Action ConnectionFailed;
        /// <summary>See the interface/base contract.</summary>
        public event Action NoActiveSession;
        /// <summary>See the interface/base contract.</summary>
        public event Action<string, SessionStatus, string> StatusUpdated;
        /// <summary>See the interface/base contract.</summary>
        public event Action Cancelled;
        /// <summary>See the interface/base contract.</summary>
        public event Action SessionRefreshed;

        /// <summary>Raise <see cref="Connected"/>.</summary>
        protected void InvokeConnected() => Connected?.Invoke();
        /// <summary>Raise <see cref="Disconnected"/>.</summary>
        protected void InvokeDisconnected() => Disconnected?.Invoke();
        /// <summary>Raise <see cref="ConnectionFailed"/>.</summary>
        protected void InvokeConnectionFailed() => ConnectionFailed?.Invoke();
        /// <summary>Raise <see cref="NoActiveSession"/>.</summary>
        protected void InvokeNoActiveSession() => NoActiveSession?.Invoke();
        /// <summary>Raise <see cref="StatusUpdated"/>.</summary>
        protected void InvokeStatusUpdated(string name, SessionStatus status, string moduleScene) =>
            StatusUpdated?.Invoke(name, status, moduleScene);
        /// <summary>Raise <see cref="Cancelled"/>.</summary>
        protected void InvokeCancelled() => Cancelled?.Invoke();
        /// <summary>Raise <see cref="SessionRefreshed"/>.</summary>
        protected void InvokeSessionRefreshed() => SessionRefreshed?.Invoke();

        /// <summary>See the interface/base contract.</summary>
        public virtual string ConnectedBadgeId { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public virtual string ConnectedUserId { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public virtual string CurrentModuleName
        {
            get => SessionContext.Instance != null ? SessionContext.Instance.Module : null;
            set { if (SessionContext.Instance != null) SessionContext.Instance.Module = value; }
        }
        /// <summary>See the interface/base contract.</summary>
        public virtual string SelectedModuleName
        {
            get => SessionContext.Instance != null ? SessionContext.Instance.Module : null;
            set { if (SessionContext.Instance != null) SessionContext.Instance.Module = value; }
        }
        /// <summary>See the interface/base contract.</summary>
        public virtual string ServerBaseAddress => string.Empty;
        /// <summary>See the interface/base contract.</summary>
        public virtual string StudentNickname { get; set; }

        /// <summary>Catalog returned by <see cref="GetUserScenarios"/> (never null).</summary>
        protected UserScenarios Catalog = new UserScenarios();

        /// <summary>See the interface/base contract.</summary>
        public virtual Multiuser.ConnectionState State =>
            IsConnected ? Multiuser.ConnectionState.Connected : Multiuser.ConnectionState.Disconnected;
        /// <summary>See the interface/base contract.</summary>
        public virtual void SetSessionId(string sessionId)
        {
            if (SessionContext.Instance != null)
                SessionContext.Instance.SessionId = sessionId;
        }
        /// <summary>See the interface/base contract.</summary>
        public virtual Task SendEventAsync(string action, string target)
        {
            Utility.Log.Warning($"SendEventAsync('{action}') not supported by this session provider");
            return Task.CompletedTask;
        }
        /// <summary>See the interface/base contract.</summary>
        public Task RefreshSessionDetails() => RefreshSessionAsync();

        /// <summary>See the interface/base contract.</summary>
        public abstract Task<bool> LoginAsync(string username, string password);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task<bool> LoginWithPinAsync(int pin);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task<bool> LoginWithTokenAsync(string token);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task RefreshSessionAsync();
        /// <summary>See the interface/base contract.</summary>
        public abstract Task SetStatusAsync(string sessionId, SessionStatus status, string moduleScene);
        /// <summary>See the interface/base contract. Idempotent: repeated starts within one run return early.</summary>
        public Task ModuleStartedAsync(string mode, string scenario, string module)
        {
            if (moduleReportActive)
                return Task.CompletedTask;
            moduleReportActive = true;
            return OnModuleStartedAsync(mode, scenario, module);
        }
        /// <summary>See the interface/base contract. Idempotent: ignored when no module report is active.</summary>
        public Task ModuleEndedAsync(string mode, string scenario, string module, bool passed)
        {
            if (!moduleReportActive)
                return Task.CompletedTask;
            moduleReportActive = false;
            return OnModuleEndedAsync(mode, scenario, module, passed);
        }
        /// <summary>Provider implementation of the module-start report.</summary>
        protected virtual Task OnModuleStartedAsync(string mode, string scenario, string module) => Task.CompletedTask;
        /// <summary>Provider implementation of the module-end report.</summary>
        protected virtual Task OnModuleEndedAsync(string mode, string scenario, string module, bool passed) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepStartedAsync(StepBase step);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepCompletedAsync(StepBase step);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepFailedAsync(StepBase step, string reason);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task DisconnectAsync();

        /// <summary>Default: report the session as active (providers without a status endpoint).</summary>
        public virtual Task GetStatusAsync(string sessionId)
        {
            InvokeStatusUpdated(sessionId, SessionStatus.Active, CurrentModuleName);
            return Task.CompletedTask;
        }

        /// <summary>Default: forward to <see cref="SendEventAsync"/> as "module_info_started".</summary>
        public virtual Task ModuleInfoStartedAsync(string sessionId, string module) =>
            SendEventAsync("module_info_started", module);

        /// <summary>Default: forward to <see cref="SendEventAsync"/> as "module_info_ended".</summary>
        public virtual Task ModuleInfoEndedAsync(string sessionId, string module) =>
            SendEventAsync("module_info_ended", module);

        /// <summary>See the interface/base contract.</summary>
        public virtual UserScenarios GetUserScenarios() => Catalog;

        /// <summary>See the interface/base contract.</summary>
        public virtual string LaunchToken => null;
    }

    /// <summary>No-op platform session used when no backend is configured; calls log only.</summary>
    public class NullPlatformSession : IPlatformSession
    {
        /// <summary>See the interface/base contract.</summary>
        public bool IsConnected => false;
        /// <summary>See the interface/base contract.</summary>
        public string UserId => null;
        /// <summary>See the interface/base contract.</summary>
        public string SessionId => null;

        /// <summary>See the interface/base contract.</summary>
        public event Action Connected;
        /// <summary>See the interface/base contract.</summary>
        public event Action Disconnected;
        /// <summary>See the interface/base contract.</summary>
        public event Action ConnectionFailed;
        /// <summary>See the interface/base contract.</summary>
        public event Action NoActiveSession;
        /// <summary>See the interface/base contract.</summary>
        public event Action<string, SessionStatus, string> StatusUpdated;
        /// <summary>See the interface/base contract.</summary>
        public event Action Cancelled;
        /// <summary>See the interface/base contract.</summary>
        public event Action SessionRefreshed;
        /// <summary>See the interface/base contract.</summary>
        public string ConnectedBadgeId { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public string ConnectedUserId { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public string CurrentModuleName { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public string SelectedModuleName { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public string ServerBaseAddress => string.Empty;
        /// <summary>See the interface/base contract.</summary>
        public string LaunchToken => null;
        /// <summary>See the interface/base contract.</summary>
        public string StudentNickname { get; set; }
        /// <summary>See the interface/base contract.</summary>
        public Multiuser.ConnectionState State => Multiuser.ConnectionState.Disconnected;
        /// <summary>See the interface/base contract.</summary>
        public void SetSessionId(string sessionId) { }
        /// <summary>See the interface/base contract.</summary>
        public Task SendEventAsync(string action, string target) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task RefreshSessionDetails() => Task.CompletedTask;

        /// <summary>See the interface/base contract.</summary>
        public Task<bool> LoginAsync(string username, string password) => Task.FromResult(false);
        /// <summary>See the interface/base contract.</summary>
        public Task<bool> LoginWithPinAsync(int pin) => Task.FromResult(false);
        /// <summary>See the interface/base contract.</summary>
        public Task<bool> LoginWithTokenAsync(string token) => Task.FromResult(false);
        /// <summary>See the interface/base contract.</summary>
        public Task RefreshSessionAsync() => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task SetStatusAsync(string sessionId, SessionStatus status, string moduleScene) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task ModuleStartedAsync(string mode, string scenario, string module) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task ModuleEndedAsync(string mode, string scenario, string module, bool passed) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task StepStartedAsync(StepBase step) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task StepCompletedAsync(StepBase step) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task StepFailedAsync(StepBase step, string reason) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task DisconnectAsync() => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task GetStatusAsync(string sessionId) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task ModuleInfoStartedAsync(string sessionId, string module) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public Task ModuleInfoEndedAsync(string sessionId, string module) => Task.CompletedTask;
        /// <summary>See the interface/base contract.</summary>
        public UserScenarios GetUserScenarios() => new UserScenarios();
    }

    /// <summary>Static platform-level events mirrored from the active session.</summary>
    public static class PlatformEvents
    {
        /// <summary>Fired when a session connects.</summary>
        public static Action Connected;
        /// <summary>Fired when a session disconnects.</summary>
        public static Action Disconnected;
        /// <summary>Fired when a connection attempt fails.</summary>
        public static Action ConnectionFailed;
        /// <summary>Fired when the backend reports no active session.</summary>
        public static Action NoActiveSession;
        /// <summary>Session status update forwarded from the active session.</summary>
        public static Action<string, SessionStatus, string> StatusUpdated;
    }
}
