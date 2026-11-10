using System.Collections;
using UnityEngine;

/// Va en el objeto raiz del dron (sin Rigidbody).
/// IMPORTANTE: los waypoints NO deben ser hijos del dron.
///
/// Giro de helices: si una helice tiene un hijo llamado "Hub" (se crea con
/// Tools > Dron > Arreglar giro de aspas), gira alrededor de ese punto (el centro del motor).
/// Si no lo tiene, gira sobre su propio pivote como antes.
public class DroneFlight : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 1.5f;
    public float turnSpeed = 3f;
    public float tolerance = 0.1f;
    public bool loop = true;
    public float startDelay = 1f;

    [Header("Helices (se asignan al armar; se giran en su eje Y local)")]
    public Transform[] propellers;
    public float propSpeed = 1800f;        // grados por segundo
    public Vector3 propAxis = Vector3.up;  // eje local de giro; si giran "de lado", prueba (0,0,1) o (1,0,0)
    public bool alternateDirection = true; // helices vecinas giran en sentido contrario
    public float spinUpTime = 1.5f;

    bool flying;
    float spin;

    // Estado inicial (para resetear)
    Vector3 startPos;
    Quaternion startRot;
    Vector3[] propLocalPos;
    Quaternion[] propLocalRot;
    Transform[] hubs;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (waypoints != null)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.05f);
                int next = (i + 1) % waypoints.Length;
                if (waypoints[next] != null) Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
            }
        }

        // Centro de giro de cada helice
        Gizmos.color = Color.yellow;
        if (propellers != null)
        {
            foreach (var p in propellers)
            {
                if (p == null) continue;
                var hub = p.Find("Hub");
                if (hub != null) Gizmos.DrawWireSphere(hub.position, 0.02f);
            }
        }
    }

    public void StartFlight()
    {
        if (flying) return;

        // Guardar estado de las helices antes de girarlas (para el reset) y buscar su "Hub"
        int n = propellers.Length;
        propLocalPos = new Vector3[n];
        propLocalRot = new Quaternion[n];
        hubs = new Transform[n];
        for (int i = 0; i < n; i++)
        {
            var p = propellers[i];
            if (p == null) continue;
            propLocalPos[i] = p.localPosition;
            propLocalRot[i] = p.localRotation;
            hubs[i] = p.Find("Hub");
        }

        StartCoroutine(Fly());
    }

    /// Detiene el vuelo y regresa el dron (y las helices fijas) a su estado inicial.
    public void ResetFlight()
    {
        StopAllCoroutines();
        flying = false;
        spin = 0f;

        transform.SetPositionAndRotation(startPos, startRot);

        if (propLocalRot != null)
        {
            for (int i = 0; i < propellers.Length && i < propLocalRot.Length; i++)
            {
                if (propellers[i] == null) continue;
                propellers[i].localPosition = propLocalPos[i];
                propellers[i].localRotation = propLocalRot[i];
            }
        }
        propLocalPos = null;
        propLocalRot = null;
        hubs = null;
    }

    void Update()
    {
        if (!flying || hubs == null) return;
        spin = Mathf.MoveTowards(spin, propSpeed, propSpeed / spinUpTime * Time.deltaTime);
        for (int i = 0; i < propellers.Length; i++)
        {
            var p = propellers[i];
            if (p == null) continue;
            float angle = ((alternateDirection && i % 2 == 1) ? -1f : 1f) * spin * Time.deltaTime;

            if (hubs[i] != null)
                p.RotateAround(hubs[i].position, p.TransformDirection(propAxis), angle);   // sobre el motor
            else
                p.Rotate(propAxis, angle, Space.Self);                                      // sobre su pivote
        }
    }

    IEnumerator Fly()
    {
        flying = true;
        yield return new WaitForSeconds(startDelay);

        do
        {
            foreach (var wp in waypoints)
            {
                while (Vector3.Distance(transform.position, wp.position) > tolerance)
                {
                    transform.position = Vector3.MoveTowards(transform.position, wp.position, speed * Time.deltaTime);

                    Vector3 dir = wp.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                    {
                        Quaternion target = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
                    }
                    yield return null;
                }
            }
        } while (loop);

        // Termina el recorrido: las helices se detienen
        while (spin > 1f)
        {
            spin = Mathf.MoveTowards(spin, 0f, propSpeed / spinUpTime * Time.deltaTime);
            yield return null;
        }
        flying = false;
    }
}
