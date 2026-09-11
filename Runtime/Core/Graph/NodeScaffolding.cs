using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>A scene-object binding captured per node property (serialized inside NodeData).</summary>
    [Serializable]
    public class NodeSavedProperty
    {
        /// <summary>Node property this binds to.</summary>
        public string PropertyName;

        /// <summary>Single bound object.</summary>
        public GuidReference ObjectReference;

        /// <summary>Bound object list.</summary>
        public List<GuidReference> ObjectReferences = new List<GuidReference>();

        /// <summary>Empty binding.</summary>
        public NodeSavedProperty() { }

        /// <summary>Binding for a property name.</summary>
        public NodeSavedProperty(string propertyName) => PropertyName = propertyName;

        /// <summary>Copy ctor.</summary>
        public NodeSavedProperty(NodeSavedProperty property)
        {
            PropertyName = property.PropertyName;
            ObjectReference = property.ObjectReference;
            ObjectReferences = new List<GuidReference>(property.ObjectReferences ?? new List<GuidReference>());
        }
    }

    /// <summary>Per-node serialized blob: scene references + per-mode include flags.</summary>
    [Serializable]
    public class NodeData
    {
        /// <summary>Saved scene references.</summary>
        [SerializeField]
        private List<NodeSavedProperty> references = new List<NodeSavedProperty>();

        /// <summary>Per-mode include flags.</summary>
        [SerializeField]
        private List<GameModeData> gameModesData = new List<GameModeData>();

        /// <summary>Empty node data.</summary>
        public NodeData() { }

        /// <summary>Copy ctor.</summary>
        public NodeData(NodeData data)
        {
            if (data == null)
                return;
            references = data.references?.Select(r => new NodeSavedProperty(r)).ToList() ?? new List<NodeSavedProperty>();
            gameModesData = data.gameModesData?.Select(g => new GameModeData(g.Mode, g.Include)).ToList() ?? new List<GameModeData>();
        }

        /// <summary>True when the node is included in the given mode (default: included).</summary>
        public bool IsIncludedInMode(GameMode mode)
        {
            var d = GetGameModeDataFor(mode);
            return d == null || d.Include;
        }

        private GameModeData GetGameModeDataFor(GameMode mode) =>
            gameModesData?.FirstOrDefault(g => g.Mode == mode);

        /// <summary>Set include flag for a mode.</summary>
        public void SetIncludeValueFor(GameMode mode, bool includeValue)
        {
            var d = GetGameModeDataFor(mode);
            if (d != null)
                d.Include = includeValue;
            else
                (gameModesData ??= new List<GameModeData>()).Add(new GameModeData(mode, includeValue));
        }

        /// <summary>Start tracking a property reference.</summary>
        public void Add(string propertyName) => Add(new NodeSavedProperty(propertyName));

        /// <summary>Add a saved property.</summary>
        public void Add(NodeSavedProperty property)
        {
            if (Find(property.PropertyName) == null)
                references.Add(property);
        }

        /// <summary>Find a saved property by name.</summary>
        public NodeSavedProperty Find(string name) => references?.FirstOrDefault(r => r.PropertyName == name);

        /// <summary>No scene references stored.</summary>
        public bool HasNoReferences() => references == null || references.Count == 0;

        /// <summary>All saved properties.</summary>
        public IEnumerable<NodeSavedProperty> SavedProperties => references ?? Enumerable.Empty<NodeSavedProperty>();
    }

    /// <summary>One node's serialized reference blob, keyed by node GUID.</summary>
    [Serializable]
    public class Reference
    {
        /// <summary>Owning node GUID.</summary>
        [HideInInspector]
        public string NodeID;

        /// <summary>Node data.</summary>
        public NodeData Data;

        /// <summary>Pair a node id with its data.</summary>
        public Reference(string nodeID, NodeData data)
        {
            NodeID = nodeID;
            Data = data;
        }
    }

    /// <summary>All scene-reference blobs for a graph (the legacy <c>savedRefs</c> field).</summary>
    [Serializable]
    public class SceneReferences : IEnumerable<Reference>
    {
        /// <summary>Stored references.</summary>
        [SerializeField]
        private List<Reference> objectReferences = new List<Reference>();

        /// <summary>Add a reference (replaces existing for the same node).</summary>
        public void Add(Reference objectRef)
        {
            if (objectRef == null)
                return;
            var existing = objectReferences.FirstOrDefault(r => r.NodeID == objectRef.NodeID);
            if (existing != null)
                objectReferences.Remove(existing);
            objectReferences.Add(objectRef);
        }

        /// <summary>Remove a reference.</summary>
        public void Remove(Reference objectRef) => objectReferences.Remove(objectRef);

        /// <summary>Find the reference for a node GUID.</summary>
        public Reference Find(string nodeID) => objectReferences.FirstOrDefault(r => r.NodeID == nodeID);

        /// <inheritdoc/>
        public IEnumerator<Reference> GetEnumerator() => objectReferences.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => objectReferences.GetEnumerator();
    }

    /// <summary>A dynamic named output port on a conditional node.</summary>
    [Serializable]
    public class DynamicPort
    {
        /// <summary>Backing field name (e.g. "Executes1").</summary>
        public string FieldName;

        /// <summary>True while the port is unbound.</summary>
        [SerializeField]
        private bool IsFree = true;

        /// <summary>Branch weight for random selection.</summary>
        public int Weight = 1;

        /// <summary>Create a port bound to a field name.</summary>
        public DynamicPort(string fieldName) => FieldName = fieldName;

        /// <summary>Mark the port free.</summary>
        public void Reset() => IsFree = true;

        /// <summary>Whether the port has been bound.</summary>
        public bool IsUsed() => !IsFree;

        /// <summary>Mark the port bound.</summary>
        public void Bind() => IsFree = false;
    }

    /// <summary>Maps an enum value to a set of output steps.</summary>
    [Serializable]
    public class EnumToStepMapping
    {
        /// <summary>Enum index.</summary>
        public int Enum;

        /// <summary>Steps for that index.</summary>
        public List<StepBase> Steps;
    }

    /// <summary>A weighted list of output steps (random/parallel branches).</summary>
    [Serializable]
    public class WeightedOutputSteps
    {
        /// <summary>Branch weight.</summary>
        public int Weight;

        /// <summary>Output steps.</summary>
        public List<StepBase> Steps;

        /// <summary>Create a weighted output.</summary>
        public WeightedOutputSteps(int weight, List<StepBase> outputs)
        {
            Weight = weight;
            Steps = outputs;
        }
    }

    /// <summary>Group bookkeeping for group nodes (parallel sub-steps).</summary>
    [Serializable]
    public class GroupNodeFunctionality
    {
        /// <summary>Step node GUIDs inside this group.</summary>
        public List<string> GroupedStepGuids = new List<string>();
    }
}
