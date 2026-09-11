using HighlightPlus;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore.Highlight
{
    /// <summary>
    /// <see cref="HighlightBase"/> adapter over the HighlightPlus asset's <see cref="HighlightEffect"/>.
    /// Requires the HighlightPlus asset in the project and the HIGHLIGHT_PLUS scripting define.
    /// </summary>
    [RequireComponent(typeof(HighlightEffect))]
    public class HighlightPlusHighlighter : HighlightBase
    {
        private HighlightEffect highlighter;

        private void Awake()
        {
            highlighter = GetComponent<HighlightEffect>();
        }

        /// <summary>Enable the outline effect.</summary>
        public override void Highlight()
        {
            base.Highlight();
            SetHighlighted(true);
        }

        /// <summary>Disable the outline effect.</summary>
        public override void Unhighlight()
        {
            base.Unhighlight();
            SetHighlighted(false);
        }

        private void SetHighlighted(bool state)
        {
            if (highlighter == null)
                highlighter = GetComponent<HighlightEffect>();
            if (highlighter != null)
                highlighter.SetHighlighted(state);
        }
    }
}
