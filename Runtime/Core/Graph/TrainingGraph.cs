using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>
    /// A TrainingCore module graph (NodeGraphProcessor <see cref="BaseGraph"/>).
    /// Serialized layout matches migrated Luminous <c>DefaultGraph</c> assets.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingGraph", menuName = "TrainingCore/Training Graph")]
    public class TrainingGraph : BaseGraph
    {
        /// <summary>Per-node scene-reference blobs.</summary>
        [HideInInspector]
        [SerializeField]
        private SceneReferences savedRefs = new SceneReferences();

        /// <summary>Path of the linked module scene.</summary>
        [HideInInspector]
        public string LinkedScenePath;

        /// <summary>GUID of the linked module scene.</summary>
        [HideInInspector]
        public string LinkedSceneGUID;

        /// <summary>Editor debug flag.</summary>
        public bool DebugToggleValue = false;

        /// <summary>Whether the graph uses portal comms.</summary>
        [HideInInspector]
        public bool PortalCommunication = false;

        /// <summary>Portal scene name.</summary>
        public string PortalSceneName = "";

        /// <summary>Node types the editor allows (empty = all).</summary>
        [HideInInspector]
        public SerializableSystemType[] ValidNodeTypes = new SerializableSystemType[0];

        /// <summary>Saved scene references.</summary>
        public SceneReferences SavedRefs => savedRefs ??= new SceneReferences();

        /// <summary>The graph's start node.</summary>
        public StartNode StartNode => nodes.OfType<StartNode>().FirstOrDefault();

        /// <summary>All fail-handler nodes.</summary>
        public List<FailHandlerNode> FailHandlers => nodes.OfType<FailHandlerNode>().ToList();

        /// <summary>The default fail handler.</summary>
        public FailHandlerNode DefaultFailHandler => FailHandlers.FirstOrDefault(h => h.IsDefault);

        /// <summary>The global-exceptions node.</summary>
        public GlobalExceptionNode GlobalExceptions => nodes.OfType<GlobalExceptionNode>().FirstOrDefault();

        /// <summary>Add a node's reference blob.</summary>
        public void AddNodeReferences(Reference reference) => SavedRefs.Add(reference);

        /// <summary>Remove a node's reference blob.</summary>
        public void RemoveNodeReferences(Reference reference) => savedRefs?.Remove(reference);
    }

    /// <summary>A serializable <see cref="Type"/> wrapper used by <see cref="TrainingGraph.ValidNodeTypes"/>.</summary>
    [Serializable]
    public struct SerializableSystemType : ISerializationCallbackReceiver
    {
        /// <summary>Assembly-qualified name.</summary>
        [SerializeField]
        private string name;

        private Type _type;

        /// <summary>The resolved type.</summary>
        public Type SystemType
        {
            get => _type;
            set
            {
                _type = value;
                name = value?.AssemblyQualifiedName;
            }
        }

        /// <summary>Qualified name.</summary>
        public string Name => name;

        /// <summary>Create from a type.</summary>
        public SerializableSystemType(Type type) : this() => SystemType = type;

        /// <summary>The resolved type (alias).</summary>
        public Type ManagedType => SystemType;

        /// <summary>Re-resolve after deserialization.</summary>
        public void TryRebindManagedType()
        {
            if (_type == null && !string.IsNullOrEmpty(name))
                _type = Type.GetType(name);
        }

        /// <inheritdoc/>
        public void OnBeforeSerialize() => name = _type?.AssemblyQualifiedName ?? name;

        /// <inheritdoc/>
        public void OnAfterDeserialize() => _type = string.IsNullOrEmpty(name) ? null : Type.GetType(name);
    }
}
