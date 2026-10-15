using System.Collections;
using UnityEngine;

public class Drone : MonoBehaviour
{
    public Transform[] collectibles;   // cubitos
    public Transform[] depoPoints;     // collectibles[i] va a depoPoints[i]
    public Transform holdPoint;        // solo se usa como padre del cubo cargado
    public float speed = 3f;
    public float descendSpeed = 1.5f;
    public float arriveTolerance = 0.2f;   // margen para considerar que está "encima"

    int current;
    bool holding;
    float hoverY;                      // altura de vuelo = altura inicial del dron
    Vector3 startPos;
    Quaternion startRot;
    Vector3[] cubeStart;

    public bool Finished => current >= collectibles.Length;

    void Awake()
    {
        startPos = transform.position;
        startRot = transform.rotation;
        hoverY = startPos.y;
        cubeStart = new Vector3[collectibles.Length];
        for (int i = 0; i < collectibles.Length; i++)
            cubeStart[i] = collectibles[i].position;
    }

    public IEnumerator Do(CommandBlock.Cmd cmd)
    {
        if (Finished) yield break;

        switch (cmd)
        {
            case CommandBlock.Cmd.IrACubo:
                if (!holding) yield return GoTo(collectibles[current].position);
                break;

            case CommandBlock.Cmd.Recoger:
                if (!holding && FlatDist(collectibles[current].position) < arriveTolerance)
                    yield return PickUp();
                else
                    yield return new WaitForSeconds(0.4f);
                break;

            case CommandBlock.Cmd.IrADepo:
                if (holding) yield return GoTo(depoPoints[current].position);
                break;

            case CommandBlock.Cmd.Soltar:
                if (holding && FlatDist(depoPoints[current].position) < arriveTolerance)
                    yield return Release();
                else
                    yield return new WaitForSeconds(0.4f);
                break;
        }
    }

    // ---------- Acciones (aquí luego pones tus animaciones) ----------

    IEnumerator PickUp()
    {
        Transform cube = collectibles[current];

        // bajar hasta que la base del dron toque la parte de arriba del cubo
        float dy = Top(cube) - Bottom(transform);
        yield return MoveY(transform.position.y + dy);

        // >>> aquí iría la animación de agarrar <<<
        cube.SetParent(holdPoint);       // conserva su posición actual: queda pegado a la base
        holding = true;
        yield return new WaitForSeconds(0.2f);

        yield return MoveY(hoverY);      // subir
    }

    IEnumerator Release()
    {
        Transform cube = collectibles[current];
        Transform depo = depoPoints[current];

        // bajar hasta que la base del cubo toque la parte de arriba del depo
        float dy = Top(depo) - Bottom(cube);
        yield return MoveY(transform.position.y + dy);

        // >>> aquí iría la animación de soltar <<<
        cube.SetParent(null);
        cube.position = new Vector3(depo.position.x, cube.position.y, depo.position.z);
        holding = false;
        current++;
        yield return new WaitForSeconds(0.2f);

        yield return MoveY(hoverY);      // subir
    }

    // ---------- Movimiento ----------

    IEnumerator GoTo(Vector3 target)
    {
        while (FlatDist(target) > 0.02f)
        {
            Vector3 flat = new Vector3(target.x, hoverY, target.z);
            if (FlatDist(target) > 0.1f) transform.LookAt(flat);
            transform.position = Vector3.MoveTowards(transform.position, flat, speed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator MoveY(float y)
    {
        while (Mathf.Abs(transform.position.y - y) > 0.001f)
        {
            Vector3 p = transform.position;
            p.y = Mathf.MoveTowards(p.y, y, descendSpeed * Time.deltaTime);
            transform.position = p;
            yield return null;
        }
    }

    // ---------- Utilidades ----------

    float FlatDist(Vector3 p)
    {
        p.y = transform.position.y;
        return Vector3.Distance(transform.position, p);
    }

    static Bounds GetBounds(Transform t)
    {
        var r = t.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds : new Bounds(t.position, Vector3.zero);
    }

    static float Top(Transform t)    => GetBounds(t).max.y;
    static float Bottom(Transform t) => GetBounds(t).min.y;

    public void ResetDrone()
    {
        StopAllCoroutines();
        transform.SetPositionAndRotation(startPos, startRot);
        for (int i = 0; i < collectibles.Length; i++)
        {
            collectibles[i].SetParent(null);
            collectibles[i].position = cubeStart[i];
        }
        current = 0;
        holding = false;
    }
}