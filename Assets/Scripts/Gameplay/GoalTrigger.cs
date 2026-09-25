using UnityEngine;

namespace HeadSoccer
{
    /// <summary>
    /// The mouth of one goal. Sits just inside the net so that by the time the ball
    /// centre enters, the ball has fully crossed the line.
    /// A goal only counts while the match state is Playing, which is what stops the
    /// same ball entry from scoring twice during the celebration freeze.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [Tooltip("Which side of the pitch this goal belongs to. The other player scores here.")]
        [SerializeField] private Side goalOwner = Side.Left;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State != MatchState.Playing) return;
            if (other.GetComponentInParent<BallController>() == null) return;

            Side scorer = goalOwner == Side.Left ? Side.Right : Side.Left;
            GameManager.Instance.ScoreGoal(scorer);
        }
    }
}
