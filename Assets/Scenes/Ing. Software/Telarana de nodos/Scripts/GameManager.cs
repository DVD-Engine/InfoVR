using UnityEngine;

public enum Direction { Up, Right, Down, Left }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int movementLimit = 3;

    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Goal goal;

    private int padsTouched = 0;
    private bool gameStarted = false;
    private bool gameEnded = false;

    public bool IsPlaying => gameStarted && !gameEnded;
    public bool IsSetupPhase => !gameStarted && !gameEnded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartGame()
    {
        if (gameStarted || gameEnded) return;

        gameStarted = true;
        Debug.Log("Game start");
        player.StartMoving();
    }

    public void OnPadTouched(int padNumber, Direction newDirection)
    {
        if (!IsPlaying) return;

        padsTouched++;
        Debug.Log($"Movepad #{padNumber} touched, switching player direction to {newDirection}");
        player.SetDirection(newDirection);

        if (padsTouched >= movementLimit)
        {
            EndGame(false);
        }
    }

    public void OnGoalReached()
    {
        if (!IsPlaying) return;
        EndGame(true);
    }

    private void EndGame(bool won)
    {
        gameEnded = true;
        player.StopMoving();

        if (won)
            Debug.Log("Goal reached");
        else
            Debug.Log("Limit met");
    }
}
