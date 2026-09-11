using System;
using System.Threading.Tasks;
using PixoVR.TrainingCore.Flow;
using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>Lifecycle status of a training session on the platform.</summary>
    public enum SessionStatus
    {
        /// <summary>Status not yet known.</summary>
        Unknown,
        /// <summary>Session is scheduled but not started.</summary>
        Scheduled,
        /// <summary>Session is running.</summary>
        InProgress,
        /// <summary>Session finished successfully.</summary>
        Completed,
        /// <summary>Session was cancelled.</summary>
        Cancelled,
        /// <summary>Session failed.</summary>
        Failed
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

        /// <summary>Registers this provider as <see cref="Instance"/>.</summary>
        protected virtual void Awake() => Instance = this;

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
        /// <summary>See the interface/base contract.</summary>
        public abstract Task ModuleStartedAsync(string mode, string scenario, string module);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task ModuleEndedAsync(string mode, string scenario, string module, bool passed);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepStartedAsync(StepBase step);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepCompletedAsync(StepBase step);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task StepFailedAsync(StepBase step, string reason);
        /// <summary>See the interface/base contract.</summary>
        public abstract Task DisconnectAsync();
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
