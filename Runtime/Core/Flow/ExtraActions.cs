using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Commands;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Graph;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixoVR.TrainingCore.Flow
{
    /// <summary>"Fade" action: fades the screen via a recorded <see cref="FadeCommand"/>.</summary>
    [Serializable]
    public class FadeAction : ActionBase
    {
        [SerializeField]
        private FadeSettings settings;

        [SerializeField]
        private FadeCommand actionCommand;

        /// <summary>Create from node.</summary>
        public FadeAction(FadeActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            settings = node.FadeSettings;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (settings == null)
                return;
            actionCommand = new FadeCommand(settings);
            CommandHistory.Instance.ExecuteAndRecord(actionCommand);
        }

        /// <inheritdoc/>
        public override void OnStepForward()
        {
            if (settings == null)
                return;
            var instant = new FadeCommand(new FadeSettings(settings) { FadeDuration = 0f });
            CommandHistory.Instance.ExecuteAndRecord(instant);
        }

        /// <inheritdoc/>
        public override void Undo() => actionCommand?.Unexecute();
    }

    /// <summary>"Log" action: writes a message to the log.</summary>
    [Serializable]
    public class LogAction : ActionBase
    {
        [SerializeField]
        private string message;

        [SerializeField]
        private LogType type;

        /// <summary>Create from node.</summary>
        public LogAction(LogActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            message = node.Message;
            type = node.LogType;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    Log.Error(message ?? string.Empty, LogCategory.Flow);
                    break;
                case LogType.Warning:
                    Log.Warning(message ?? string.Empty, LogCategory.Flow);
                    break;
                default:
                    Log.Info(message ?? string.Empty, LogCategory.Flow);
                    break;
            }
        }
    }

    /// <summary>"Fail" action: triggers the fail handler on the current steps.</summary>
    [Serializable]
    public class FailAction : ActionBase
    {
        [SerializeField]
        private string reasonForFailure;

        /// <summary>Create from node.</summary>
        public FailAction(FailActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            reasonForFailure = node.FailReason;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (!GraphFlowManager.InstanceExists)
            {
                Log.Warning($"FailAction without a GraphFlowManager: {reasonForFailure}", LogCategory.Flow);
                return;
            }
            GraphFlowManager.Instance.OnFail(GraphFlowManager.Instance.CurrentSteps, reasonForFailure);
        }
    }

    /// <summary>"Set Color" action via <see cref="SetColorCommand"/>.</summary>
    [Serializable]
    public class SetColorAction : ActionBase
    {
        [SerializeField]
        private Identity.GuidReference targetObjectReference;

        [SerializeField]
        private Color setToColor;

        [SerializeField]
        private SetColorCommand actionCommand;

        private GameObject targetObject => targetObjectReference?.GameObject;

        /// <summary>Create from node.</summary>
        public SetColorAction(SetColorActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            var target = node.TargetObject;
            targetObjectReference = target != null ? new Identity.GuidReference(target) : null;
            setToColor = node.TargetColor;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (targetObject == null)
                return;
            actionCommand = new SetColorCommand(targetObject, setToColor);
            CommandHistory.Instance.ExecuteAndRecord(actionCommand);
        }

        /// <inheritdoc/>
        public override void OnStepForward() => Act();

        /// <inheritdoc/>
        public override void Undo() => actionCommand?.Unexecute();
    }

    /// <summary>"Set Object Material" action via <see cref="SetObjectMaterialCommand"/>.</summary>
    [Serializable]
    public class SetGameObjectMaterialAction : ActionBase
    {
        [SerializeField]
        private Identity.GuidReference _targetObjectReference;

        [SerializeField]
        private Material targetMaterial;

        [SerializeField]
        private SetObjectMaterialCommand lastSetMaterialCommand;

        private GameObject targetObject => _targetObjectReference?.GameObject;

        /// <summary>Create from node.</summary>
        public SetGameObjectMaterialAction(SetGameObjectMaterialActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            var target = node.TargetObject;
            _targetObjectReference = target != null ? new Identity.GuidReference(target) : null;
            targetMaterial = node.TargetMaterial;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (targetObject == null)
                return;
            lastSetMaterialCommand = new SetObjectMaterialCommand(targetObject, targetMaterial);
            CommandHistory.Instance.ExecuteAndRecord(lastSetMaterialCommand);
        }

        /// <inheritdoc/>
        public override void OnStepForward() => Act();

        /// <inheritdoc/>
        public override void Undo() => lastSetMaterialCommand?.Unexecute();
    }

    /// <summary>"Set Object Position" action via <see cref="SetObjectPositionCommand"/>.</summary>
    [Serializable]
    public class SetGameObjectPositionAction : ActionBase
    {
        [SerializeField]
        private Identity.GuidReference _targetObjectReference;

        [SerializeField]
        private Vector3 newObjectPosition;

        [SerializeField]
        private Vector3 initialPosition;

        [SerializeField]
        private SetObjectPositionCommand lastCommand;

        private GameObject targetObject => _targetObjectReference?.GameObject;

        /// <summary>Create from node.</summary>
        public SetGameObjectPositionAction(SetGameObjectPositionActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            var target = node.TargetObject;
            _targetObjectReference = target != null ? new Identity.GuidReference(target) : null;
            newObjectPosition = node.UseTransform && node.ObjectTransform != null
                ? node.ObjectTransform.transform.position
                : node.ObjectPostion;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (targetObject == null)
                return;
            initialPosition = targetObject.transform.position;
            lastCommand = new SetObjectPositionCommand(targetObject, newObjectPosition);
            CommandHistory.Instance.ExecuteAndRecord(lastCommand);
        }

        /// <inheritdoc/>
        public override void OnStepForward() => Act();

        /// <inheritdoc/>
        public override void Undo() => lastCommand?.Unexecute();
    }

    /// <summary>"Set Highlight Zones" action: toggles highlight on each listed zone.</summary>
    [Serializable]
    public class SetHighlightZonesAction : ActionBase
    {
        [SerializeField]
        private List<HighlightZoneData> zonesData = new List<HighlightZoneData>();

        private readonly List<HighlightObjectCommand> commands = new List<HighlightObjectCommand>();

        /// <summary>Create from node.</summary>
        public SetHighlightZonesAction(SetHighlightZonesActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            zonesData = node.HighlightData ?? new List<HighlightZoneData>();
        }

        /// <inheritdoc/>
        public override void Act()
        {
            commands.Clear();
            foreach (var zone in zonesData)
            {
                var highlight = zone?.Target?.GetComponent<HighlightBase>();
                if (highlight == null)
                    continue;
                var command = new HighlightObjectCommand(highlight, zone.State);
                CommandHistory.Instance.ExecuteAndRecord(command);
                commands.Add(command);
            }
        }

        /// <inheritdoc/>
        public override void OnStepForward() => Act();

        /// <inheritdoc/>
        public override void Undo()
        {
            foreach (var command in commands)
                command?.Unexecute();
            commands.Clear();
        }
    }

    /// <summary>"Play Timeline" action driving a <see cref="PlayableDirector"/>.</summary>
    [Serializable]
    public class PlayTimelineAction : ActionBase
    {
        [SerializeField]
        private PlayableDirector director;

        [SerializeField]
        private TimelineAsset timelineAsset;

        [SerializeField]
        private bool loop;

        [SerializeField]
        private SkipOnCompleteSettings skipOnCompleteSettings;

        /// <summary>Create from node.</summary>
        public PlayTimelineAction(PlayTimelineActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            director = node.PlayableDirector;
            timelineAsset = node.TimelineToPlay;
            loop = node.Loop;
            skipOnCompleteSettings = node.SkipOnCompleteSettings;
        }

        /// <inheritdoc/>
        public override void Act() => TimelinePlayer.Play(director, timelineAsset, loop);

        /// <inheritdoc/>
        public override void OnStepForward() => TimelinePlayer.SetToLastFrame(director, timelineAsset);

        /// <inheritdoc/>
        public override void OnStepBackward() => TimelinePlayer.SetToFirstFrame(director, timelineAsset);

        /// <inheritdoc/>
        public override void OnStepCompleted()
        {
            if (skipOnCompleteSettings != null && skipOnCompleteSettings.SkipOnComplete)
                TimelinePlayer.SetToLastFrame(director, timelineAsset);
        }

        /// <inheritdoc/>
        public override void OnStepFailed() => director?.Stop();
    }

    /// <summary>"Hand Coach" action: spawns a hand coach via <see cref="SpawnHandCoachCommand"/>.</summary>
    [Serializable]
    public class HandCoachAction : ActionBase
    {
        [SerializeField]
        private HandCoachBase handCoachPrefab;

        [SerializeField]
        private Transform spawnLocation;

        [SerializeField]
        private float startDelay;

        [SerializeField]
        private string animationName;

        private SpawnHandCoachCommand spawnCommand;

        /// <summary>Create from node.</summary>
        public HandCoachAction(HandCoachActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            handCoachPrefab = node.HandCoachMiddlemanPrefab;
            spawnLocation = node.SpawnLocation;
            startDelay = node.StartDelay;
            animationName = node.AnimationToPlay;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (handCoachPrefab == null)
                return;
            spawnCommand = new SpawnHandCoachCommand(handCoachPrefab, spawnLocation, animationName, startDelay);
            CommandHistory.Instance.ExecuteAndRecord(spawnCommand);
        }

        /// <inheritdoc/>
        public override void Undo() => spawnCommand?.Unexecute();

        /// <inheritdoc/>
        public override void OnStepFailed() => spawnCommand?.Unexecute();
    }

    /// <summary>"Instructional Arrow Action": places an arrow via <see cref="PlaceArrowCommand"/>.</summary>
    [Serializable]
    public class InstructionalArrowAction : ActionBase
    {
        [SerializeField]
        private Transform relativeToTransform;

        [SerializeField]
        private ArrowPlacerSettings settings;

        [SerializeField]
        private PlaceArrowCommand lastPlaceCommand;

        [SerializeField]
        private Color completeColor;

        private Utility.Display.ArrowPlacer placer;

        /// <summary>Create from node.</summary>
        public InstructionalArrowAction(InstructionArrowActionNode node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            relativeToTransform = node.TargetTransform;
            settings = node.PlacerSettings;
            completeColor = node.CompleteColor;
            placer = node.Placer != null ? node.Placer : UnityEngine.Object.FindFirstObjectByType<Utility.Display.ArrowPlacer>();
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (placer == null || relativeToTransform == null)
                return;
            lastPlaceCommand = new PlaceArrowCommand(placer, relativeToTransform, settings);
            CommandHistory.Instance.ExecuteAndRecord(lastPlaceCommand);
        }

        /// <inheritdoc/>
        public override void OnStepCompleted()
        {
            var arrow = lastPlaceCommand?.PlacedArrow;
            if (arrow != null)
                CommandHistory.Instance.ExecuteAndRecord(new SetColorCommand(arrow, completeColor));
        }

        /// <inheritdoc/>
        public override void OnStepFailed() => lastPlaceCommand?.Unexecute();

        /// <inheritdoc/>
        public override void Undo() => lastPlaceCommand?.Unexecute();

        /// <inheritdoc/>
        public override void OnStepForward() { }
    }

    /// <summary>"Parameter Setter" action: writes a value into an exposed parameter.</summary>
    [Serializable]
    public class SetExposedParameterAction<T> : ActionBase
    {
        [SerializeField]
        private string exposedParameterName;

        [SerializeField]
        private T setValue;

        /// <summary>Create from node.</summary>
        public SetExposedParameterAction(SetExposedParameterActionNodeBase<T> node)
        {
            GUID = node.GUID;
            UndoOnStepEntryPoints = node.UndoEntries;
            exposedParameterName = node.ExposedParameter?.name;
            setValue = node.Value;
        }

        /// <inheritdoc/>
        public override void Act()
        {
            if (!string.IsNullOrEmpty(exposedParameterName))
                ExposedParameterManager.SetExposedParameter(exposedParameterName, setValue);
        }
    }
}
