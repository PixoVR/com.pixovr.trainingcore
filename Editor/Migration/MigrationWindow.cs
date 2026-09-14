using UnityEditor;
using UnityEngine;

namespace PixoVR.TrainingCore.Editor.Migration
{
    /// <summary>Tools/PixoVR/Migrate Luminous Project… window.</summary>
    public class MigrationWindow : EditorWindow
    {
        private MigrationOptions _options = new MigrationOptions { DryRun = true };
        private Vector2 _scroll;
        private LuminousMigrator.Result _lastResult;

        /// <summary>Open the migration window.</summary>
        [MenuItem("Tools/PixoVR/Migrate Luminous Project…")]
        public static void Open() => GetWindow<MigrationWindow>("Migrate Luminous Project");

        private void OnEnable() => _options.ProjectRoot = System.IO.Directory.GetCurrentDirectory();

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("Luminous → PixoVR TrainingCore migration", EditorStyles.boldLabel);
            _options.ProjectRoot = EditorGUILayout.TextField("Project root", _options.ProjectRoot);
            _options.TrainingCoreDependency = EditorGUILayout.TextField("PixoVR package dep", _options.TrainingCoreDependency);
            _options.DryRun = EditorGUILayout.Toggle("Dry run (report only)", _options.DryRun);
            _options.DeleteLuminousPackages = EditorGUILayout.Toggle("Delete Luminous Packages/", _options.DeleteLuminousPackages);
            _options.RelocateThirdParty = EditorGUILayout.Toggle("Relocate third-party (HighlightPlus…)", _options.RelocateThirdParty);
            _options.AddHighlightPlusDefine = EditorGUILayout.Toggle("Add HIGHLIGHT_PLUS define", _options.AddHighlightPlusDefine);

            EditorGUILayout.Space();
            if (GUILayout.Button(_options.DryRun ? "Run dry-run" : "RUN MIGRATION"))
            {
                _lastResult = LuminousMigrator.Run(_options);
            }

            if (_lastResult != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Files touched: {_lastResult.FilesTouched}");
                EditorGUILayout.LabelField($"Refs rewritten: {_lastResult.RefsRewritten}");
                EditorGUILayout.LabelField($"Unmapped refs: {_lastResult.Unmapped}");
                EditorGUILayout.LabelField("Report: Logs/luminous-migration-report.csv");
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
