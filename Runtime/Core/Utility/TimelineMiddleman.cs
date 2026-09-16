using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixoVR.TrainingCore.Utility
{
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
}
