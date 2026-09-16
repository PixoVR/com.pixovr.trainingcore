using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Settings;
using UnityEditor;
using UnityEngine;

namespace PixoVR.TrainingCore.Editor
{
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
}
