using System;
using System.Collections.Generic;
using GraphProcessor;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.Interactions;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>"Blank Step": an empty step completed externally.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Blank Step", null)]
    public class BlankStepNode : SingleFlowStepNode
    {
        /// <inheritdoc/>
        public override string name => "Blank Step";

        /// <inheritdoc/>
        public override StepBase Create() => new BlankStep(this);
    }

    /// <summary>"Fade Step": fades the screen, completing when the fade finishes.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Fade Step", null)]
    public class FadeStepNode : SingleFlowStepNode
    {
        /// <summary>Fade configuration.</summary>
        [HideInInspector]
        public FadeSettings Settings;

        /// <inheritdoc/>
        public override string name => "Fade Step";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            Settings ??= new FadeSettings();
        }

        /// <inheritdoc/>
        public override StepBase Create() => new FadeStep(this);
    }

    /// <summary>"Open/Close Hand Menu": completes when the menu reaches the wanted state.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Hand Menu", null)]
    public class HandMenuStepNode : SingleFlowStepNode
    {
        /// <summary>Required open state.</summary>
        [HideInInspector]
        public bool ShouldOpen = true;

        /// <inheritdoc/>
        public override string name => "Open/Close Hand Menu";

        /// <inheritdoc/>
        public override StepBase Create() => new HandMenuStep(this);
    }

    /// <summary>"Input Action": completes when the bound input action is performed.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Input Action", null)]
    public class InputActionStepNode : SingleFlowStepNode
    {
        /// <summary>Input action that completes the step.</summary>
        [HideInInspector]
        public InputActionReference Input;

        /// <inheritdoc/>
        public override string name => "Input Action";

        /// <inheritdoc/>
        public override StepBase Create() => new InputActionStep(this);
    }

    /// <summary>"Move To Position": completes when the camera comes within <see cref="Size"/> of the location.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Move To Position", null)]
    public class MoveToPositionStepNode : SingleFlowStepNode
    {
        /// <summary>Target position (used when not using a transform).</summary>
        [HideInInspector]
        public Vector3 Location;

        /// <summary>Completion radius.</summary>
        [HideInInspector]
        public float Size = 1f;

        /// <summary>Read the location from a scene transform.</summary>
        [HideInInspector]
        public bool UseTransform = true;

        private readonly string locationTransformName = "LocationTransformName";

        /// <inheritdoc/>
        public override string name => "Move To Position";

        /// <summary>Scene transform providing the location.</summary>
        [HideInInspector]
        public Transform LocationTransform
        {
            get => GetSavedComponent<Transform>(locationTransformName);
            set => SetSavedComponent(locationTransformName, value);
        }

        /// <inheritdoc/>
        public override StepBase Create() => new MoveToPositionStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(locationTransformName);
    }

    /// <summary>"Info Point Step": completes correct/incorrect off an <see cref="InfoPointBase"/>.</summary>
    [Serializable]
    [NodeMenuItem("Current/Steps/Info Point Step", null)]
    public class InfoPointStepNode : CorrectIncorrectStepBaseNode
    {
        private readonly string infoPointSavedReferenceName = "InfoPointSavedRef";

        /// <summary>Bound info point.</summary>
        [HideInInspector]
        public InfoPointBase InfoPoint
        {
            get => GetSavedComponent<InfoPointBase>(infoPointSavedReferenceName);
            set => SetSavedComponent(infoPointSavedReferenceName, value);
        }

        /// <inheritdoc/>
        public override string name => "Info Point Step";

        /// <inheritdoc/>
        public override StepBase Create() => new InfoPointStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(infoPointSavedReferenceName);
    }

    /// <summary>"Info Point Step Group": runs a group of info points and resolves one outcome.</summary>
    [Serializable]
    [NodeMenuItem("Current/Group Steps/Info Point Group", null)]
    public class GroupedInfoPointsStepNode : CorrectIncorrectStepBaseNode, IGroupNode
    {
        private readonly string buttonSavedRefName = "CompleteButtonSavedRef";

        /// <summary>Group membership bookkeeping.</summary>
        [HideInInspector]
        public GroupNodeFunctionality GroupNodeFunctionality;

        /// <summary>Pick a random subset of the grouped points.</summary>
        [HideInInspector]
        public bool SelectRandomInfoPoints = false;

        /// <summary>How many points to enable when selecting randomly.</summary>
        [HideInInspector]
        public int NumberOfInfoPointsToEnable;

        /// <summary>Wait for a confirm button before resolving the outcome.</summary>
        [HideInInspector]
        public bool RequireButtonPressOnComplete = false;

        /// <summary>Confirm button.</summary>
        [HideInInspector]
        public Button CompleteButton
        {
            get => GetSavedComponent<Button>(buttonSavedRefName);
            set => SetSavedComponent(buttonSavedRefName, value);
        }

        /// <inheritdoc/>
        public override string name => "Info Point Step Group";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            GroupNodeFunctionality ??= new GroupNodeFunctionality();
        }

        /// <inheritdoc/>
        public override StepBase Create() => new GroupedInfoPointsStep(this);

        /// <inheritdoc/>
        protected override void AddReferences() => Data?.Add(buttonSavedRefName);
    }

    /// <summary>"Show Display Group": shows a display once all grouped steps complete.</summary>
    [Serializable]
    [NodeMenuItem("Current/Group Steps/Show Display Group", null)]
    public class ShowDisplayGroupStepNode : ShowDisplayNode, IGroupNode
    {
        /// <summary>Group membership bookkeeping.</summary>
        [HideInInspector]
        public GroupNodeFunctionality GroupNodeFunctionality;

        /// <summary>Group completion settings.</summary>
        [HideInInspector]
        public AndGroupSettings AndSettings;

        /// <inheritdoc/>
        public override string name => "Show Display Group";

        /// <inheritdoc/>
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            GroupNodeFunctionality ??= new GroupNodeFunctionality();
            AndSettings ??= new AndGroupSettings();
        }

        /// <inheritdoc/>
        public override StepBase Create() => new ShowDisplayGroupStep(this);
    }
}
