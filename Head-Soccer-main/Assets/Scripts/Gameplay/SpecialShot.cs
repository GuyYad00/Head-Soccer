using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The once-per-match Super. It charges only from real contact with the ball
    /// (touching it or connecting a kick) and, when full, turns the next kick into a
    /// boosted shot with a short slow motion window. Owned by one PlayerController.
    /// </summary>
    public class SpecialShot
    {
        private readonly GameConfig config;
        private float charge;

        public SpecialShot(GameConfig config)
        {
            this.config = config;
        }

        /// <summary>0..1, how full the meter is.</summary>
        public float Charge => charge;

        /// <summary>True once the meter is full and the shot has not been spent.</summary>
        public bool IsReady => !IsUsed && charge >= 1f;

        /// <summary>True after the shot has been fired this match.</summary>
        public bool IsUsed { get; private set; }

        /// <summary>Called every physics step while the ball is inside the charge reach.</summary>
        public void ChargeFromContact(float deltaTime)
        {
            float time = Mathf.Max(0.35f, config.superChargeTime);
            Add(deltaTime / time);
        }

        /// <summary>A connecting kick is worth a fixed slice of the meter.</summary>
        public void ChargeFromKick() => Add(config.superKickCharge);

        /// <summary>The ball bouncing off the head or body is worth a smaller slice.</summary>
        public void ChargeFromBounce() => Add(config.superBounceCharge);

        /// <summary>Spends the meter. Returns false if it was not ready.</summary>
        public bool TryFire()
        {
            if (!IsReady) return false;
            IsUsed = true;
            charge = 0f;
            return true;
        }

        public void Reset()
        {
            IsUsed = false;
            charge = 0f;
        }

        private void Add(float amount)
        {
            if (IsUsed) return;
            charge = Mathf.Min(1f, charge + amount);
        }
    }
}
