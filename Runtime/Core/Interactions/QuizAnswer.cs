using System;
using System.Collections.Generic;
using System.Linq;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Events;
using PixoVR.TrainingCore.Identity;
using UnityEngine;
using UnityEngine.Events;

namespace PixoVR.TrainingCore.Interactions
{
    /// <summary>A single clickable answer option bound to a <see cref="ToggleInfoPointLabel"/>.</summary>
    public class QuizAnswer : MonoBehaviour
    {
        /// <summary>Text for displaying this answer.</summary>
        public TMPro.TextMeshProUGUI AnswerText;

        /// <summary>If this is a correct or incorrect option.</summary>
        [HideInInspector]
        public bool Correct = false;

        /// <summary>Info point this object belongs to.</summary>
        [HideInInspector]
        public ToggleInfoPointLabel InfoPoint;

        /// <summary>Sets the answer text.</summary>
        public void SetText(string answerText)
        {
            if (AnswerText != null)
                AnswerText.text = answerText;
        }

        /// <summary>Informs the info point that this option was selected.</summary>
        public void OptionChosen() => InfoPoint?.AnswerChosen(Correct);
    }
}
