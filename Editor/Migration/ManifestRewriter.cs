using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>Options for <see cref="ManifestRewriter"/> and the migration run.</summary>
    public class MigrationOptions
    {
        /// <summary>Unity project root (the folder containing Assets/ and Packages/).</summary>
        public string ProjectRoot;

        /// <summary>When true, no files are modified — only the report is produced.</summary>
        public bool DryRun = true;

        /// <summary>Delete the "Luminous Packages" folder after rewriting.</summary>
        public bool DeleteLuminousPackages = false;

        /// <summary>Move licensed third-party assets (HighlightPlus, Ultimate Replay) from
        /// "Luminous Packages" into Assets/Plugins preserving guids.</summary>
        public bool RelocateThirdParty = true;

        /// <summary>Add HIGHLIGHT_PLUS to the project's scripting define symbols (all build targets).</summary>
        public bool AddHighlightPlusDefine = true;

        /// <summary>Dependency spec for com.pixovr.trainingcore (version, "file:../path", or git URL).</summary>
        public string TrainingCoreDependency = "file:../com.pixovr.trainingcore";

        /// <summary>Asset extensions to rewrite.</summary>
        public string[] Extensions = { ".unity", ".prefab", ".asset", ".playable", ".controller" };

        /// <summary>Manifest dependency version for Addressables.</summary>
        public string AddressablesVersion = "2.10.3";

        /// <summary>Manifest dependency version for the Input System.</summary>
        public string InputSystemVersion = "1.20.0";

        /// <summary>Manifest dependency version for Newtonsoft Json.</summary>
        public string NewtonsoftVersion = "3.2.1";

        /// <summary>Manifest dependency version for Timeline.</summary>
        public string TimelineVersion = "1.8.13";

        /// <summary>Manifest dependency version for ugui/TMP.</summary>
        public string UguiVersion = "2.0.0";

        /// <summary>NodeGraphProcessor git dependency spec.</summary>
        public string NodeGraphProcessorDependency =
            "https://github.com/alelievr/NodeGraphProcessor.git?path=/Assets/com.alelievr.NodeGraphProcessor#1.3.1";

        /// <summary>Report output path; null → &lt;project&gt;/Logs/luminous-migration-report.csv.</summary>
        public string ReportPath;
    }

    /// <summary>Pure manifest.json rewrite: drop Luminous deps/registry, add TrainingCore deps.</summary>
    public static class ManifestRewriter
    {
        /// <summary>Luminous package ids removed from dependencies.</summary>
        public static readonly string[] LuminousPackages =
        {
            "com.luminous.core",
            "com.luminous.core.middlemen",
            "com.luminous.core.middlemen.interactiontoolkit",
        };

        /// <summary>Scoped-registry names that identify the Luminous registry.</summary>
        public static bool IsLuminousRegistry(JObject registry)
        {
            var name = registry?["name"]?.ToString() ?? "";
            var url = registry?["url"]?.ToString() ?? "";
            return name.IndexOf("luminous", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   url.IndexOf("luminous", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Rewrite manifest.json text; returns the new text.</summary>
        public static string Rewrite(string manifestJson, MigrationOptions options, List<string> notes = null)
        {
            var root = JObject.Parse(manifestJson);
            var deps = root["dependencies"] as JObject;
            if (deps != null)
            {
                foreach (var p in LuminousPackages)
                    if (deps.Remove(p))
                        notes?.Add($"removed dependency {p}");
                deps["com.pixovr.trainingcore"] = options.TrainingCoreDependency;
                deps["com.unity.addressables"] = options.AddressablesVersion;
                deps["com.unity.inputsystem"] = options.InputSystemVersion;
                deps["com.unity.nuget.newtonsoft-json"] = options.NewtonsoftVersion;
                deps["com.unity.timeline"] = options.TimelineVersion;
                deps["com.unity.ugui"] = options.UguiVersion;
                deps["com.alelievr.nodegraphprocessor"] = options.NodeGraphProcessorDependency;
            }

            if (root["scopedRegistries"] is JArray regs)
            {
                for (int i = regs.Count - 1; i >= 0; i--)
                    if (regs[i] is JObject r && IsLuminousRegistry(r))
                    {
                        regs.RemoveAt(i);
                        notes?.Add("removed Luminous scoped registry");
                    }
                if (regs.Count == 0)
                    root.Remove("scopedRegistries");
            }

            return root.ToString(Formatting.Indented) + "\n";
        }
    }
}
