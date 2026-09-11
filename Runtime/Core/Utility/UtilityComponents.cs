using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>Base for objects that can be highlighted/unhighlighted.</summary>
    public abstract class HighlightBase : MonoBehaviour
    {
        /// <summary>Current highlight state.</summary>
        public bool IsHighlighted { get; protected set; }

        /// <summary>Toggle the highlight.</summary>
        public virtual void ToggleHighlight(bool state)
        {
            if (state)
                Highlight();
            else
                Unhighlight();
        }

        /// <summary>Turn the highlight on.</summary>
        public virtual void Highlight() => IsHighlighted = true;

        /// <summary>Turn the highlight off.</summary>
        public virtual void Unhighlight() => IsHighlighted = false;
    }

    /// <summary>UnityEvent-style invokable scene actions (enable/disable objects).</summary>
    public class InvokableActions : MonoBehaviour
    {
        /// <summary>Activate a GameObject.</summary>
        public void EnableObjectAction(GameObject target)
        {
            if (target != null)
                target.SetActive(true);
        }

        /// <summary>Deactivate a GameObject.</summary>
        public void DisableObjectAction(GameObject target)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    /// <summary>Drives a <see cref="PlayableDirector"/> to the start/end of a timeline.</summary>
    public class TimelineMiddleman : MonoBehaviour
    {
        /// <summary>The bound director.</summary>
        [SerializeField]
        private PlayableDirector TargetDirector;

        /// <summary>Resume playback.</summary>
        public virtual void Play() => TargetDirector?.Play();

        /// <summary>Jump to the first frame.</summary>
        public virtual void SetToStart()
        {
            if (TargetDirector != null)
                TargetDirector.time = 0;
        }

        /// <summary>Jump to the last frame.</summary>
        public virtual void SetToEnd()
        {
            if (TargetDirector != null && TargetDirector.playableAsset != null)
                TargetDirector.time = TargetDirector.playableAsset.duration;
        }
    }

    /// <summary>Scrubs a <see cref="PlayableDirector"/> between start and end with lerp control.</summary>
    public class TimelinePlayer : MonoBehaviour
    {
        /// <summary>The bound director.</summary>
        [SerializeField]
        private PlayableDirector targetDirector;

        /// <summary>Normalized position (0-1).</summary>
        public float Progress { get; private set; }

        /// <summary>The bound director.</summary>
        public PlayableDirector Director => targetDirector;

        /// <summary>Jump to a normalized position.</summary>
        public void SetProgress(float t)
        {
            Progress = Mathf.Clamp01(t);
            if (targetDirector != null && targetDirector.playableAsset != null)
                targetDirector.time = Progress * targetDirector.playableAsset.duration;
        }

        /// <summary>Seek to the start.</summary>
        public void Reset() => SetProgress(0f);

        /// <summary>Seek to the end.</summary>
        public void Complete() => SetProgress(1f);
    }
}
