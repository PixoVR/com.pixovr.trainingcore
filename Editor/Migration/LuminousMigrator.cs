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

        /// <summary>Resolve a Pixo class to its .cs meta guid via AssetDatabase (class may live in an aggregated file).</summary>
        private static string ResolvePixoGuid(string ns, string className)
        {
            if (string.IsNullOrEmpty(className))
                return null;
            // classes may be aggregated several-per-file, so match file text rather than file names
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { "Packages/com.pixovr.trainingcore" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                var text = File.ReadAllText(path);
                if (System.Text.RegularExpressions.Regex.IsMatch(text,
                        @"\bclass\s+" + System.Text.RegularExpressions.Regex.Escape(className) + @"\b") &&
                    (string.IsNullOrEmpty(ns) || text.Contains("namespace " + ns)))
                    return guid;
            }
            return null;
        }

        private static void WriteReport(List<MigrationReportEntry> report, string path)
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
