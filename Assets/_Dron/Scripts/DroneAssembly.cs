using System;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Controla el armado.
/// - pieces[i] y slots[i] van emparejados: el slot i esta en el lugar donde termina la pieza i.
/// Reglas:
///  - ASPAS / HELICES: en cualquier momento, cada una en SU PROPIO motor (por eso van de colores).
///  - COMPONENTES internos: en cualquier orden. Los del mismo tipo (Circuito_1 / Circuito_2) son intercambiables.
///  - CARCASA: solo se puede poner al final, cuando ya estan todas las piezas internas
///    (las aspas NO cuentan: pueden ir antes o despues).
/// RESET: ResetAll() devuelve todo a su estado inicial. Tecla de atajo: F8.
public class DroneAssembly : MonoBehaviour
{
    public DronePiece[] pieces;
    public Transform[] slots;
    public Transform droneRoot;
    public float snapDistance = 0.12f;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeClip;

    [Header("Al completar")]
    public DroneFlight flight;             // Se inicia automaticamente
    public UnityEvent onCompleted;

    [Header("Reset")]
    public UnityEvent onReset;

    bool[] slotTaken;
    bool completed;

    void Start()
    {
        slotTaken = new bool[slots.Length];
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) ResetAll();
#endif

        if (completed || slotTaken == null) return;

        bool innerDone = InnerPiecesDone();

        for (int pi = 0; pi < pieces.Length; pi++)
        {
            var piece = pieces[pi];
            if (piece == null || piece.placed || piece.IsHeld) continue;

            Vector3 pos = piece.transform.position;
            float radius = snapDistance * piece.SnapMultiplier;

            int good = -1;
            if (piece.IsPropeller)
            {
                // aspa: cualquier momento, pero en SU motor
                good = OwnSlotIfNear(pi, pos, radius);
            }
            else if (piece.IsCasing)
            {
                // la carcasa solo al final
                if (innerDone) good = NearestFreeSlot(pos, radius, p => p.IsCasing);
            }
            else
            {
                // componente interno: cualquier orden; slot libre de su mismo tipo
                string key = piece.GroupKey;
                good = NearestFreeSlot(pos, radius, p => !p.IsPropeller && !p.IsCasing && p.GroupKey == key);
            }

            if (good >= 0)
            {
                slotTaken[good] = true;
                piece.Lock(slots[good], droneRoot);
                Play(correctClip);
                if (AllPlaced()) Complete();
                continue;
            }

            // Cerca de algun slot libre pero no es el correcto / no es el momento: rechazar.
            int any = NearestFreeSlot(pos, radius, null);
            if (any >= 0 && piece.Reject())
            {
                string why = piece.IsCasing && !innerDone ? "la carcasa va al final"
                           : piece.IsPropeller ? "esa aspa va en otro motor"
                           : "ese slot es de otra pieza";
                Debug.Log("[DroneAssembly] Rechazada '" + piece.name + "' cerca de '" + slots[any].name
                          + "' (" + why + "). Radio " + radius.ToString("F2") + " m.", piece);
                Play(wrongClip);
            }
        }
    }

    /// Resetea todo el ejercicio: piezas a la mesa, dron a su lugar inicial y vuelo detenido.
    public void ResetAll()
    {
        completed = false;
        if (slotTaken != null) Array.Clear(slotTaken, 0, slotTaken.Length);

        if (flight != null) flight.ResetFlight();
        foreach (var p in pieces)
            if (p != null) p.ResetPiece();

        onReset?.Invoke();
        Debug.Log("[DroneAssembly] Ejercicio reiniciado.");
    }

    // Todas las piezas internas (ni aspas ni carcasa) ya colocadas
    bool InnerPiecesDone()
    {
        foreach (var p in pieces)
            if (p != null && !p.placed && !p.IsPropeller && !p.IsCasing) return false;
        return true;
    }

    bool AllPlaced()
    {
        foreach (var p in pieces)
            if (p != null && !p.placed) return false;
        return true;
    }

    int OwnSlotIfNear(int index, Vector3 pos, float radius)
    {
        if (index >= slots.Length || slotTaken[index] || slots[index] == null) return -1;
        return Vector3.Distance(pos, slots[index].position) <= radius ? index : -1;
    }

    /// Slot libre mas cercano dentro de 'radius'. 'filter' (opcional) decide que slots cuentan,
    /// segun la pieza que le corresponde a ese slot.
    int NearestFreeSlot(Vector3 pos, float radius, Func<DronePiece, bool> filter)
    {
        int best = -1;
        float bestDist = radius;
        int n = Mathf.Min(slots.Length, pieces.Length);
        for (int i = 0; i < n; i++)
        {
            if (slotTaken[i]) continue;
            if (filter != null && (pieces[i] == null || !filter(pieces[i]))) continue;
            float d = Vector3.Distance(pos, slots[i].position);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    void Complete()
    {
        completed = true;
        Play(completeClip);
        onCompleted?.Invoke();
        if (flight != null) flight.StartFlight();
    }

    void Play(AudioClip c) { if (audioSource != null && c != null) audioSource.PlayOneShot(c); }
}
