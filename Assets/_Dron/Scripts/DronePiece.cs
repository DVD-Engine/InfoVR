using System.Collections;
using UnityEngine;
// XRI 3.x / Unity 6:
using UnityEngine.XR.Interaction.Toolkit.Interactables;
// Si usas XRI 2.x, cambia la linea de arriba por:
// using UnityEngine.XR.Interaction.Toolkit;

/// Pieza del dron. Requiere Rigidbody + Collider + XRGrabInteractable.
[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class DronePiece : MonoBehaviour
{
    [Tooltip("Ya no se usa (el orden ahora lo decide DroneAssembly). Se conserva para no romper la escena.")]
    public int orderIndex;

    [Tooltip("Que tan generoso es el acople para esta pieza. 0 = automatico (x2 helices, x1.5 el resto).")]
    public float snapRadiusMultiplier = 0f;

    [Tooltip("Marca esto en la carcasa. Tambien se detecta por nombre: carcasa, cubierta, tapa, cover, shell, casing, housing.")]
    public bool isCasing;

    [HideInInspector] public bool placed;

    Rigidbody rb;
    XRGrabInteractable grab;
    Renderer[] rends;
    Collider[] cols;
    Color[] originalColors;

    // Estado inicial (para resetear)
    Vector3 startPos;
    Quaternion startRot;
    Transform startParent;
    bool origKinematic, origGravity, origDetect;
    RigidbodyInterpolation origInterp;
    CollisionDetectionMode origCollision;

    bool rejecting;

    public bool IsHeld => grab != null && grab.isSelected;

    /// True si es una helice/aspa (se detecta por nombre).
    public bool IsPropeller
    {
        get
        {
            string n = name.ToLowerInvariant();
            return n.Contains("helice") || n.Contains("hélice") || n.Contains("propeller") || n.Contains("aspa");
        }
    }

    /// True si es la carcasa: solo se puede poner al final de las piezas internas.
    public bool IsCasing
    {
        get
        {
            if (isCasing) return true;
            string n = name.ToLowerInvariant();
            return n.Contains("carcasa") || n.Contains("cubierta") || n.Contains("tapa")
                || n.Contains("cover") || n.Contains("shell") || n.Contains("casing") || n.Contains("housing");
        }
    }

    public float SnapMultiplier
    {
        get
        {
            if (snapRadiusMultiplier > 0f) return snapRadiusMultiplier;
            return IsPropeller ? 2f : 1.5f;
        }
    }

    /// Identifica el "tipo" de pieza quitando numeros y simbolos del nombre:
    /// Circuito_1_Pieza y Circuito_2_Pieza comparten tipo, asi que son intercambiables.
    public string GroupKey
    {
        get
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in name.ToLowerInvariant())
                if (char.IsLetter(c)) sb.Append(c);
            return sb.ToString();
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        cols = GetComponentsInChildren<Collider>();
        rends = GetComponentsInChildren<Renderer>();
        originalColors = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++) originalColors[i] = rends[i].material.color;

        startPos = transform.position;
        startRot = transform.rotation;
        startParent = transform.parent;

        origKinematic = rb.isKinematic;
        origGravity = rb.useGravity;
        origDetect = rb.detectCollisions;
        origInterp = rb.interpolation;
        origCollision = rb.collisionDetectionMode;
    }

    // ---------- rechazo ----------

    /// Devuelve true solo si inicio un rechazo nuevo (para que el sonido suene una vez).
    public bool Reject()
    {
        // DroneAssembly llama a Reject() en cada frame mientras la pieza sigue cerca de un slot.
        if (rejecting || placed) return false;
        StartCoroutine(RejectRoutine());
        return true;
    }

    IEnumerator RejectRoutine()
    {
        rejecting = true;
        Tint(Color.red);
        yield return new WaitForSeconds(0.4f);
        Restore();

        // Si el jugador la volvio a agarrar durante el rechazo, no se la quitamos de la mano.
        if (!IsHeld)
        {
            // Teletransporte "limpio": kinematica un instante, mover, sincronizar fisica y soltarla.
            StopMotion();
            rb.isKinematic = true;
            transform.SetPositionAndRotation(startPos, startRot);
            rb.position = startPos;
            rb.rotation = startRot;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();

            if (!IsHeld)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.WakeUp();
            }
        }

        rejecting = false;
        EnsureGrabbable();
    }

    /// Si algo dejo la pieza sin poder agarrarse, lo corrige y avisa en la Consola que era.
    void EnsureGrabbable()
    {
        if (placed) return;

        if (grab != null && !grab.enabled)
        {
            Debug.LogWarning("[DronePiece] " + name + ": el XRGrabInteractable estaba DESACTIVADO. Lo reactive.", this);
            grab.enabled = true;
        }
        if (rb != null && rb.isKinematic && !IsHeld)
        {
            Debug.LogWarning("[DronePiece] " + name + ": estaba Kinematic sin que la sostuvieran. La devolvi a fisica normal.", this);
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        if (rb != null && !rb.detectCollisions)
        {
            Debug.LogWarning("[DronePiece] " + name + ": Detect Collisions estaba apagado. Lo reactive.", this);
            rb.detectCollisions = true;
        }
        if (cols != null)
        {
            foreach (var c in cols)
            {
                if (c != null && !c.enabled)
                {
                    Debug.LogWarning("[DronePiece] " + name + ": el collider '" + c.name + "' estaba desactivado. Lo reactive.", this);
                    c.enabled = true;
                }
            }
        }
    }

    // ---------- acople ----------

    public void Lock(Transform slot, Transform droneRoot, float duration = 0.3f)
    {
        placed = true;

        // Frenar antes de volverla kinematica (StopMotion no hace nada si ya es kinematica)
        StopMotion();

        // Que el sistema XR ya no pueda tocarla
        grab.enabled = false;

        // SOLDADURA: la pieza deja de ser un cuerpo fisico y pasa a ser un hijo mas del dron.
        // Interpolate en una pieza hija de un objeto movido por transform la hace ir con lag.
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.detectCollisions = false;

        transform.SetParent(droneRoot, true);
        StartCoroutine(SnapRoutine(slot, duration));
    }

    // Red de seguridad por frame
    void LateUpdate()
    {
        if (rb == null) return;
        if (!placed)
        {
            if (!rejecting) EnsureGrabbable();
            return;
        }
        if (!rb.isKinematic || rb.useGravity || rb.interpolation != RigidbodyInterpolation.None)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
        }
    }

    IEnumerator SnapRoutine(Transform slot, float duration)
    {
        Vector3 p0 = transform.position;
        Quaternion r0 = transform.rotation;
        for (float t = 0; t < 1f; t += Time.deltaTime / duration)
        {
            transform.SetPositionAndRotation(
                Vector3.Lerp(p0, slot.position, t),
                Quaternion.Slerp(r0, slot.rotation, t));
            yield return null;
        }
        transform.SetPositionAndRotation(slot.position, slot.rotation);
        Tint(Color.green);
        yield return new WaitForSeconds(0.3f);
        Restore();
    }

    // ---------- reset ----------

    /// Devuelve la pieza a la mesa, suelta y lista para volver a armarse.
    public void ResetPiece()
    {
        StopAllCoroutines();
        rejecting = false;
        placed = false;

        if (grab != null)
        {
            // Si el jugador la tiene en la mano, soltarla
            if (grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            grab.enabled = true;
        }

        Restore();

        // Mover con la fisica "congelada" y luego devolver los ajustes originales
        rb.isKinematic = true;
        transform.SetParent(startParent, true);
        transform.SetPositionAndRotation(startPos, startRot);
        rb.position = startPos;
        rb.rotation = startRot;

        rb.isKinematic = origKinematic;
        rb.useGravity = origGravity;
        rb.interpolation = origInterp;
        rb.collisionDetectionMode = origCollision;
        rb.detectCollisions = origDetect;
        StopMotion();
        Physics.SyncTransforms();
        rb.WakeUp();
    }

    // ---------- utilidades ----------

    void StopMotion()
    {
        if (rb.isKinematic) return;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    void Tint(Color c) { foreach (var r in rends) r.material.color = c; }
    void Restore() { for (int i = 0; i < rends.Length; i++) rends[i].material.color = originalColors[i]; }
}
