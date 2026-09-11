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
    /// <summary>Abstract info-point: an openable/closable UI anchor bound to an <see cref="ObservableSubject"/>.</summary>
    [RequireComponent(typeof(ObservableSubject))]
    public abstract class InfoPointBase : MonoBehaviour
    {
        /// <summary>Close sibling info points when this one opens.</summary>
        public bool ShouldCloseAllOtherPoints = true;

        private ObservableSubject observableSubject;

        [SerializeField]
        protected bool IsOpen = false;

        /// <summary>Raised when the interaction completes; argument is correctness.</summary>
        public event Action<bool> CompleteInteraction;

        /// <summary>Raised when the open state is reverted.</summary>
        public event Action OnReverted;

        /// <summary>See the interface/base contract.</summary>
        protected virtual void Awake()
        {
            observableSubject = GetComponent<ObservableSubject>();
        }

        /// <summary>Report a completed interaction.</summary>
        protected void OnInteractionCompleted(bool correct) => CompleteInteraction?.Invoke(correct);

        /// <summary>Open the point, optionally closing others.</summary>
        public virtual void Open()
        {
            if (ShouldCloseAllOtherPoints)
                CloseOtherInfoPoints();
            IsOpen = true;
        }

        /// <summary>Close the point.</summary>
        public virtual void Close() => IsOpen = false;

        /// <summary>Toggle open state; returns the new state.</summary>
        public bool Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
            return IsOpen;
        }

        private void CloseOtherInfoPoints()
        {
            foreach (var point in FindObjectsOfType<InfoPointBase>())
            {
                if (point != this && point.IsOpen)
                    point.Close();
            }
        }

        /// <summary>Undo a previous completion.</summary>
        protected virtual void RevertCompletion() => OnReverted?.Invoke();
    }

    /// <summary>Simple info point completed by a key press or explicit call.</summary>
    public class InfoPointCoreImplementation : InfoPointBase
    {
        /// <summary>Complete as correct rather than incorrect.</summary>
        public bool CompleteCorrectly = true;

        /// <summary>Keyboard shortcut that completes the interaction.</summary>
        public KeyCode KeyToPress;

        private void Update()
        {
            if (KeyToPress != KeyCode.None && Input.GetKeyDown(KeyToPress))
                OnInteractionCompleted(CompleteCorrectly);
        }

        /// <inheritdoc/>
        public override void Close() => base.Close();
    }

    /// <summary>Single info point that participates in networked open/close sync.</summary>
    public class NetworkInfoPoint : MonoBehaviour
    {
        /// <summary>Toggle the point and notify the manager.</summary>
        public virtual bool Toggle()
        {
            if (NetworkInfoPointManager.Instance != null)
            {
                NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
                return true;
            }
            return false;
        }

        /// <summary>Open the point.</summary>
        public virtual void Open()
        {
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastOpen(GetGuid());
        }

        /// <summary>Close the point.</summary>
        public virtual void Close()
        {
            if (NetworkInfoPointManager.Instance != null)
                NetworkInfoPointManager.Instance.BroadCastClose(GetGuid());
        }

        private string GetGuid() => gameObject.GetGuidString();
    }

    /// <summary>Tracks open info points and relays open/close state across the session.</summary>
    public class NetworkInfoPointManager : Utility.SingletonBehaviour<NetworkInfoPointManager>
    {
        /// <summary>Only one point may be open at once.</summary>
        public bool OnlyAllowSinglePointOpen;

        /// <summary>Disable only the point's children rather than the whole object.</summary>
        public bool DisableInfoPointChildrenOnly;

        private readonly HashSet<string> registered = new HashSet<string>();
        private readonly Dictionary<string, bool> openStates = new Dictionary<string, bool>();

        /// <summary>Raised when a point's enable state is applied; guid, enabled.</summary>
        public event Action<string, bool> OnInfoPointStateChanged;

        /// <summary>Register a point by guid.</summary>
        public void RegisterInfoPoint(string guid) => registered.Add(guid);

        /// <summary>Unregister a point by guid.</summary>
        public void UnregisterInfoPoint(string guid)
        {
            registered.Remove(guid);
            openStates.Remove(guid);
        }

        /// <summary>Open a point and broadcast it.</summary>
        public void BroadCastOpen(string guid) => SetState(guid, true);

        /// <summary>Close a point and broadcast it.</summary>
        public void BroadCastClose(string guid) => SetState(guid, false);

        /// <summary>Open a video content point and broadcast it.</summary>
        public void BroadCastVideoOpen(string guid) => SetState(guid, true);

        /// <summary>Close a video content point and broadcast it.</summary>
        public void BroadCastVideoClose(string guid) => SetState(guid, false);

        /// <summary>Open a picture content point and broadcast it.</summary>
        public void BroadCastPictureOpen(string guid) => SetState(guid, true);

        /// <summary>Close a picture content point and broadcast it.</summary>
        public void BroadCastPictureClose(string guid) => SetState(guid, false);

        /// <summary>Disable a point and broadcast it.</summary>
        public void BroadCastDisableInfoPoint(string guid) => SetState(guid, false);

        /// <summary>Apply an enable state received from the network.</summary>
        public void ReceiveSetInfoPointEnableState(string guid, bool state) => SetState(guid, state, broadcast: false);

        /// <summary>Push all known point states to a newly joined player.</summary>
        public void UpdateNewPlayerOfStatus()
        {
            foreach (var pair in openStates)
                OnInfoPointStateChanged?.Invoke(pair.Key, pair.Value);
        }

        private void SetState(string guid, bool state, bool broadcast = true)
        {
            if (OnlyAllowSinglePointOpen && state)
            {
                foreach (var key in openStates.Keys.ToList())
                    openStates[key] = false;
            }
            openStates[guid] = state;
            OnInfoPointStateChanged?.Invoke(guid, state);
        }
    }

    /// <summary>Bool UnityEvent used by info-point/quiz components.</summary>
    [Serializable]
    public class UnityBoolEvent : UnityEvent<bool> { }

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
            base.Open();
            if (InfoPointLabel != null)
                InfoPointLabel.SetActive(true);
            if (Animator != null)
                Animator.SetBool("Open", true);
            SpawnAnswers();
            if (CompleteStepOnOpeningInTrainingMode)
                OnInteractionCompleted(true);
        }

        /// <inheritdoc/>
        public override void Close()
        {
            base.Close();
            if (InfoPointLabel != null)
                InfoPointLabel.SetActive(false);
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
            }
        }

        /// <summary>Spawn the configured answer options.</summary>
        protected virtual void SpawnAnswers()
        {
            if (AnswerParent == null || AnswerPrefab == null)
                return;
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
