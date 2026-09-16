using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>
    /// Orchestrates the Luminous→PixoVR project migration: YAML rewrite, manifest rewrite,
    /// optional package deletion, dry-run reporting.
    /// </summary>
    public static class LuminousMigrator
    {
        /// <summary>Package-relative path of the shipped mapping table.</summary>
        public const string MapAssetPath = "Packages/com.pixovr.trainingcore/Editor/Migration/luminous-map.json";

        /// <summary>Result summary.</summary>
        public class Result
        {
            /// <summary>Files rewritten (or that would be, in dry run).</summary>
            public int FilesTouched;

            /// <summary>Individual refs rewritten.</summary>
            public int RefsRewritten;

            /// <summary>Unmapped references.</summary>
            public int Unmapped;

            /// <summary>Full report rows.</summary>
            public List<MigrationReportEntry> Report = new List<MigrationReportEntry>();

            /// <summary>Manifest changes made.</summary>
            public List<string> ManifestNotes = new List<string>();
        }

        /// <summary>Run the migration over a Unity project.</summary>
        public static Result Run(MigrationOptions options)
        {
            var result = new Result();
            var root = options.ProjectRoot ?? Directory.GetCurrentDirectory();
            var map = MigrationMap.LoadFile(ResolveMapPath());
            var report = result.Report;

            var exts = new HashSet<string>(options.Extensions ?? new string[0], StringComparer.OrdinalIgnoreCase);
            var searchRoots = new[] { Path.Combine(root, "Assets") };
            foreach (var dir in searchRoots)
            {
                if (!Directory.Exists(dir))
                    continue;
                foreach (var file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
                {
                    if (!exts.Contains(Path.GetExtension(file)))
                        continue;
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var text = File.ReadAllText(file);
                    var rel = file.Substring(root.Length).TrimStart('/', '\\');
                    var rewritten = YamlRewriter.Rewrite(text, map, ResolvePixoGuid, report, rel);
                    if (!ReferenceEquals(rewritten, text))
                    {
                        result.FilesTouched++;
                        if (!options.DryRun)
                            File.WriteAllText(file, rewritten);
                    }
                }
            }

            result.RefsRewritten = report.Count(r => r.Status == "mapped" || r.Status == "field-renamed");
            result.Unmapped = report.Count(r => r.Status.StartsWith("unmapped") || r.Status == "unresolved-pixo-script");

            var manifestPath = Path.Combine(root, "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                var rewritten = ManifestRewriter.Rewrite(File.ReadAllText(manifestPath), options, result.ManifestNotes);
                if (!options.DryRun)
                    File.WriteAllText(manifestPath, rewritten);
            }

            if (options.RelocateThirdParty)
                RelocateThirdPartyAssets(root, options.DryRun, result.ManifestNotes);

            if (options.AddHighlightPlusDefine)
            {
                if (!options.DryRun)
                    AddScriptingDefine("HIGHLIGHT_PLUS");
                else
                    result.ManifestNotes.Add("would add scripting define HIGHLIGHT_PLUS");
            }

            if (!options.DryRun && options.DeleteLuminousPackages)
            {
                var lp = Path.Combine(root, "Luminous Packages");
                if (Directory.Exists(lp))
                    Directory.Delete(lp, true);
            }

            WriteReport(report, options.ReportPath ?? Path.Combine(root, "Logs", "luminous-migration-report.csv"));
            return result;
        }

        /// <summary>Entry point for -executeMethod batchmode migration.</summary>
        public static void RunFromCommandLine()
        {
            var options = new MigrationOptions { DryRun = false };
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-luminousProjectRoot" && i + 1 < args.Length)
                    options.ProjectRoot = args[++i];
                else if (args[i] == "-luminousDryRun")
                    options.DryRun = true;
                else if (args[i] == "-luminousPackageSource" && i + 1 < args.Length)
                    options.TrainingCoreDependency = args[++i];
            }
            var result = Run(options);
            UnityEngine.Debug.Log($"LuminousMigrator: {result.FilesTouched} files, {result.RefsRewritten} refs, {result.Unmapped} unmapped");
        }

        private static string ResolveMapPath()
        {
            if (File.Exists(MapAssetPath))
                return MapAssetPath;
            // find the map inside the package wherever it is installed
            var found = AssetDatabase.FindAssets("luminous-map")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.EndsWith("luminous-map.json"));
            return found;
        }

        /// <summary>Resolve a Pixo class to its .cs meta guid via AssetDatabase. Unity binds m_Script
        /// references by file name, so only a class declared in a file of the same name is a valid target.</summary>
        private static string ResolvePixoGuid(string ns, string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Packages/com.pixovr.trainingcore" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Path.GetFileNameWithoutExtension(path) != className)
                    continue;
                var text = File.ReadAllText(path);
                if (System.Text.RegularExpressions.Regex.IsMatch(text,
                        @"\bclass\s+" + System.Text.RegularExpressions.Regex.Escape(className) + @"\b") &&
                    (string.IsNullOrEmpty(ns) || text.Contains("namespace " + ns)))
                    return guid;
            }
            return null;
        }

        /// <summary>Names of third-party asset folders relocated into Assets/Plugins.</summary>
        public static readonly string[] RelocatableFolderNames = { "HighlightPlus", "Ultimate Replay", "Ultimate Replay 2.0" };

        /// <summary>Loose third-party scripts relocated alongside (file name → plugin folder).</summary>
        public static readonly (string File, string IntoFolder)[] RelocatableFiles =
        {
            ("HighlightPlusRenderPassFeature.cs", "HighlightPlus"),
        };

        public static void RelocateThirdPartyAssets(string root, bool dryRun, List<string> notes)
        {
            var luminousRoot = Path.Combine(root, "Luminous Packages");
            if (!Directory.Exists(luminousRoot))
                return;
            var pluginsDir = Path.Combine(root, "Assets", "Plugins");
            var found = new List<string>();
            foreach (var name in RelocatableFolderNames)
                found.AddRange(FindDirectoriesNamed(luminousRoot, name));
            foreach (var dir in found)
            {
                var name = new DirectoryInfo(dir).Name;
                var dest = Path.Combine(pluginsDir, name);
                if (dryRun)
                {
                    notes?.Add($"would relocate {dir.Substring(root.Length).TrimStart('/', '\\')} → Assets/Plugins/{name}");
                    continue;
                }
                if (Directory.Exists(dest))
                    continue;
                Directory.CreateDirectory(pluginsDir);
                Directory.Move(dir, dest);
                var meta = dir + ".meta";
                if (File.Exists(meta))
                    File.Move(meta, dest + ".meta");
                notes?.Add($"relocated {name} → Assets/Plugins/{name}");
            }
            foreach (var (file, intoFolder) in RelocatableFiles)
            {
                foreach (var path in FindFilesNamed(luminousRoot, file))
                {
                    var destDir = Path.Combine(pluginsDir, intoFolder);
                    var dest = Path.Combine(destDir, file);
                    if (dryRun)
                    {
                        notes?.Add($"would relocate {path.Substring(root.Length).TrimStart('/', '\\')} → Assets/Plugins/{intoFolder}/{file}");
                        continue;
                    }
                    if (File.Exists(dest))
                        continue;
                    Directory.CreateDirectory(destDir);
                    File.Move(path, dest);
                    var meta = path + ".meta";
                    if (File.Exists(meta))
                        File.Move(meta, dest + ".meta");
                    notes?.Add($"relocated {file} → Assets/Plugins/{intoFolder}");
                }
            }
        }

        public static IEnumerable<string> FindFilesNamed(string root, string name)
        {
            var found = new List<string>();
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                foreach (var f in Directory.EnumerateFiles(dir))
                    if (string.Equals(Path.GetFileName(f), name, StringComparison.Ordinal))
                        found.Add(f);
                foreach (var sub in Directory.EnumerateDirectories(dir))
                    stack.Push(sub);
            }
            return found;
        }

        public static IEnumerable<string> FindDirectoriesNamed(string root, string name)
        {
            var found = new List<string>();
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    if (string.Equals(new DirectoryInfo(sub).Name, name, StringComparison.Ordinal))
                        found.Add(sub);
                    else
                        stack.Push(sub);
                }
            }
            return found;
        }

        private static void AddScriptingDefine(string define)
        {
            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;
                try
                {
                    var named = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    var symbols = PlayerSettings.GetScriptingDefineSymbols(named);
                    if (symbols.Split(';').Contains(define))
                        continue;
                    PlayerSettings.SetScriptingDefineSymbols(named,
                        string.IsNullOrEmpty(symbols) ? define : symbols + ";" + define);
                }
                catch (Exception)
                {
                    // group not installed/supported — skip
                }
            }
        }

        public static void WriteReport(List<MigrationReportEntry> report, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var w = new StreamWriter(path))
            {
                w.WriteLine("file,line,from,to,status");
                foreach (var e in report)
                    w.WriteLine(e.ToCsv());
                var unmapped = report.Where(r => r.Status.StartsWith("unmapped")).ToList();
                if (unmapped.Count > 0)
                {
                    w.WriteLine();
                    w.WriteLine("unmapped types:");
                    foreach (var g in unmapped.GroupBy(u => u.From))
                        w.WriteLine($"{g.Key},{g.Count()}");
                }
            }
        }
    }
}
