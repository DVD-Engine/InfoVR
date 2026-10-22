using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ReadyBtn : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable xrInteractable;

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
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }
}
