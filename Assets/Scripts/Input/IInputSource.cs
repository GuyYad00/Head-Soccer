namespace HeadSoccer
{
    /// <summary>
    /// One interface for every way a character can be driven: keyboard, touch or AI.
    /// PlayerController never knows which one it is holding, so the same movement,
    /// jump and kick code runs for a human and for the CPU.
    /// </summary>
    public interface IInputSource
    {
        /// <summary>Called once per frame from Update, before the properties are read.</summary>
        void Poll();

        /// <summary>-1 for left, 0 for still, +1 for right.</summary>
        float Horizontal { get; }

        bool JumpPressedThisFrame { get; }

        bool KickPressedThisFrame { get; }

        bool SuperPressedThisFrame { get; }
    }
}
