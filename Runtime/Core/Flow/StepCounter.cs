namespace PixoVR.TrainingCore.Flow
{
    /// <summary>Global counter of completed steps; commands are stamped with <see cref="Current"/>.</summary>
    public static class StepCounter
    {
        /// <summary>Current step counter value.</summary>
        public static int Current { get; private set; }

        /// <summary>Reset the counter to a value.</summary>
        public static void InitializeTo(int value) => Current = value;

        /// <summary>Increment and return the new value.</summary>
        public static int Increment() => ++Current;

        /// <summary>Decrement and return the new value.</summary>
        public static int Decrement() => --Current;
    }
}
