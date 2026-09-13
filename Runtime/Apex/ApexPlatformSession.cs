using System;
using System.Net.Http;
using System.Threading.Tasks;
using PixoVR.Apex;
using PixoVR.Apex.XAPI;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Platform;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Apex
{
    /// <summary>
    /// Thin interface over the Apex SDK's static <see cref="ApexSystem"/> facade so the session
    /// can be unit-tested with a fake client.
    /// </summary>
    public interface IApexClient
    {
        /// <summary>User/pass login.</summary>
        void Login(string user, string pass, Action<bool, string> done);

        /// <summary>Token login.</summary>
        void LoginWithToken(string token, Action<bool, string> done);

        /// <summary>QuickID login.</summary>
        void QuickIdLogin(string serial, string username, Action<bool, string> done);

        /// <summary>Join/start a session.</summary>
        void JoinSession(string scenarioId, Extension context, Action<bool, int> done);

        /// <summary>Complete the active session.</summary>
        void CompleteSession(SessionData data, Extension context, Extension result, Action<bool> done);

        /// <summary>Send a simple session event.</summary>
        void SendSimpleSessionEvent(string action, string target, Extension context, Action<bool> done);

        /// <summary>Send a full xAPI statement.</summary>
        void SendSessionEvent(TinCan.Statement statement, Action<bool> done);

        /// <summary>Keepalive.</summary>
        void Ping(Action<bool> done);

        /// <summary>Whether a session is in progress (SDK-side heartbeat flag).</summary>
        bool IsSessionInProgress { get; }

        /// <summary>Current session id (set by JoinSession).</summary>
        int SessionId { get; }

        /// <summary>Current user id.</summary>
        string UserId { get; }
    }

    /// <summary>Default <see cref="IApexClient"/> backed by <see cref="ApexSystem"/>.</summary>
    public class ApexSystemClient : IApexClient
    {
        /// <inheritdoc/>
        public bool IsSessionInProgress => ApexSystem.IsSessionInProgress;

        /// <inheritdoc/>
        public int SessionId { get; private set; }

        /// <inheritdoc/>
        public string UserId { get; private set; }

        /// <inheritdoc/>
        public void Login(string user, string pass, Action<bool, string> done)
        {
            ApexSystem.Login(user, pass,
                (resp, info) =>
                {
                    UserId = info?.User?.ID.ToString();
                    done?.Invoke(true, null);
                },
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "login failed"));
        }

        /// <inheritdoc/>
        public void LoginWithToken(string token, Action<bool, string> done)
        {
            ApexSystem.LoginWithToken(token,
                (resp, info) =>
                {
                    UserId = info?.User?.ID.ToString();
                    done?.Invoke(true, null);
                },
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "token login failed"));
        }

        /// <inheritdoc/>
        public void QuickIdLogin(string serial, string username, Action<bool, string> done)
        {
            ApexSystem.QuickIDLogin(serial, username,
                (resp, info) =>
                {
                    UserId = info?.User?.ID.ToString();
                    done?.Invoke(true, null);
                },
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "quickid login failed"));
        }

        /// <inheritdoc/>
        public void JoinSession(string scenarioId, Extension context, Action<bool, int> done)
        {
            ApexSystem.JoinSession(scenarioId, context,
                (resp, join) =>
                {
                    SessionId = join != null ? join.SessionId : 0;
                    done?.Invoke(true, SessionId);
                },
                (resp, fail) => done?.Invoke(false, 0));
        }

        /// <inheritdoc/>
        public void CompleteSession(SessionData data, Extension context, Extension result, Action<bool> done)
        {
            ApexSystem.CompleteSession(data, context, result,
                (resp, o) => done?.Invoke(true),
                (resp, fail) => done?.Invoke(false));
        }

        /// <inheritdoc/>
        public void SendSimpleSessionEvent(string action, string target, Extension context, Action<bool> done)
        {
            ApexSystem.SendSimpleSessionEvent(action, target, context,
                (resp, o) => done?.Invoke(true),
                (resp, fail) => done?.Invoke(false));
        }

        /// <inheritdoc/>
        public void SendSessionEvent(TinCan.Statement statement, Action<bool> done)
        {
            ApexSystem.SendSessionEvent(statement,
                (resp, o) => done?.Invoke(true),
                (resp, fail) => done?.Invoke(false));
        }

        /// <inheritdoc/>
        public void Ping(Action<bool> done)
        {
            ApexSystem.Ping((resp, o) => done?.Invoke(true), (resp, fail) => done?.Invoke(false));
        }
    }

    /// <summary>
    /// <see cref="PlatformSessionBase"/> implemented on top of the Apex Unity SDK.
    /// </summary>
    public class ApexPlatformSession : PlatformSessionBase
    {
        /// <summary>Credentials/config asset driving the session.</summary>
        public ApexCredentialsConfig Config;

        /// <summary>SDK client (defaults to <see cref="ApexSystemClient"/>; inject a fake for tests).</summary>
        public IApexClient Client = new ApexSystemClient();

        private string _sessionId;
        private string _userId;

        /// <inheritdoc/>
        public override bool IsConnected => Client != null && Client.IsSessionInProgress;

        /// <inheritdoc/>
        public override string UserId => _userId ?? Client?.UserId;

        /// <inheritdoc/>
        public override string SessionId => _sessionId;

        /// <inheritdoc/>
        public override Task<bool> LoginAsync(string username, string password)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.Login(username, password, (ok, err) =>
            {
                if (ok)
                {
                    _userId = Client.UserId;
                    InvokeConnected();
                }
                else
                {
                    InvokeConnectionFailed();
                    Log.Warning($"Apex login failed: {err}", LogCategory.Platform);
                }
                tcs.SetResult(ok);
            });
            return tcs.Task;
        }

        /// <summary>
        /// PIN login has no Apex SDK equivalent — uses QuickID serial+username login via
        /// <see cref="SessionContext.userPin"/> as the username when a serial is configured.
        /// </summary>
        public override Task<bool> LoginWithPinAsync(int pin)
        {
            if (Config != null && !string.IsNullOrEmpty(Config.DeviceSerialNumber))
            {
                var tcs = new TaskCompletionSource<bool>();
                Client.QuickIdLogin(Config.DeviceSerialNumber, pin.ToString(), (ok, err) =>
                {
                    if (ok)
                        InvokeConnected();
                    tcs.SetResult(ok);
                });
                return tcs.Task;
            }
            Log.Warning("ApexPlatformSession.LoginWithPinAsync: no PIN login in the Apex SDK; " +
                        "configure DeviceSerialNumber to use QuickID login instead", LogCategory.Platform);
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public override Task<bool> LoginWithTokenAsync(string token)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.LoginWithToken(token, (ok, err) =>
            {
                if (ok)
                {
                    _userId = Client.UserId;
                    InvokeConnected();
                }
                tcs.SetResult(ok);
            });
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task RefreshSessionAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.Ping(ok =>
            {
                if (ok)
                    InvokeSessionRefreshed();
                tcs.SetResult(ok);
            });
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override string ServerBaseAddress => ApexSystem.APIEndpoint;

        /// <inheritdoc/>
        public override Task SendEventAsync(string action, string target)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.SendSimpleSessionEvent(action, target ?? string.Empty, null, ok => tcs.SetResult(ok));
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task SetStatusAsync(string sessionId, SessionStatus status, string moduleScene)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.SendSimpleSessionEvent("status", $"{status}", new Extension(new System.Collections.Generic.Dictionary<string, string>
            {
                { "sessionId", sessionId ?? "" },
                { "moduleScene", moduleScene ?? "" }
            }), ok => tcs.SetResult(ok));
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task ModuleStartedAsync(string mode, string scenario, string module)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.JoinSession(Config != null ? Config.ScenarioId : scenario, null, (ok, sessionId) =>
            {
                if (ok)
                {
                    _sessionId = sessionId.ToString();
                    InvokeStatusUpdated(_sessionId, SessionStatus.Active, module);
                }
                tcs.SetResult(ok);
            });
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task ModuleEndedAsync(string mode, string scenario, string module, bool passed)
        {
            var tcs = new TaskCompletionSource<bool>();
            var data = new SessionData(passed ? 100f : 0f, passed ? 1f : 0f, 0f, 100f, 0, true, passed);
            Client.CompleteSession(data, null, null, ok =>
            {
                InvokeStatusUpdated(_sessionId, passed ? SessionStatus.Complete : SessionStatus.Complete, module);
                tcs.SetResult(ok);
            });
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task StepStartedAsync(StepBase step) =>
            SendStepEvent("step_started", step);

        /// <inheritdoc/>
        public override Task StepCompletedAsync(StepBase step) =>
            SendStepEvent("step_completed", step);

        /// <inheritdoc/>
        public override Task StepFailedAsync(StepBase step, string reason) =>
            SendStepEvent($"step_failed:{reason}", step);

        private Task SendStepEvent(string action, StepBase step)
        {
            var tcs = new TaskCompletionSource<bool>();
            Client.SendSimpleSessionEvent(action, step?.Name ?? step?.GUID ?? "", null, ok => tcs.SetResult(ok));
            return tcs.Task;
        }

        /// <inheritdoc/>
        public override Task DisconnectAsync()
        {
            InvokeDisconnected();
            _sessionId = null;
            return Task.CompletedTask;
        }
    }

    /// <summary>Configuration for <see cref="ApexPlatformSession"/> — holds no secrets.</summary>
    [CreateAssetMenu(fileName = "ApexCredentialsConfig", menuName = "TrainingCore/Apex Credentials Config")]
    public class ApexCredentialsConfig : ScriptableObject
    {
        /// <summary>Apex scenario id.</summary>
        public string ScenarioId;

        /// <summary>Device serial used by QuickID login.</summary>
        public string DeviceSerialNumber;

        /// <summary>Scenario name (informational).</summary>
        public string ScenarioName;

        /// <summary>Optional login token override.</summary>
        public string Token;
    }

    /// <summary>Installs an <see cref="ApexPlatformSession"/> as the active <see cref="PlatformSessionBase.Instance"/>.</summary>
    public class ApexPlatformBootstrap : MonoBehaviour
    {
        /// <summary>The session component to install.</summary>
        public ApexPlatformSession Session;

        /// <summary>Optional config asset to apply.</summary>
        public ApexCredentialsConfig Config;

        private void Awake()
        {
            var session = Session != null ? Session : GetComponent<ApexPlatformSession>();
            if (session == null)
            {
                session = gameObject.AddComponent<ApexPlatformSession>();
            }
            if (Config != null)
                session.Config = Config;
            // PlatformSessionBase.Awake sets Instance; ensure this component exists before the flow starts.
        }
    }
}
