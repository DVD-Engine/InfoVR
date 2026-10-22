using UnityEngine;

public class Goal : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.collider.GetComponent<Player>();
        if (player != null && GameManager.Instance != null)
        {
            GameManager.Instance.OnGoalReached();
        }
    }
}
