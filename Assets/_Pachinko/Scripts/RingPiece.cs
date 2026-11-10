using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Anillo agarrable. Solo se puede agarrar si es el de arriba de su poste.
public class RingPiece : XRGrabInteractable
{
    [HideInInspector] public int colorId;
    [HideInInspector] public PoleStack pole;
    [HideInInspector] public bool busy;
    [HideInInspector] public ColorStackGame game;

    protected override void Awake()
    {
        // Asegura un collider para poder agarrarlo
        if (GetComponentInChildren<Collider>() == null)
        {
            var mf = GetComponentInChildren<MeshFilter>();
            if (mf != null) mf.gameObject.AddComponent<BoxCollider>();
            else gameObject.AddComponent<SphereCollider>();
        }

        base.Awake();

        movementType = MovementType.Instantaneous;
        throwOnDetach = false;
        trackRotation = false; // se mantiene derecho

        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
    }

    public void Init(ColorStackGame g, int id, Color c)
    {
        game = g;
        colorId = id;
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", c); // URP
        block.SetColor("_Color", c);     // Built-in
        foreach (var r in GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(block);
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        return !busy && pole != null && pole.Top == this && base.IsSelectableBy(interactor);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        if (!isSelected && game != null) game.Drop(this);
    }
}
