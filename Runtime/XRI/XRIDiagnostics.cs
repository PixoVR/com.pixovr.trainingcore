using UnityEngine;

namespace PixoVR.TrainingCore.XRI
{
    /// <summary>Opt-out runtime diagnostics for the XRI interaction chain.</summary>
    public static class XRIDiagnostics
    {
        /// <summary>Master switch for all XRI diagnostic logging.</summary>
        public static bool Enabled = true;

        /// <summary>Log a diagnostic message with the shared "[XRI Diag]" prefix.</summary>
        public static void Log(string msg, Object ctx = null)
        {
            if (Enabled)
                Debug.Log("[XRI Diag] " + msg, ctx);
        }
    }
}
