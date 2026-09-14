using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Settings;
using UnityEditor;
using UnityEngine;

namespace PixoVR.TrainingCore.Editor
{
    /// <summary>Inspector for <see cref="GuidComponent"/>: read-only guid + regenerate button.</summary>
    [CustomEditor(typeof(GuidComponent))]
    public class GuidComponentInspector : UnityEditor.Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            var component = (GuidComponent)target;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("GUID", component.IsGuidAssigned ? component.GetGuid().ToString() : "(unassigned)");
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Regenerate GUID"))
            {
                Undo.RecordObject(component, "Regenerate GUID");
                component.CreateGuid();
                EditorUtility.SetDirty(component);
            }
            DrawDefaultInspector();
        }
    }

    /// <summary>Inspector for <see cref="GraphFlowManager"/> listing the parsed steps.</summary>
    [CustomEditor(typeof(GraphFlowManager))]
    public class GraphFlowManagerInspector : UnityEditor.Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (GraphFlowManager)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Parsed steps", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Steps are listed in play mode after the graph is parsed.", MessageType.Info);
                return;
            }
            var steps = manager.CurrentSteps;
            if (steps == null || steps.Count == 0)
            {
                EditorGUILayout.LabelField("(no active steps)");
                return;
            }
            foreach (var step in steps)
                EditorGUILayout.LabelField($"{step?.StepNumber}  {step?.Name}");
        }
    }

    /// <summary>Project Settings → PixoVR → Training Core provider for <see cref="TrainingConfig"/>.</summary>
    public static class TrainingCoreSettingsProvider
    {
        /// <summary>Project settings entry.</summary>
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/PixoVR/Training Core", SettingsScope.Project)
            {
                label = "Training Core",
                guiHandler = _ =>
                {
                    var config = Resources.Load<TrainingConfig>("TrainingConfig");
                    if (config == null)
                    {
                        EditorGUILayout.HelpBox("No TrainingConfig in Resources. Create one via Assets/Create/PixoVR/Training Config.", MessageType.Warning);
                        if (GUILayout.Button("Create Assets/Resources/TrainingConfig.asset"))
                            CreateConfigAsset();
                        return;
                    }
                    var so = new SerializedObject(config);
                    so.Update();
                    EditorGUILayout.PropertyField(so.FindProperty("MinTapDuration"));
                    EditorGUILayout.PropertyField(so.FindProperty("LogMask"));
                    EditorGUILayout.PropertyField(so.FindProperty("FadeSettings"), true);
                    EditorGUILayout.PropertyField(so.FindProperty("PlacerSettings"), true);
                    so.ApplyModifiedProperties();
                },
                keywords = new[] { "pixovr", "training", "fade", "tap" }
            };
        }

        /// <summary>Create the TrainingConfig asset.</summary>
        [MenuItem("Assets/Create/PixoVR/Training Config")]
        public static void CreateConfigAsset()
        {
            var config = ScriptableObject.CreateInstance<TrainingConfig>();
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(config, "Assets/Resources/TrainingConfig.asset");
            AssetDatabase.SaveAssets();
        }
    }
}
