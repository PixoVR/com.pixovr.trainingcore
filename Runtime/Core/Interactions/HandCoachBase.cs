using System.Collections;
using UnityEngine;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>Base hand-coach: plays a named animation after an optional delay.</summary>
    public abstract class HandCoachBase : MonoBehaviour
    {
        [SerializeField]
        private Animator animator;

        /// <summary>Play <paramref name="animationName"/> after <paramref name="delay"/> seconds.</summary>
        public virtual void Play(string animationName, float delay = 0f)
        {
            if (delay > 0f)
                StartCoroutine(PlayDelayed(animationName, delay));
            else
                animator?.Play(animationName);
        }

        /// <summary>Stop playback.</summary>
        public virtual void Stop()
        {
            StopAllCoroutines();
            if (animator != null)
                animator.StopPlayback();
        }

        private IEnumerator PlayDelayed(string animationName, float delay)
        {
            yield return new WaitForSeconds(delay);
            animator?.Play(animationName);
        }
    }
}
