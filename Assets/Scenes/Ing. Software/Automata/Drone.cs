using System.Collections;
using UnityEngine;

public class Drone : MonoBehaviour
{
    public Transform[] collectibles;   // cubitos
    public Transform[] depoPoints;     // collectibles[i] va a depoPoints[i]
    public Transform holdPoint;        // padre del cubo cargado
    public float speed = 3f;
    public float descendSpeed = 1.5f;
    public float arriveTolerance = 0.2f;

    public string LastError = "";      // vacío = el paso salió bien

    int current;
    bool holding;
    float hoverY;
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
        LastError = "";
        if (Finished) yield break;

        switch (cmd)
        {
            case CommandBlock.Cmd.IrACubo:
                if (holding) { LastError = "ya llevo un cubo."; break; }
                yield return GoTo(collectibles[current].position);
                break;

            case CommandBlock.Cmd.Recoger:
                if (holding) { LastError = "ya llevo un cubo."; break; }
                if (FlatDist(collectibles[current].position) >= arriveTolerance)
                { LastError = "no hay ningún cubo cerca."; break; }
                yield return PickUp();
                break;

            case CommandBlock.Cmd.IrADepo:
                if (!holding) { LastError = "no llevo ningún cubo."; break; }
                yield return GoTo(depoPoints[current].position);
                break;

            case CommandBlock.Cmd.Soltar:
                if (!holding) { LastError = "no llevo nada que soltar."; break; }
                if (FlatDist(depoPoints[current].position) >= arriveTolerance)
                { LastError = "no estoy sobre mi zona de entrega."; break; }
                yield return Release();
                break;
        }
    }

    // Acciones (aquí luego pones tus animaciones)

    IEnumerator PickUp()
    {
        Transform cube = collectibles[current];

        float dy = Top(cube) - Bottom(transform);
        yield return MoveY(transform.position.y + dy);

        // >>> animación de agarrar <<<
        cube.SetParent(holdPoint);
        holding = true;
        yield return new WaitForSeconds(0.2f);

        yield return MoveY(hoverY);
    }

    IEnumerator Release()
    {
        Transform cube = collectibles[current];
        Transform depo = depoPoints[current];

        float dy = Top(depo) - Bottom(cube);
        yield return MoveY(transform.position.y + dy);

        // animación de soltar
        cube.SetParent(null);
        cube.position = new Vector3(depo.position.x, cube.position.y, depo.position.z);
        holding = false;
        current++;
        yield return new WaitForSeconds(0.2f);

        yield return MoveY(hoverY);
    }

    // Movimiento 

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

    // Utilidades 
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
        LastError = "";
    }
}