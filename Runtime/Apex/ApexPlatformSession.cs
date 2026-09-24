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

        /// <summary>Whether a user is logged in to the platform (not a session join).</summary>
        bool IsLoggedIn { get; }

        /// <summary>Clears the logged-in user state (no SDK logout exists).</summary>
        void ClearUser();

        /// <summary>Fetch the org modules available to the current user.</summary>
        void GetCurrentUserModules(System.Action<bool, System.Collections.Generic.IReadOnlyList<OrgModule>> done);
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
        public bool IsLoggedIn => !string.IsNullOrEmpty(UserId);

        /// <inheritdoc/>
        public void ClearUser()
        {
            UserId = null;
        }

        /// <inheritdoc/>
        public void Login(string user, string pass, Action<bool, string> done)
        {
            ApexSystem.Login(user, pass,
                (resp, info) => HandleLoginSuccess(info, done),
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "login failed"));
        }

        /// <inheritdoc/>
        public void LoginWithToken(string token, Action<bool, string> done)
        {
            ApexSystem.LoginWithToken(token,
                (resp, info) => HandleLoginSuccess(info, done),
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "token login failed"));
        }

        /// <inheritdoc/>
        public void QuickIdLogin(string serial, string username, Action<bool, string> done)
        {
            ApexSystem.QuickIDLogin(serial, username,
                (resp, info) => HandleLoginSuccess(info, done),
                (resp, fail) => done?.Invoke(false, fail?.ToString() ?? "quickid login failed"));
        }

        private void HandleLoginSuccess(ActiveUserInformation info, Action<bool, string> done)
        {
            if (ApexSystem.LoginCheckModuleAccess && info?.ModuleUserInformation == null)
                return;
            if (info?.ModuleUserInformation != null && !info.ModuleUserInformation.Access)
            {
                done?.Invoke(false, "User has no access to this module");
                return;
            }
            UserId = info?.User?.ID.ToString();
            done?.Invoke(true, null);
        }

        /// <inheritdoc/>
        public void JoinSession(string scenarioId, Extension context, Action<bool, int> done)
        {
            ApexSystem.JoinSession(scenarioId, context,
                (resp, join) =>
                {
                    SessionId = join != null ? join.SessionId : 0;
                    Log.Info($"[Apex Diag] JoinSession ok: sessionId={SessionId}", LogCategory.Platform);
                    done?.Invoke(true, SessionId);
                },
                (resp, fail) =>
                {
                    Log.Warning($"[Apex Diag] JoinSession failed: {fail?.Message}", LogCategory.Platform);
                    done?.Invoke(false, 0);
                });
        }

        /// <inheritdoc/>
        public void CompleteSession(SessionData data, Extension context, Extension result, Action<bool> done)
        {
            ApexSystem.CompleteSession(data, context, result,
                (resp, o) =>
                {
                    Log.Info("[Apex Diag] CompleteSession ok", LogCategory.Platform);
                    done?.Invoke(true);
                },
                (resp, fail) =>
                {
                    Log.Warning($"[Apex Diag] CompleteSession failed: {fail?.Message}", LogCategory.Platform);
                    done?.Invoke(false);
                });
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

        /// <inheritdoc/>
        public void GetCurrentUserModules(Action<bool, System.Collections.Generic.IReadOnlyList<OrgModule>> done)
        {
            if (ApexSystem.Instance == null)
            {
                done?.Invoke(false, null);
                return;
            }

            System.Collections.Generic.List<int> ids = null;
            System.Collections.Generic.List<OrgModule> all = null;
            var pending = 2;

            void Finish()
            {
                if (--pending != 0)
                    return;
                var mods = new System.Collections.Generic.List<OrgModule>();
                if (all != null)
                {
                    foreach (var m in all)
                    {
                        if (m == null)
                            continue;
                        if (ids == null || ids.Contains(m.ID))
                            mods.Add(m);
                    }
                }
                done?.Invoke(all != null, mods);
            }

            // apexunitysdk dev: module fetches take (HttpResponseMessage, T) callbacks directly.
            if (!ApexSystem.GetCurrentUserModules(
                (resp, r) =>
                {
                    ids = new System.Collections.Generic.List<int>();
                    if (r?.ParsedData != null)
                    {
                        foreach (var u in r.ParsedData)
                        {
                            if (u?.AvailableModules == null)
                                continue;
                            foreach (var id in u.AvailableModules)
                            {
                                if (!ids.Contains(id))
                                    ids.Add(id);
                            }
                        }
                    }
                    Finish();
                },
                (resp, fail) => Finish()))
            {
                Finish();
            }

            if (!ApexSystem.GetModulesList(null,
                (resp, o) =>
                {
                    if (o is UserModulesResponse umr && umr.modules != null)
                    {
                        all = new System.Collections.Generic.List<OrgModule>();
                        foreach (var m in umr.modules)
                        {
                            var om = m.ToOrgModule();
                            if (om != null)
                                all.Add(om);
                        }
                    }
                    Finish();
                },
                (resp, fail) => Finish()))
            {
                Finish();
            }
        }
    }

    /// <summary>
    /// <see cref="PlatformSessionBase"/> implemented on top of the Apex Unity SDK.
    /// </summary>
    public class ApexPlatformSession : PlatformSessionBase
    {
        /// <summary>Credentials/config asset driving the session.</summary>
        public ApexCredentialsConfig Config;

        /// <summary>Project-authored scenario catalog; when set it replaces the Apex module query.</summary>
        public ScenarioCatalog ScenarioCatalog;

        /// <summary>SDK client (defaults to <see cref="ApexSystemClient"/>; inject a fake for tests).</summary>
        public IApexClient Client = new ApexSystemClient();

        private string _sessionId;
        private string _userId;

        /// <summary>Assigns <paramref name="catalog"/> and applies it to <see cref="Catalog"/> immediately.</summary>
        public void ApplyScenarioCatalog(ScenarioCatalog catalog)
        {
            ScenarioCatalog = catalog;
            Catalog = catalog != null ? catalog.ToUserScenarios() : new UserScenarios();
        }

        /// <summary>Seeds <see cref="Catalog"/> from <see cref="ScenarioCatalog"/> so it exists before/without login.</summary>
        protected override void Start()
        {
            base.Start();
            if (ScenarioCatalog != null)
                ApplyScenarioCatalog(ScenarioCatalog);
        }

        /// <inheritdoc/>
        public override bool IsConnected => Client != null && Client.IsLoggedIn;

        /// <inheritdoc/>
        public override string ConnectedUserId { get => UserId; set => _userId = value; }

        /// <inheritdoc/>
        public override string UserId => _userId ?? Client?.UserId;

        /// <inheritdoc/>
        public override string SessionId => _sessionId;

        /// <inheritdoc/>
        public override void SetSessionId(string sessionId)
        {
            base.SetSessionId(sessionId);
            _sessionId = sessionId;
        }

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
                    RefreshCatalog();
                }
                else
                {
                    InvokeConnectionFailed();
                    Log.Warning($"Apex login failed: {err}", LogCategory.Platform);
                }
                tcs.TrySetResult(ok);
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
                    {
                        InvokeConnected();
                        RefreshCatalog();
                    }
                    else
                    {
                        InvokeConnectionFailed();
                        Log.Warning($"Apex PIN/QuickID login failed: {err}", LogCategory.Platform);
                    }
                    tcs.TrySetResult(ok);
                });
                return tcs.Task;
            }
            Log.Warning("ApexPlatformSession.LoginWithPinAsync: no PIN login in the Apex SDK; " +
                        "configure DeviceSerialNumber to use QuickID login instead", LogCategory.Platform);
            InvokeConnectionFailed();
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public override Task<bool> LoginWithTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                token = LaunchToken;
            if (string.IsNullOrEmpty(token))
            {
                Log.Warning("ApexPlatformSession.LoginWithTokenAsync: no token supplied and no launch token", LogCategory.Platform);
                InvokeConnectionFailed();
                return Task.FromResult(false);
            }
            var tcs = new TaskCompletionSource<bool>();
            Client.LoginWithToken(token, (ok, err) =>
            {
                if (ok)
                {
                    _userId = Client.UserId;
                    InvokeConnected();
                    RefreshCatalog();
                }
                else
                {
                    InvokeConnectionFailed();
                    Log.Warning($"Apex token login failed: {err}", LogCategory.Platform);
                }
                tcs.TrySetResult(ok);
            });
            return tcs.Task;
        }

        /// <summary>Populate <see cref="Catalog"/> from the user's available org modules.</summary>
        protected virtual void RefreshCatalog()
        {
            if (ScenarioCatalog != null)
            {
                Catalog = ScenarioCatalog.ToUserScenarios();
                return;
            }
            Client.GetCurrentUserModules((ok, modules) =>
            {
                if (!ok || modules == null)
                    return;
                var catalog = new UserScenarios();
                foreach (var m in modules)
                {
                    catalog.AvailableScenarios.Add(new Scenario
                    {
                        ScenarioName = m.Name,
                        ScenarioId = m.ID.ToString(),
                        Modules =
                        {
                            new PixoVR.TrainingCore.Platform.Module
                            {
                                Name = m.Name,
                                Description = m.Description,
                                SceneToLoad = m.Name,
                                ImageAddress = m.IconURL
                            }
                        }
                    });
                }
                Catalog = catalog;
            });
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
        public override string LaunchToken => ApexSystem.PassedLoginToken;

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
        public override Task GetStatusAsync(string sessionId)
        {
            InvokeStatusUpdated(sessionId,
                !string.IsNullOrEmpty(_sessionId) ? SessionStatus.Active : SessionStatus.Created,
                CurrentModuleName);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override Task OnModuleStartedAsync(string mode, string scenario, string module)
        {
            var tcs = new TaskCompletionSource<bool>();
            Log.Info($"[Apex Diag] OnModuleStartedAsync: joining session (scenario={Config?.ScenarioId ?? scenario}, module={module})", LogCategory.Platform);
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
        protected override Task OnModuleEndedAsync(string mode, string scenario, string module, bool passed)
        {
            var tcs = new TaskCompletionSource<bool>();
            Log.Info($"[Apex Diag] OnModuleEndedAsync: completing session (module={module}, passed={passed}, sessionId={_sessionId})", LogCategory.Platform);
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
            _userId = null;
            _sessionId = null;
            Client?.ClearUser();
            InvokeDisconnected();
            return Task.CompletedTask;
        }
    }


}
