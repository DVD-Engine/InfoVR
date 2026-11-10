using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// Ponlo en el objeto PADRE de todos los PoleStack (o en cualquier objeto, y llena la lista a mano).
public class ColorStackGame : MonoBehaviour
{
    [Header("Anillo")]
    public GameObject ringPrefab;            // tu modelo (decimado). Si está vacío usa un cilindro de prueba
    public int ringsPerColor = 4;

    [Header("Reglas")]
    public bool strictRules = false;         // true = solo sobre vacío o mismo color
    public float snapRadius = 0.25f;         // distancia máxima al poste al soltar

    [Header("Colores (uno por tipo de anillo)")]
    public Color[] colors =
    {
        Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green,
        Color.cyan, Color.blue, new Color(0.6f, 0.1f, 1f), Color.magenta
    };

    public List<PoleStack> poles = new List<PoleStack>();
    public UnityEvent onWin;

    readonly List<RingPiece> all = new List<RingPiece>();

    void Start()
    {
        if (poles.Count == 0)
            poles = GetComponentsInChildren<PoleStack>().OrderBy(p => p.transform.position.x).ToList();
        NewGame();
    }

    [ContextMenu("New Game")]
    public void NewGame()
    {
        foreach (var r in all) if (r != null) Destroy(r.gameObject);
        all.Clear();
        foreach (var p in poles) p.rings.Clear();

        var ids = new List<int>();
        for (int c = 0; c < colors.Length; c++)
            for (int i = 0; i < ringsPerColor; i++) ids.Add(c);

        do { ids = ids.OrderBy(_ => Random.value).ToList(); } while (SolvedIds(ids));

        int k = 0;
        foreach (var p in poles)
        {
            for (int i = 0; i < ringsPerColor && k < ids.Count; i++, k++)
            {
                var ring = Spawn(ids[k]);
                ring.pole = p;
                p.rings.Add(ring);
                ring.transform.SetPositionAndRotation(p.SlotPosition(i), p.transform.rotation);
                all.Add(ring);
            }
        }
    }

    bool SolvedIds(List<int> ids)
    {
        for (int i = 0; i < ids.Count; i++)
            if (ids[i] != ids[(i / ringsPerColor) * ringsPerColor]) return false;
        return true;
    }

    RingPiece Spawn(int colorId)
    {
        GameObject go;
        if (ringPrefab != null) go = Instantiate(ringPrefab);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
        }
        var ring = go.GetComponent<RingPiece>();
        if (ring == null) ring = go.AddComponent<RingPiece>();
        ring.Init(this, colorId, colors[colorId]);
        return ring;
    }

    public void Drop(RingPiece r)
    {
        PoleStack best = null;
        float bestDist = snapRadius;
        foreach (var p in poles)
        {
            Vector3 d = Vector3.ProjectOnPlane(r.transform.position - p.transform.position, p.transform.up);
            float dist = d.magnitude;
            if (dist < bestDist && (p == r.pole || p.Accepts(r, strictRules)))
            {
                bestDist = dist;
                best = p;
            }
        }
        if (best == null) best = r.pole;

        if (best != r.pole)
        {
            r.pole.rings.Remove(r);
            best.rings.Add(r);
            r.pole = best;
        }
        StartCoroutine(Snap(r));
    }

    IEnumerator Snap(RingPiece r)
    {
        r.busy = true;
        Vector3 from = r.transform.position;
        Vector3 to = r.pole.SlotPosition(r.pole.rings.IndexOf(r));
        Quaternion rot = r.pole.transform.rotation;
        for (float t = 0; t < 1f; t += Time.deltaTime / 0.15f)
        {
            r.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), Quaternion.Slerp(r.transform.rotation, rot, t));
            yield return null;
        }
        r.transform.SetPositionAndRotation(to, rot);
        r.busy = false;

        if (IsSolved()) { Debug.Log("¡Ganaste!"); onWin?.Invoke(); }
    }

    public bool IsSolved()
    {
        foreach (var p in poles)
        {
            if (p.rings.Count == 0) continue;
            if (p.rings.Count != ringsPerColor) return false;
            int c = p.rings[0].colorId;
            if (p.rings.Any(x => x.colorId != c)) return false;
        }
        return true;
    }
}
