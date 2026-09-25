using UnityEngine;

namespace HeadSoccer
{
    /// <summary>Waves both arms on a stadium spectator. Each fan has its own timing.</summary>
    public class CrowdFan : MonoBehaviour
    {
        public Transform leftArm;
        public Transform rightArm;
        public float phase;
        public float speed = 4.2f;
        public float amount = 26f;

        private void LateUpdate()
        {
            float wave = Mathf.Sin(Time.time * speed + phase) * amount;
            if (leftArm != null)
                leftArm.localRotation = Quaternion.Euler(0f, 0f, 55f + wave);
            if (rightArm != null)
                rightArm.localRotation = Quaternion.Euler(0f, 0f, -55f - wave);
        }
    }
}
