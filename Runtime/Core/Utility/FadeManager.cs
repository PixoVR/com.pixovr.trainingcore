using System.Collections;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>Full-screen fade-to-black overlay driven by <see cref="FadeSettings"/>.</summary>
    public class FadeManager : SingletonBehaviour<FadeManager>
    {
        /// <summary>The rig's fade canvas; when set it is driven instead of an overlay.</summary>
        public CanvasGroup DefaultFadeCanvas;

        private Image fadeImage;
        private Coroutine fadeCoroutine;
        private float targetAlpha;
        private System.Action pendingCallback;

        /// <summary>Current overlay opacity (0–1).</summary>
        public float CurrentOpacity { get; private set; }

        private bool UsingDefaultCanvas => DefaultFadeCanvas != null;

        /// <summary>Lazily creates the overlay canvas.</summary>
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureOverlay()
        {
            if (UsingDefaultCanvas)
            {
                if (fadeImage == null)
                {
                    fadeImage = DefaultFadeCanvas.GetComponentInChildren<Image>(true);
                    if (fadeImage != null)
                        fadeImage.color = Color.black;
                    DefaultFadeCanvas.alpha = 0f;
                }
                return;
            }
            if (fadeImage != null)
                return;
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 0.1f;
            canvas.sortingOrder = short.MaxValue;
            var imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, false);
            fadeImage = imageGo.AddComponent<Image>();
            var rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            fadeImage.color = Color.clear;
            fadeImage.raycastTarget = false;
        }

        /// <summary>Fade the screen in/out over <see cref="FadeSettings.FadeTime"/>.</summary>
        public void Fade(bool toBlack)
        {
            EnsureOverlay();
            var settings = TrainingConfig.Instance != null ? TrainingConfig.Instance.FadeSettings : null;
            float duration = settings != null ? settings.FadeDuration : 0.5f;
            targetAlpha = toBlack ? 1f : 0f;
            StartFade(duration, null, settings != null && settings.UseUnscaledTime);
        }

        /// <summary>Fade using explicit settings.</summary>
        public void FadeCanvasGroup(Settings.FadeSettings settings) => FadeCanvasGroup(settings, null);

        /// <summary>Fade using explicit settings and invoke a completion callback once the target alpha is reached.</summary>
        public void FadeCanvasGroup(Settings.FadeSettings settings, System.Action onFadeComplete)
        {
            EnsureOverlay();
            if (settings != null)
                fadeImage.color = settings.UseCustomColor ? settings.FadeColor : Color.black;
            float duration = settings?.FadeDuration ?? 0.5f;
            targetAlpha = settings?.TargetAlpha ?? 1f;
            StartFade(duration, onFadeComplete, settings != null && settings.UseUnscaledTime);
        }

        /// <summary>Starts a new fade; a fade that gets replaced fires its callback immediately so waiting steps never hang.</summary>
        private void StartFade(float duration, System.Action onFadeComplete, bool useUnscaledTime = false)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            var replaced = pendingCallback;
            pendingCallback = onFadeComplete;
            replaced?.Invoke();
            fadeCoroutine = StartCoroutine(FadeCoroutine(duration, useUnscaledTime));
        }

        /// <summary>Convenience: fade to black.</summary>
        public void FadeToBlack() => Fade(true);

        /// <summary>Convenience: fade back to clear.</summary>
        public void FadeToClear() => Fade(false);

        private void SetOpacity(float alpha)
        {
            CurrentOpacity = alpha;
            if (UsingDefaultCanvas)
            {
                DefaultFadeCanvas.alpha = alpha;
                return;
            }
            if (fadeImage == null)
                return;
            var color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }

        private IEnumerator FadeCoroutine(float duration, bool useUnscaledTime)
        {
            bool useDefault = UsingDefaultCanvas;
            float start = CurrentOpacity;
            for (float t = 0f; t < duration;
                 t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime)
            {
                if (useDefault && DefaultFadeCanvas == null)
                    yield break;
                SetOpacity(Mathf.Lerp(start, targetAlpha, t / duration));
                yield return null;
            }
            SetOpacity(targetAlpha);
            fadeCoroutine = null;
            var callback = pendingCallback;
            pendingCallback = null;
            callback?.Invoke();
        }
    }
}

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>Freezes a desktop user by disabling UI event systems and pausing time.</summary>
    public class DesktopFreezeBehaviour : Multiuser.IFreezeBehaviour
    {
        /// <inheritdoc/>
        public override void Freeze()
        {
            if (!IsJoiningUser)
                System.Array.ForEach(EventSystems, x => x.enabled = false);
            FadeManager.Instance?.FadeCanvasGroup(new Settings.FadeSettings(false, Color.black, IsJoiningUser ? 0f : 0.6f, IsJoiningUser ? 0.0f : 0.8f, true));
            Time.timeScale = 0;
        }

        /// <inheritdoc/>
        public override void Unfreeze()
        {
            System.Array.ForEach(EventSystems, x => x.enabled = true);
            FadeManager.Instance?.FadeCanvasGroup(new Settings.FadeSettings(false, Color.black, 0.6f, 0.0f, true));
            Time.timeScale = 1;
        }
    }
}
