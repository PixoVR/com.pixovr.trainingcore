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
    /// <summary>Info point that toggles a label and can present a multiple-choice quiz.</summary>
    public class ToggleInfoPointLabel : InfoPointBase
    {
        /// <summary>Label object toggled on open.</summary>
        public GameObject InfoPointLabel;

        /// <summary>Quiz label object.</summary>
        public GameObject QuizLabel;

        /// <summary>Optional line shown from the point to its label.</summary>
        public LineRenderer LineRenderer;

        /// <summary>Mark the current step complete when opened during training.</summary>
        public bool CompleteStepOnOpeningInTrainingMode = true;

        /// <summary>Disable the point once the step completes.</summary>
        public bool DisableInfoPointOnComplete = true;

        /// <summary>Id of the player who opened the point.</summary>
        public int OpenerId;

        /// <summary>Correct answer pool.</summary>
        public List<QuizAnswerData> CorrectAnswers = new List<QuizAnswerData>();

        /// <summary>Answers that must always be displayed.</summary>
        public List<QuizAnswerData> AlwaysDisplayedAnswers = new List<QuizAnswerData>();

        /// <summary>Incorrect answer pool.</summary>
        public List<QuizAnswerData> IncorrectAnswers = new List<QuizAnswerData>();

        /// <summary>How many answers are shown.</summary>
        public int AnswerCount = 3;

        /// <summary>Parent transform for spawned answers.</summary>
        public Transform AnswerParent;

        /// <summary>Prefab spawned per answer.</summary>
        public GameObject AnswerPrefab;

        /// <summary>Fired when an answer is chosen; argument is correctness.</summary>
        public UnityBoolEvent AnswerChosenEvent;

        /// <summary>Optional open/close animator.</summary>
        public Animator Animator;

        /// <summary>Audio played on a correct answer.</summary>
        public AudioSource CorrectAnswerAudioSource;

        /// <summary>Open the point for a player.</summary>
        public virtual void Open(int playerId, bool forceCloseAllOtherPoints)
        {
            OpenerId = playerId;
            if (forceCloseAllOtherPoints)
                ShouldCloseAllOtherPoints = true;
            Open();
        }

        /// <inheritdoc/>
        public override void Open()
        {
            if (IsOpen)
                return;
            base.Open();
            if (InfoPointLabel != null)
                InfoPointLabel.SetActive(true);
            if (QuizLabel != null)
                QuizLabel.SetActive(true);
            if (LineRenderer != null)
                LineRenderer.gameObject.SetActive(true);
            if (Animator != null)
                Animator.SetBool("Open", true);
            SpawnAnswers();
            if (CompleteStepOnOpeningInTrainingMode && GameModes.GameModeManager.CurrentMode == GameModes.GameMode.Training)
                OnInteractionCompleted(true);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            base.Close();
            if (InfoPointLabel != null)
                InfoPointLabel.SetActive(false);
            if (QuizLabel != null)
                QuizLabel.SetActive(false);
            if (LineRenderer != null)
                LineRenderer.gameObject.SetActive(false);
            if (Animator != null)
                Animator.SetBool("Open", false);
        }

        /// <summary>Called by a <see cref="QuizAnswer"/> when chosen.</summary>
        public virtual void AnswerChosen(bool correct)
        {
            AnswerChosenEvent?.Invoke(correct);
            if (correct)
            {
                if (CorrectAnswerAudioSource != null)
                    CorrectAnswerAudioSource.Play();
                OnInteractionCompleted(true);
                if (DisableInfoPointOnComplete)
                    gameObject.SetActive(false);
            }
        }

        /// <summary>Spawn the configured answer options.</summary>
        protected virtual void SpawnAnswers()
        {
            if (AnswerParent == null || AnswerPrefab == null)
                return;
            for (int i = AnswerParent.childCount - 1; i >= 0; i--)
                Destroy(AnswerParent.GetChild(i).gameObject);
            var picked = new List<QuizAnswerData>();
            picked.AddRange(AlwaysDisplayedAnswers);
            if (CorrectAnswers.Count > 0 && !picked.Any(a => CorrectAnswers.Contains(a)))
                picked.Add(CorrectAnswers[UnityEngine.Random.Range(0, CorrectAnswers.Count)]);
            var incorrectPool = new List<QuizAnswerData>(IncorrectAnswers);
            while (picked.Count < AnswerCount && incorrectPool.Count > 0)
            {
                int i = UnityEngine.Random.Range(0, incorrectPool.Count);
                picked.Add(incorrectPool[i]);
                incorrectPool.RemoveAt(i);
            }
            foreach (var data in picked.Take(AnswerCount))
            {
                var go = Instantiate(AnswerPrefab, AnswerParent);
                var answer = go.GetComponent<QuizAnswer>();
                if (answer != null)
                {
                    answer.SetText(data != null ? data.AnswerText : string.Empty);
                    answer.Correct = data != null && CorrectAnswers.Contains(data);
                    answer.InfoPoint = this;
                }
            }
        }
    }
}
