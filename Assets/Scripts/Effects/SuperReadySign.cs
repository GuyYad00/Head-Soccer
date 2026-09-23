using UnityEngine;

namespace HeadSoccer
{
    /// <summary>A bouncing bang mark above the head, only while Super is ready.</summary>
    public class SuperReadySign : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private Transform mark;

        public void Setup(PlayerController owner, Transform sign)
        {
            player = owner;
            mark = sign;
            if (mark != null) mark.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (mark == null || player == null) return;

            bool show = player.SpecialReady;
            if (mark.gameObject.activeSelf != show)
                mark.gameObject.SetActive(show);

            if (!show) return;

            float bounce = Mathf.Sin(Time.unscaledTime * 8f) * 0.08f;
            float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 10f)) * 0.25f;
            mark.localPosition = new Vector3(0f, 1.45f + bounce, 0f);
            mark.localScale = Vector3.one * pulse;
        }
    }
}
