using System.Threading.Tasks;
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
        /// <summary>Username+password login.</summary>
        Task LoginAsync(string username, string password);

        /// <summary>PIN login.</summary>
        Task LoginWithPinAsync(string username, string pin);

        /// <summary>Resume from a stored token.</summary>
        Task LoginWithTokenAsync(string token);

        /// <summary>Revalidate an expiring session.</summary>
        Task RefreshSessionAsync();

        /// <summary>Report the session status.</summary>
        Task SetStatusAsync(SessionStatus status);

        /// <summary>Report that a module run started.</summary>
        Task ModuleStartedAsync(string moduleId);

        /// <summary>Report that a module run ended with the given status.</summary>
        Task ModuleEndedAsync(string moduleId, SessionStatus status);

        /// <summary>Report that a step started.</summary>
        Task StepStartedAsync(string stepId);

        /// <summary>Report that a step completed.</summary>
        Task StepCompletedAsync(string stepId);

        /// <summary>Report that a step failed.</summary>
        Task StepFailedAsync(string stepId, string reason);

        /// <summary>Terminate the session.</summary>
        Task DisconnectAsync();
    }

    /// <summary>Base class for platform session providers; registered as <see cref="Instance"/>.</summary>
    public abstract class PlatformSessionBase : Utility.SingletonBehaviour<PlatformSessionBase>, IPlatformSession
    {
        
        public abstract Task LoginAsync(string username, string password);
        
        public abstract Task LoginWithPinAsync(string username, string pin);
        
        public abstract Task LoginWithTokenAsync(string token);
        
        public abstract Task RefreshSessionAsync();
        
        public abstract Task SetStatusAsync(SessionStatus status);
        
        public abstract Task ModuleStartedAsync(string moduleId);
        
        public abstract Task ModuleEndedAsync(string moduleId, SessionStatus status);
        
        public abstract Task StepStartedAsync(string stepId);
        
        public abstract Task StepCompletedAsync(string stepId);
        
        public abstract Task StepFailedAsync(string stepId, string reason);
        
        public abstract Task DisconnectAsync();
    }

    /// <summary>No-op platform session used when no backend is configured.</summary>
    public class NullPlatformSession : IPlatformSession
    {
        public Task LoginAsync(string username, string password) => Task.CompletedTask;
        public Task LoginWithPinAsync(string username, string pin) => Task.CompletedTask;
        public Task LoginWithTokenAsync(string token) => Task.CompletedTask;
        public Task RefreshSessionAsync() => Task.CompletedTask;
        public Task SetStatusAsync(SessionStatus status) => Task.CompletedTask;
        public Task ModuleStartedAsync(string moduleId) => Task.CompletedTask;
        public Task ModuleEndedAsync(string moduleId, SessionStatus status) => Task.CompletedTask;
        public Task StepStartedAsync(string stepId) => Task.CompletedTask;
        public Task StepCompletedAsync(string stepId) => Task.CompletedTask;
        public Task StepFailedAsync(string stepId, string reason) => Task.CompletedTask;
        public Task DisconnectAsync() => Task.CompletedTask;
    }
}
