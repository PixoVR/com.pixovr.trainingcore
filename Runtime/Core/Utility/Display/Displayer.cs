using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
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

        /// <summary>Optional audio manager.</summary>
        public AudioManager AudioClipPlaybackManager;

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
            if (data.AudioSettings != null && AudioClipPlaybackManager != null)
                AudioClipPlaybackManager.Play(data.AudioSettings);
        }

        private DisplayData displayedData;

        /// <summary>Populate all slots from data (Luminous name: DisplayTextData).</summary>
        public virtual void DisplayTextData(DisplayData data, AnswerData answerData = null)
        {
            displayedData = data;
            SetContent(data);
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
