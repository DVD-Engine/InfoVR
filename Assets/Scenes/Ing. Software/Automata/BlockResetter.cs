using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockResetter : MonoBehaviour
{
    public Transform blocksParent;   // "Botones"

    class Saved
    {
        public Transform t;
        public Rigidbody rb;
        public Transform parent;
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 worldScale;   // tamaño real en el mundo
    }

    readonly List<Saved> saved = new List<Saved>();

    void Awake()
    {
        foreach (var b in blocksParent.GetComponentsInChildren<CommandBlock>(true))
        {
            saved.Add(new Saved
            {
                t = b.transform,
                rb = b.GetComponent<Rigidbody>(),
                parent = b.transform.parent,
                localPos = b.transform.localPosition,
                localRot = b.transform.localRotation,
                worldScale = b.transform.lossyScale
            });
        }
    }

    // Conéctalo al botón LIMPIAR (Select Entered)
    public void ResetBlocks()
    {
        StopAllCoroutines();
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        // 1. Apagar el objeto: fuerza a XRI a soltarlo del socket
        foreach (var s in saved) s.t.gameObject.SetActive(false);
        yield return null;
        yield return null;

        // 2. Regresar cada bloque a su lugar original
        foreach (var s in saved)
        {
            s.t.SetParent(s.parent, false);
            s.t.localPosition = s.localPos;
            s.t.localRotation = s.localRot;
            ApplyScale(s);

            if (s.rb != null && !s.rb.isKinematic)
            {
                s.rb.linearVelocity = Vector3.zero;
                s.rb.angularVelocity = Vector3.zero;
            }
        }
        Physics.SyncTransforms();

        // 3. Encender de nuevo
        foreach (var s in saved) s.t.gameObject.SetActive(true);
    }

    // Mantiene el tamaño real de los bloques pase lo que pase
    void LateUpdate()
    {
        foreach (var s in saved)
            if (s.t.gameObject.activeSelf) ApplyScale(s);
    }

    static void ApplyScale(Saved s)
    {
        Vector3 p = s.t.parent != null ? s.t.parent.lossyScale : Vector3.one;
        s.t.localScale = new Vector3(
            s.worldScale.x / p.x,
            s.worldScale.y / p.y,
            s.worldScale.z / p.z);
    }
}