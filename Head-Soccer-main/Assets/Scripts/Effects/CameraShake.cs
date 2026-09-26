using UnityEngine;

namespace HeadSoccer
{
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 rest;
        private float remaining;
        private float strength;

        private void Awake()
        {
            Instance = this;
            rest = transform.localPosition;
        }

        public static void Play(float duration, float amount)
        {
            if (Instance == null && Camera.main != null)
                Camera.main.gameObject.AddComponent<CameraShake>();

            if (Instance == null) return;
            Instance.remaining = duration;
            Instance.strength = amount;
        }

        private void LateUpdate()
        {
            if (remaining <= 0f)
            {
                transform.localPosition = rest;
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * strength);
        }
    }
}
