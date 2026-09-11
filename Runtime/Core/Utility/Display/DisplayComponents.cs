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

    /// <summary>Places a display object relative to the camera using <see cref="PlacerSettings"/>.</summary>
    public class DisplayObjectPlacer : MonoBehaviour
    {
        /// <summary>Placement settings.</summary>
        public PlacerSettings Settings;

        /// <summary>Placement origin override.</summary>
        public Transform OriginTransform;

        /// <summary>Off-screen arrow (left).</summary>
        public ArrowPlacer OffScreenLeft;

        /// <summary>Off-screen arrow (right).</summary>
        public ArrowPlacer OffScreenRight;

        /// <summary>Show debug gizmos.</summary>
        public bool ShowGizmos;

        /// <summary>The object being placed.</summary>
        public GameObject PlacementObject;

        /// <summary>Live placement point.</summary>
        public Vector3 PlacementLocation { get; private set; }

        /// <summary>Live rotation.</summary>
        public Quaternion PlacementRotation { get; private set; }

        /// <summary>Seconds spent on placement.</summary>
        public float TotalPlacementTime => _totalPlacementTime;

        /// <summary>Whether the object is currently visible on screen.</summary>
        public bool ObjectOnScreen => _objectOnScreen;

        private float _totalPlacementTime;
        private bool _objectOnScreen = true;
        private bool _placementInitiated;
        private bool _placementFinished;

        /// <summary>Show and place the object in front of the camera.</summary>
        public virtual void ShowObject()
        {
            if (PlacementObject == null)
                return;
            var origin = OriginTransform != null
                ? OriginTransform
                : (Camera.main != null ? Camera.main.transform : transform);
            float dist = Settings != null ? Mathf.Max(Settings.TargetOriginDistance, Settings.MinimumDistanceFromCamera) : 2f;
            float height = Settings != null ? Settings.DisplayHeight : 0f;
            PlacementObject.SetActive(true);
            PlacementObject.transform.position = origin.position + origin.forward * dist + Vector3.up * height;
            PlacementObject.transform.rotation = Quaternion.LookRotation(origin.forward);
            PlacementLocation = PlacementObject.transform.position;
            PlacementRotation = PlacementObject.transform.rotation;
            _placementFinished = true;
        }

        /// <summary>Hide the object.</summary>
        public virtual void HideObject()
        {
            if (PlacementObject != null)
                PlacementObject.SetActive(false);
            _placementFinished = false;
            _placementInitiated = false;
        }

        /// <summary>Start interactive placement.</summary>
        public void StartPlacementMode()
        {
            _placementInitiated = true;
            _placementFinished = false;
        }

        /// <summary>Stop placement without finishing.</summary>
        public void StopPlacementMode() => _placementInitiated = false;

        private void Update()
        {
            if (!_placementInitiated)
                return;
            _totalPlacementTime += Time.deltaTime;
        }
    }

    /// <summary>Rotates an object to always face a target (billboard).</summary>
    public class Billboard : MonoBehaviour
    {
        /// <summary>Local axis that tracks the target.</summary>
        public Vector3 pivotAxis = Vector3.up;

        /// <summary>The object to rotate (defaults to self).</summary>
        public Transform ObjectToRotate;

        /// <summary>Rotation lerp speed.</summary>
        public float LerpSpeed = 10f;

        /// <summary>Target transform (defaults to main camera).</summary>
        public Transform targetTransform;

        /// <summary>Axis to pivot around (alias for <see cref="pivotAxis"/>).</summary>
        public Vector3 PivotAxis => pivotAxis;

        private void Update()
        {
            var body = ObjectToRotate != null ? ObjectToRotate : transform;
            var target = targetTransform != null
                ? targetTransform
                : (Camera.main != null ? Camera.main.transform : null);
            if (target == null)
                return;
            var look = Quaternion.LookRotation(body.position - target.position, pivotAxis);
            body.rotation = Quaternion.Slerp(body.rotation, look, Time.deltaTime * LerpSpeed);
        }
    }

    /// <summary>Spawns an arrow pointing from a source object toward a target.</summary>
    public class ArrowPlacer : MonoBehaviour
    {
        /// <summary>Placement settings.</summary>
        public ArrowPlacerSettings Settings;

        /// <summary>Arrow prefab override (uses <see cref="ArrowPlacerSettings.ArrowPrefab"/> when null).</summary>
        public GameObject ArrowPrefab;

        /// <summary>The spawned arrow instance.</summary>
        public GameObject Arrow { get; private set; }

        /// <summary>Place an arrow on <paramref name="source"/> pointing at <paramref name="target"/>.</summary>
        public virtual void PlaceArrow(GameObject source, GameObject target)
        {
            var prefab = ArrowPrefab != null ? ArrowPrefab : Settings?.ArrowPrefab;
            if (prefab == null || source == null || target == null)
                return;
            if (Arrow == null)
            {
                Arrow = Instantiate(prefab);
                if (Settings != null && Settings.SpawnAsChild)
                    Arrow.transform.SetParent(source.transform, false);
            }
            float offset = Settings?.Offset ?? 0f;
            Arrow.transform.position = source.transform.position + Vector3.up * offset;
            Arrow.transform.LookAt(target.transform);
            if (Settings != null && Settings.Degrees != Vector2.zero)
                Arrow.transform.Rotate(Settings.Degrees.x, Settings.Degrees.y, 0f);
        }

        /// <summary>Destroy the arrow.</summary>
        public virtual void ClearArrow()
        {
            if (Arrow != null)
                Destroy(Arrow);
        }
    }

    /// <summary>Emits a <see cref="Events.DisplayInteractionEventArgs"/> when invoked (e.g. by a UI button).</summary>
    [RequireComponent(typeof(Events.ObservableSubject))]
    public class DisplayInteraction : MonoBehaviour
    {
        [SerializeField]
        private Events.ObservableSubject subject;

        private void Awake()
        {
            subject = GetComponent<Events.ObservableSubject>();
        }

        /// <summary>Record the interaction command and publish the event.</summary>
        public void OnInteractionEvent()
        {
            var args = new Events.DisplayInteractionEventArgs(subject != null ? subject.Id : null, null);
            var command = args.ToCommand();
            if (command != null)
                Commands.CommandHistory.Instance.Record(command);
            Events.EventBus.Instance.Publish(args.SubjectId, args);
        }
    }
}
