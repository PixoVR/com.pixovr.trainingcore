using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Marker struct for action attachment ports (OnStartActions/OnFinishActions).</summary>
    [Serializable]
    public struct ActionLink { }

    /// <summary>Marker struct for step execution flow ports (executed/executes).</summary>
    [Serializable]
    public struct ExecutionLink { }

    /// <summary>When an action's Undo runs relative to a step boundary.</summary>
    public enum UndoActionEntryOption
    {
        /// <summary>Undo when the step is entered.</summary>
        OnStart,
        /// <summary>Undo when the step completes.</summary>
        OnComplete
    }

    /// <summary>Maps an action node GUID to an undo trigger point.</summary>
    [Serializable]
    public class UndoOnStepNodeEntry
    {
        /// <summary>Target step node GUID.</summary>
        public string NodeGUID;
        /// <summary>Undo trigger.</summary>
        public UndoActionEntryOption Options;

        /// <summary>Empty entry.</summary>
        public UndoOnStepNodeEntry() { }

        /// <summary>Entry for a step guid, defaulting to OnComplete.</summary>
        public UndoOnStepNodeEntry(string guid)
        {
            NodeGUID = guid;
            Options = UndoActionEntryOption.OnComplete;
        }
    }

    /// <summary>
    /// Runtime twin of an action node: one-shot behaviour executed at step boundaries.
    /// </summary>
    [Serializable]
    public abstract class ActionBase
    {
        /// <summary>Step boundaries that trigger this action's Undo.</summary>
        public List<UndoOnStepNodeEntry> UndoOnStepEntryPoints = new List<UndoOnStepNodeEntry>();

        /// <summary>Node GUID this action was parsed from.</summary>
        public string GUID;

        /// <summary>Perform the action.</summary>
        public virtual void Act() { }

        /// <summary>Called while skipping forwards over the owning step.</summary>
        public virtual void OnStepForward() { }

        /// <summary>Called while skipping backwards over the owning step.</summary>
        public virtual void OnStepBackward() { }

        /// <summary>Revert the action.</summary>
        public virtual void Undo() { }

        /// <summary>Called when the owning step completes.</summary>
        public virtual void OnStepCompleted() { }

        /// <summary>Called when the owning step fails.</summary>
        public virtual void OnStepFailed() { }
    }

    /// <summary>Serializable reference to an <see cref="ActionBase"/> by GUID.</summary>
    [Serializable]
    public class ActionReference
    {
        [SerializeField]
        private string actionGuid;

        /// <summary>Resolved action (set during parse).</summary>
        [NonSerialized]
        public ActionBase Action;

        /// <summary>Target GUID.</summary>
        public string Guid => actionGuid;

        /// <summary>Create from an action.</summary>
        public ActionReference(ActionBase action)
        {
            Action = action;
            actionGuid = action?.GUID;
        }

        /// <summary>Create from a guid string.</summary>
        public ActionReference(string actionGuid) => this.actionGuid = actionGuid;
    }
}
