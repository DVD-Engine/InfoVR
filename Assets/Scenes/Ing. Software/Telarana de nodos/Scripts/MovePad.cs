using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MovePad : MonoBehaviour
{
    [Header("Pad Settings")]
    [SerializeField] private int padNumber = 1;
    [SerializeField] private float rotationSpeed = 720f;

    private Direction currentDirection = Direction.Up;
    private bool isRotating = false;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable xrInteractable;

    // Z-axis rotation for each direction (matches visual pointing)
    private static readonly float[] directionAngles = { 0f, 270f, 180f, 90f };

    public Direction CurrentDirection => currentDirection;

    private void Awake()
    {
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
        if (!isRotating && GameManager.Instance != null && GameManager.Instance.IsSetupPhase)
            CycleDirection();
    }

    private void OnMouseDown()
    {
        if (!isRotating && GameManager.Instance != null && GameManager.Instance.IsSetupPhase)
        {
            CycleDirection();
        }
    }

    private void CycleDirection()
    {
        currentDirection = (Direction)(((int)currentDirection + 1) % 4);
        Debug.Log($"Movepad facing: {currentDirection}");

        float targetAngle = directionAngles[(int)currentDirection];
        StartCoroutine(RotateTo(targetAngle));
    }

    private IEnumerator RotateTo(float targetAngleZ)
    {
        isRotating = true;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngleZ);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;
        isRotating = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.collider.GetComponent<Player>();
        if (player != null && GameManager.Instance != null)
        {
            GameManager.Instance.OnPadTouched(padNumber, currentDirection);
        }
    }
}
