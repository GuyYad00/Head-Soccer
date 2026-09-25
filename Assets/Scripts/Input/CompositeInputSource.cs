using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// Merges several input sources into one, so the same character answers to the
    /// keyboard, a gamepad and the on-screen buttons at the same time. Handy in the
    /// editor for testing the touch layout without deploying to a phone.
    /// </summary>
    public class CompositeInputSource : IInputSource
    {
        private readonly IInputSource[] sources;

        public float Horizontal { get; private set; }
        public bool JumpPressedThisFrame { get; private set; }
        public bool KickPressedThisFrame { get; private set; }
        public bool SuperPressedThisFrame { get; private set; }

        public CompositeInputSource(params IInputSource[] sources)
        {
            this.sources = sources;
        }

        public void Poll()
        {
            float horizontal = 0f;
            bool jump = false;
            bool kick = false;
            bool super = false;

            foreach (IInputSource source in sources)
            {
                if (source == null) continue;
                source.Poll();

                if (Mathf.Abs(source.Horizontal) > Mathf.Abs(horizontal))
                    horizontal = source.Horizontal;

                jump |= source.JumpPressedThisFrame;
                kick |= source.KickPressedThisFrame;
                super |= source.SuperPressedThisFrame;
            }

            Horizontal = horizontal;
            JumpPressedThisFrame = jump;
            KickPressedThisFrame = kick;
            SuperPressedThisFrame = super;
        }
    }
}
