// RadioSlot.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public class RadioSlot : MonoBehaviour
{
    public ShapeType expected;

    XRSocketInteractor socket;
    Collider[] lockedColliders;

    void Awake() => socket = GetComponent<XRSocketInteractor>();

    public bool IsFilled => socket.hasSelection;

    public bool IsCorrect
    {
        get
        {
            if (!socket.hasSelection) return false;
            var piece = socket.firstInteractableSelected.transform.GetComponent<RadioComponent>();
            return piece != null && piece.shape == expected;
        }
    }

    // Keeps the piece seated but stops the player from grabbing it
    public void Lock()
    {
        if (!socket.hasSelection) return;
        lockedColliders = socket.firstInteractableSelected.transform.GetComponentsInChildren<Collider>();
        foreach (var c in lockedColliders) c.enabled = false;
    }

    public void Unlock()
    {
        if (lockedColliders == null) return;
        foreach (var c in lockedColliders) if (c != null) c.enabled = true;
        lockedColliders = null;
    }
}