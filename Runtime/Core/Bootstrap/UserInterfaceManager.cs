using System.Collections;
using PixoVR.TrainingCore.Flow;
using PixoVR.TrainingCore.GameModes;
using PixoVR.TrainingCore.SceneManagement;
using PixoVR.TrainingCore.Utility;
using UnityEngine;

namespace PixoVR.TrainingCore
{
    /// <summary>Minimal UI coordinator: show/hide flow-driven UI roots.</summary>
    public class UserInterfaceManager : SingletonBehaviour<UserInterfaceManager>
    {
        /// <summary>The main display object placer.</summary>
        public Utility.Display.DisplayObjectPlacer MainDisplayer;

        /// <summary>Whether UI is currently visible.</summary>
        public bool IsVisible { get; private set; } = true;

        /// <summary>Show or hide all managed UI.</summary>
        public void SetVisible(bool visible) => IsVisible = visible;
    }
}
