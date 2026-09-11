using System.IO;
using GraphProcessor;
using PixoVR.TrainingCore.Graph;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixoVR.TrainingCore.Editor
{
    /// <summary>Graph editor window for <see cref="TrainingGraph"/> assets.</summary>
    public class TrainingGraphWindow : BaseGraphWindow
    {
        /// <summary>Open the graph under the cursor in the project browser.</summary>
        [OnOpenAsset(0)]
        public static bool OnBaseGraphOpened(int instanceId, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceId) as TrainingGraph;
            if (asset == null)
                return false;
            var window = GetWindow<TrainingGraphWindow>();
            window.InitializeGraph(asset);
            return true;
        }

        /// <summary>Create a TrainingGraph asset.</summary>
        [MenuItem("Assets/Create/PixoVR/Training Graph")]
        public static void CreateGraphAsset()
        {
            var graph = CreateInstance<TrainingGraph>();
            ProjectWindowUtil.CreateAsset(graph, "TrainingGraph.asset");
        }

        /// <inheritdoc/>
        protected override void InitializeWindow(BaseGraph graph)
        {
            titleContent = new GUIContent("Training Graph");
            var view = new BaseGraphView(this);
            view.Add(new MiniMapView(view));
            view.Add(new ToolbarView(view));
            rootView.Add(view);
        }
    }
}
