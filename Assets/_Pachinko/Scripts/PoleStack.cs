using System.Collections.Generic;
using UnityEngine;

// Ponlo en un GameObject vacío ubicado en la BASE de cada poste.
// El eje "arriba" (flecha verde) del objeto debe apuntar hacia arriba del poste.
public class PoleStack : MonoBehaviour
{
    public int capacity = 6;          // cuántos anillos caben
    public float ringHeight = 0.05f;  // grosor de un anillo en metros (ya escalado)

    [HideInInspector] public List<RingPiece> rings = new List<RingPiece>();

    public RingPiece Top => rings.Count > 0 ? rings[rings.Count - 1] : null;
    public bool IsFull => rings.Count >= capacity;

    public Vector3 SlotPosition(int index) =>
        transform.position + transform.up * (ringHeight * (index + 0.5f));

    public bool Accepts(RingPiece r, bool strict)
    {
        if (IsFull) return false;
        if (!strict || rings.Count == 0) return true;
        return Top.colorId == r.colorId;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < capacity; i++)
            Gizmos.DrawWireSphere(SlotPosition(i), ringHeight * 0.4f);
    }
}
