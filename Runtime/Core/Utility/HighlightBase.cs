using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

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



    /// <summary>Static timeline helpers: play, jump to first/last frame.</summary>
    public static class TimelinePlayer
    {
        /// <summary>Play a timeline; invoke <paramref name="onComplete"/> when it stops.</summary>
        public static void Play(PlayableDirector director, TimelineAsset timeline = null, bool loop = false, Action onComplete = null)
        {
            if (director == null)
                return;
            if (timeline != null)
                director.playableAsset = timeline;
            director.extrapolationMode = loop ? DirectorWrapMode.Loop : DirectorWrapMode.Hold;
            director.stopped += _ => onComplete?.Invoke();
            director.Play();
        }

        /// <summary>Seek to the last frame.</summary>
        public static void SetToLastFrame(PlayableDirector director, TimelineAsset asset = null)
        {
            var a = asset != null ? asset : director?.playableAsset as TimelineAsset;
            if (director != null && a != null)
                director.time = a.duration;
        }

        /// <summary>Seek to the first frame.</summary>
        public static void SetToFirstFrame(PlayableDirector director, TimelineAsset asset = null)
        {
            if (director != null)
                director.time = 0;
        }
    }
}
