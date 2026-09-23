using System;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.SceneManagement;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Shows a display object.</summary>
    [Serializable]
    public class DisplayObjectAction : ActionBase
    {
        [SerializeField]
        private Settings.PlacerSettings settings;

        [SerializeField]
        private Data.DisplayData displayData;

        private GameObject displayObject;

        /// <summary>Create from node.</summary>
        public DisplayObjectAction(DisplayObjectActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            displayData = node.StepDisplayData;
            displayObject = node.DisplayObject;
            settings = node.UseDefaultSettings ? null : node.DisplaySettings;
        }

        private Commands.DisplayObjectCommand command;

        /// <inheritdoc/>
        public override void Act()
        {
            if (displayObject == null)
            {
                Log.Warning("DisplayObjectAction: no display object", LogCategory.Flow);
                return;
            }
            command = new DisplayObjectCommand(GUID, displayObject, settings, displayData);
            command.Execute();
            CommandHistory.Instance.Record(command);
            Log.Info($"Display spawned: {displayObject.name}", LogCategory.Flow);
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            command?.Unexecute();
            command = null;
        }
    }

    /// <summary>Toggles a highlight on a scene object.</summary>
    [Serializable]
    public class HighlightAction : ActionBase
    {
        [SerializeField]
        private bool state;

        private Utility.HighlightBase highlightObject;

        /// <summary>Create from node.</summary>
        public HighlightAction(HighlightActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            state = node.HighlightState;
            highlightObject = node.HighlightObject;
        }

        /// <inheritdoc/>
        public override void Act() => highlightObject?.ToggleHighlight(state);

        /// <inheritdoc/>
        public override void Undo() => highlightObject?.ToggleHighlight(!state);
    }

    /// <summary>Updates hand-menu text.</summary>
    [Serializable]
    public class SetHandMenuTextAction : ActionBase
    {
        [SerializeField]
        private Data.DisplayData displayData;

        /// <summary>Create from node.</summary>
        public SetHandMenuTextAction(SetHandMenuTextActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            displayData = node.DisplayData;
        }

        private Commands.SetHandMenuTextCommand command;

        /// <inheritdoc/>
        public override void Act()
        {
            if (HandMenu.HandMenuBase.Instance == null)
                Log.Warning("SetHandMenuTextAction: no HandMenuBase.Instance", LogCategory.Flow);
            command = new SetHandMenuTextCommand(displayData);
            command.Execute();
            CommandHistory.Instance.Record(command);
            Log.Info($"Hand menu text set: {displayData?.Title}", LogCategory.Flow);
        }

        /// <inheritdoc/>
        public override void Undo() => command?.Unexecute();
    }

    /// <summary>Sets GameObject active states via undoable commands.</summary>
    [Serializable]
    public class SetGameObjectActiveStateAction : ActionBase
    {
        [SerializeField]
        private bool state;

        private GameObject targetObject;
        private System.Collections.Generic.List<GameObject> targetObjects;

        /// <summary>Create from node.</summary>
        public SetGameObjectActiveStateAction(SetGameObjectActiveStateNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            state = node.State;
            targetObject = node.TargetObject;
            targetObjects = node.TargetObjects;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (targetObject != null)
                Record(new SetObjectActiveStateCommand(targetObject, state));
            if (targetObjects != null)
                foreach (var go in targetObjects)
                    if (go != null)
                        Record(new SetObjectActiveStateCommand(go, state));
        }

        private static void Record(SetObjectActiveStateCommand command)
        {
            command.Execute();
            CommandHistory.Instance.Record(command);
        }
    }

    /// <summary>Enables/disables a behaviour on a target object.</summary>
    [Serializable]
    public class SetComponentStateAction : ActionBase
    {
        [SerializeField]
        private bool state;

        private Behaviour target;

        /// <summary>Create from node.</summary>
        public SetComponentStateAction(SetComponentStateActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            state = node.State;
            target = node.TargetBehavior;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (target != null)
                target.enabled = state;
        }

        /// <inheritdoc/>
        public override void Undo()
        {
            if (target != null)
                target.enabled = !state;
        }
    }

    /// <summary>Fires the bound <see cref="GenericActionTrigger"/> events.</summary>
    [Serializable]
    public class GenericAction : ActionBase
    {
        private GenericActionTrigger functionality;

        /// <summary>Create from node.</summary>
        public GenericAction(GenericActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            functionality = node.Functionality;
        }

        /// <inheritdoc/>
        public override void Act() => functionality?.Act();

        /// <inheritdoc/>
        public override void Undo() => functionality?.OnUndo();
    }

    /// <summary>Plays audio clips via <see cref="AudioManager"/>.</summary>
    [Serializable]
    public class AudioAction : ActionBase
    {
        [SerializeField]
        private Settings.AudioClipSettings audioSettings;

        private AudioSource source;

        /// <summary>Create from node.</summary>
        public AudioAction(AudioActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            audioSettings = node.AudioSettings;
            source = node.AudioSource;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (AudioManager.InstanceExists)
                AudioManager.Instance.Play(audioSettings, source);
        }
    }

    /// <summary>Loads a scene, optionally fading out first.</summary>
    [Serializable]
    public class LoadSceneAction : ActionBase
    {
        [SerializeField]
        private string sceneName;

        [SerializeField]
        private bool fade;

        [SerializeField]
        private float duration;

        [SerializeField]
        private bool additive;

        /// <summary>Create from node.</summary>
        public LoadSceneAction(LoadSceneActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            sceneName = node.SceneName;
            fade = node.FadeOut;
            duration = node.Duration;
            additive = node.Additive;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (fade && FadeManager.InstanceExists)
                FadeManager.Instance.FadeToBlack();
            if (!string.IsNullOrEmpty(sceneName))
                SceneLoading.Load(sceneName);
        }
    }

    /// <summary>Adds exceptions to the global exception set.</summary>
    [Serializable]
    public class AddGlobalExceptionAction : ActionBase
    {
        [SerializeField]
        private Settings.FailData failData;

        /// <summary>Create from node.</summary>
        public AddGlobalExceptionAction(AddGlobalExceptionActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            failData = node.FailData;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            // merged into the active fail-exception set by the flow manager; wave-3 UI hooks read FailData.
        }
    }

    /// <summary>Removes exceptions from the global exception set.</summary>
    [Serializable]
    public class RemoveGlobalExceptionAction : ActionBase
    {
        [SerializeField]
        private Settings.FailData failData;

        /// <summary>Create from node.</summary>
        public RemoveGlobalExceptionAction(RemoveGlobalExceptionActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            failData = node.FailData;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            // counterpart of AddGlobalExceptionAction
        }
    }
}
