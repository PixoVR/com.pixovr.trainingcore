using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Renders <see cref="DisplayData"/> onto text/image/video slots.</summary>
    public class Displayer : MonoBehaviour
    {
        /// <summary>Title slot.</summary>
        public TMPro.TextMeshProUGUI TitleText;

        /// <summary>Subtitle slot.</summary>
        public TMPro.TextMeshProUGUI SubtitleText;

        /// <summary>Body slot.</summary>
        public TMPro.TextMeshProUGUI BodyText;

        /// <summary>Image slot.</summary>
        public UnityEngine.UI.Image DisplayImage;

        /// <summary>Optional queued-clip audio manager.</summary>
        public AudioClipPlaybackManager AudioClipPlaybackManager;

        /// <summary>Parent for answer buttons.</summary>
        public Transform AnswersParent;

        /// <summary>Confirm button.</summary>
        public UnityEngine.UI.Button ConfirmButton;

        /// <summary>Line renderer to the connection point.</summary>
        public LineRenderer ConnectionLine;

        /// <summary>Video display transforms.</summary>
        public List<Transform> VideoDisplays = new List<Transform>();

        /// <summary>Image display transforms.</summary>
        public List<Transform> ImageDisplays = new List<Transform>();

        /// <summary>Whether to draw the connection line.</summary>
        public bool UpdateLines = true;

        /// <summary>Populate all slots from data.</summary>
        public virtual void SetContent(DisplayData data)
        {
            if (data == null)
                return;
            if (TitleText != null)
                TitleText.text = data.Title;
            if (SubtitleText != null)
                SubtitleText.text = data.Subtitle;
            if (BodyText != null)
                BodyText.text = data.Body;
            if (DisplayImage != null)
            {
                DisplayImage.enabled = data.Sprites != null && data.Sprites.Count > 0;
                if (DisplayImage.enabled)
                    DisplayImage.sprite = data.Sprites[0];
            }
            if (data.AudioSettings != null)
            {
                if (AudioClipPlaybackManager != null)
                    AudioClipPlaybackManager.ApplySettings(data.AudioSettings, true);
                else
                    AudioManager.Instance?.Play(data.AudioSettings);
            }
        }

        private DisplayData displayedData;

        /// <summary>Populate all slots from data (Luminous name: DisplayTextData).</summary>
        public virtual void DisplayTextData(DisplayData data, AnswerData answerData = null)
        {
            displayedData = data;
            SetContent(data);
            if (answerData != null)
                SpawnAnswers(answerData);
        }

        private void SpawnAnswers(AnswerData answerData)
        {
            if (AnswersParent == null || answerData.Prefab == null || answerData.Answers == null)
                return;
            for (int i = AnswersParent.childCount - 1; i >= 0; i--)
                Destroy(AnswersParent.GetChild(i).gameObject);
            var picked = new List<Answer>();
            var correct = answerData.Answers.FindAll(a => a.Correct);
            var incorrect = answerData.Answers.FindAll(a => !a.Correct);
            if (correct.Count > 0)
                picked.Add(correct[UnityEngine.Random.Range(0, correct.Count)]);
            int needed = Mathf.Max(0, answerData.DisplayCount - picked.Count);
            for (int i = 0; i < incorrect.Count && picked.Count - 1 < needed; i++)
                picked.Add(incorrect[i]);
            if (answerData.RandomOrder)
            {
                for (int i = picked.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (picked[i], picked[j]) = (picked[j], picked[i]);
                }
            }
            var subject = GetComponentInParent<Events.ObservableSubject>();
            var subjectId = subject != null ? subject.Id : gameObject.GetGuidString();
            foreach (var answer in picked)
            {
                var go = Instantiate(answerData.Prefab, AnswersParent);
                var label = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null)
                    label.text = answer.Text;
                var button = go.GetComponentInChildren<UnityEngine.UI.Button>();
                if (button != null)
                {
                    var chosen = answer;
                    button.onClick.AddListener(() =>
                        Events.EventBus.Instance.Publish(subjectId,
                            new Events.QuestionInteractionEventArgs(subjectId, chosen.Correct)));
                }
            }
        }

        /// <summary>The data currently displayed.</summary>
        public virtual DisplayData GetDisplayedData() => displayedData;

        /// <summary>Show or hide the body text.</summary>
        public virtual void SetBodyTextState(bool state)
        {
            if (BodyText != null)
                BodyText.gameObject.SetActive(state);
        }

        /// <summary>Aim the connection line at a world point.</summary>
        public virtual void SetConnectionPoint(Vector3 worldPoint)
        {
            if (ConnectionLine == null || !UpdateLines)
                return;
            ConnectionLine.positionCount = 2;
            ConnectionLine.SetPosition(0, transform.position);
            ConnectionLine.SetPosition(1, worldPoint);
        }

        /// <summary>Close the display.</summary>
        public virtual void Close() => gameObject.SetActive(false);
    }




}
