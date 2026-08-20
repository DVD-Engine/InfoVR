using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Direction currentDirection = Direction.Up;
    private bool isMoving = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable xrInteractable;
    private Rigidbody rb;

    // World-space vectors for each direction (top-down XZ plane)
    private static readonly Vector3[] directionVectors =
    {
        Vector3.forward,  // Up
        Vector3.right,    // Right
        Vector3.back,     // Down
        Vector3.left      // Left
    };

    // Z-axis rotation for each direction (matches visual facing)
    private static readonly float[] directionAngles = { 0f, 270f, 180f, 90f };

    public Direction CurrentDirection => currentDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        xrInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (xrInteractable != null)
            xrInteractable.selectEntered.AddListener(OnXRSelect);
    }

    private void OnDestroy()
    {
        if (xrInteractable != null)
            xrInteractable.selectEntered.RemoveListener(OnXRSelect);
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsSetupPhase)
            CycleDirection();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsSetupPhase)
        {
            CycleDirection();
        }
    }

    private void CycleDirection()
    {
        currentDirection = (Direction)(((int)currentDirection + 1) % 4);
        Debug.Log($"Player facing: {currentDirection}");

        float targetAngle = directionAngles[(int)currentDirection];
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }

    private void Update()
    {
        if (isMoving && rb != null)
        {
            Vector3 moveDir = directionVectors[(int)currentDirection];
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.deltaTime);
        }
    }

    public void StartMoving()
    {
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    public void SetDirection(Direction newDirection)
    {
        currentDirection = newDirection;
        float targetAngle = directionAngles[(int)currentDirection];
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
    }
}
