using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
// Si usas XRI 2.x, cambia la linea de arriba por: using UnityEngine.XR.Interaction.Toolkit;

/// Menu: Tools > Dron > Asistente de ensamblado
/// Este archivo debe estar en una carpeta llamada "Editor" (Assets/_Dron/Scripts/Editor).
public class DroneSetupWizard : EditorWindow
{
    public Transform kitRoot;
    public List<Transform> loosePieces = new List<Transform>();
    public float tableHeight = 0.9f;
    public float tableDistance = 1.0f;

    SerializedObject so;

    [MenuItem("Tools/Dron/Asistente de ensamblado")]
    static void Open() { GetWindow<DroneSetupWizard>("Asistente Dron"); }

    void OnEnable() { so = new SerializedObject(this); }

    void OnGUI()
    {
        so.Update();
        EditorGUILayout.HelpBox(
            "ANTES de usar esto: deja el dron ARMADO en la escena (todas las piezas en su posicion final).\n" +
            "Piezas sueltas: arrastralas aqui EN EL ORDEN en que el jugador debe colocarlas.\n" +
            "Todo lo que no pongas en la lista queda fijo en el dron.", MessageType.Info);
        EditorGUILayout.PropertyField(so.FindProperty("kitRoot"), new GUIContent("Raiz del kit (Drone_Kit_8_Parts)"));
        EditorGUILayout.PropertyField(so.FindProperty("loosePieces"), new GUIContent("Piezas sueltas (en orden)"), true);
        EditorGUILayout.PropertyField(so.FindProperty("tableHeight"), new GUIContent("Altura de la mesa (m)"));
        EditorGUILayout.PropertyField(so.FindProperty("tableDistance"), new GUIContent("Distancia al jugador (m)"));
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (GUILayout.Button("Configurar todo", GUILayout.Height(32)))
        {
            if (Validate()) Run();
        }
    }

    bool Validate()
    {
        string error = null;
        if (Application.isPlaying) error = "Sal del modo Play primero.";
        else if (kitRoot == null) error = "Falta la raiz del kit.";
        else if (loosePieces.Count == 0) error = "La lista de piezas sueltas esta vacia.";
        else
        {
            var seen = new HashSet<Transform>();
            foreach (var p in loosePieces)
            {
                if (p == null) { error = "Hay un elemento vacio en la lista."; break; }
                if (p.parent != kitRoot) { error = "'" + p.name + "' debe ser hijo directo de " + kitRoot.name + "."; break; }
                if (!seen.Add(p)) { error = "'" + p.name + "' esta repetido."; break; }
            }
        }
        if (error != null) EditorUtility.DisplayDialog("Asistente Dron", error, "OK");
        return error == null;
    }

    // ---------- utilidades ----------

    static bool IsProp(string n)
    {
        n = n.ToLowerInvariant();
        return n.Contains("helice") || n.Contains("hélice") || n.Contains("propeller");
    }

    static Bounds GetBounds(Transform t)
    {
        var r = t.GetComponentsInChildren<Renderer>();
        if (r.Length == 0) return new Bounds(t.position, Vector3.one * 0.05f);
        Bounds b = r[0].bounds;
        for (int i = 1; i < r.Length; i++) b.Encapsulate(r[i].bounds);
        return b;
    }

    /// Si el pivote no esta en el centro de la pieza, la envuelve en un objeto con el pivote centrado.
    static Transform Centered(Transform t)
    {
        if (t.GetComponentInChildren<Renderer>() == null) return t;
        Bounds b = GetBounds(t);
        if (Vector3.Distance(t.position, b.center) < 0.005f) return t;

        var holder = new GameObject(t.name + "_Pieza").transform;
        Undo.RegisterCreatedObjectUndo(holder.gameObject, "Asistente Dron");
        holder.SetParent(t.parent, false);
        holder.SetPositionAndRotation(b.center, Quaternion.identity);
        Undo.SetTransformParent(t, holder, "Asistente Dron");
        return holder;
    }

    // ---------- proceso principal ----------

    void Run()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        if (PrefabUtility.IsPartOfPrefabInstance(kitRoot))
            PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(kitRoot),
                PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        var loose = new List<Transform>(loosePieces);
        var fixedParts = new List<Transform>();
        foreach (Transform c in kitRoot)
            if (!loose.Contains(c)) fixedParts.Add(c);

        // 1. Pivotes centrados (sueltas y helices fijas)
        for (int i = 0; i < loose.Count; i++) loose[i] = Centered(loose[i]);
        for (int i = 0; i < fixedParts.Count; i++)
            if (IsProp(fixedParts[i].name)) fixedParts[i] = Centered(fixedParts[i]);

        // 2. Tamano del dron armado
        var all = new List<Transform>(loose);
        all.AddRange(fixedParts);
        Bounds total = GetBounds(all[0]);
        foreach (var t in all) total.Encapsulate(GetBounds(t));

        // 3. Dron_Root + piezas fijas + slots
        var root = new GameObject("Dron_Root").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Asistente Dron");
        root.position = total.center;
        foreach (var f in fixedParts) Undo.SetTransformParent(f, root, "Asistente Dron");

        var slotsParent = new GameObject("Slots").transform;
        Undo.RegisterCreatedObjectUndo(slotsParent.gameObject, "Asistente Dron");
        slotsParent.SetParent(root, false);

        var slots = new List<Transform>();
        for (int i = 0; i < loose.Count; i++)
        {
            var s = new GameObject("Slot_" + i + "_" + loose[i].name).transform;
            s.SetParent(slotsParent, false);
            s.SetPositionAndRotation(loose[i].position, loose[i].rotation);
            slots.Add(s);
        }

        // 4. Mesa
        var widths = new List<float>();
        float totalW = 0f;
        const float gap = 0.06f;
        foreach (var p in loose)
        {
            Bounds pb = GetBounds(p);
            float w = Mathf.Max(pb.size.x, pb.size.z, 0.05f);
            widths.Add(w);
            totalW += w;
        }
        totalW += gap * (loose.Count - 1);
        float tableW = Mathf.Max(1.8f, totalW + 0.2f);

        var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Mesa_Ensamble";
        Undo.RegisterCreatedObjectUndo(table, "Asistente Dron");
        table.transform.position = new Vector3(0f, tableHeight - 0.025f, tableDistance);
        table.transform.localScale = new Vector3(tableW, 0.05f, 1.0f);

        // 5. Piezas sueltas: collider, fisica, agarre y orden
        var piecesParent = new GameObject("Piezas_Sueltas").transform;
        Undo.RegisterCreatedObjectUndo(piecesParent.gameObject, "Asistente Dron");

        var dronePieces = new List<DronePiece>();
        float x = -totalW / 2f;
        int propStage = -1; // las helices comparten paso: cualquier helice va en cualquier lugar de helice
        for (int i = 0; i < loose.Count; i++)
        {
            var p = loose[i];
            Bounds b = GetBounds(p);

            var col = Undo.AddComponent<BoxCollider>(p.gameObject);
            Vector3 ws = Vector3.Max(b.size, Vector3.one * 0.03f);
            Vector3 ls = p.lossyScale;
            col.center = p.InverseTransformPoint(b.center);
            col.size = new Vector3(ws.x / Mathf.Abs(ls.x), ws.y / Mathf.Abs(ls.y), ws.z / Mathf.Abs(ls.z));

            Undo.SetTransformParent(p, piecesParent, "Asistente Dron");
            p.position = new Vector3(x + widths[i] / 2f, tableHeight + b.extents.y + 0.01f, tableDistance - 0.3f);
            x += widths[i] + gap;

            var dp = Undo.AddComponent<DronePiece>(p.gameObject); // agrega Rigidbody y XRGrabInteractable
            int stage = i;
            if (IsProp(p.name)) { if (propStage < 0) propStage = i; stage = propStage; }
            dp.orderIndex = stage;
            dronePieces.Add(dp);

            var rb = p.GetComponent<Rigidbody>();
            rb.mass = 0.2f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var grab = p.GetComponent<XRGrabInteractable>();
            if (grab != null) grab.throwOnDetach = false;

            EditorUtility.SetDirty(dp);
        }

        // 6. Dron armado al fondo de la mesa
        root.position = new Vector3(0f, tableHeight + total.extents.y + 0.02f, tableDistance + 0.25f);

        // 7. Waypoints (fuera del dron)
        var wpParent = new GameObject("Waypoints_Dron").transform;
        Undo.RegisterCreatedObjectUndo(wpParent.gameObject, "Asistente Dron");
        var wps = new List<Transform>();

        var first = new GameObject("WP_0_Despegue").transform;
        first.SetParent(wpParent, false);
        first.position = root.position + Vector3.up * 0.9f;
        wps.Add(first);

        Vector3 center = new Vector3(0f, 1.9f, tableDistance + 1.5f);
        const int n = 8;
        const float radius = 1.2f;
        for (int k = 0; k < n; k++)
        {
            float a = k * Mathf.PI * 2f / n;
            var wp = new GameObject("WP_" + (k + 1)).transform;
            wp.SetParent(wpParent, false);
            wp.position = center + new Vector3(Mathf.Cos(a) * radius, 0.25f * Mathf.Sin(2f * a), Mathf.Sin(a) * radius);
            wps.Add(wp);
        }

        // 8. Vuelo
        var flight = Undo.AddComponent<DroneFlight>(root.gameObject);
        flight.waypoints = wps.ToArray();
        var props = new List<Transform>();
        foreach (var p in loose) if (IsProp(p.name)) props.Add(p);
        foreach (var f in fixedParts) if (IsProp(f.name)) props.Add(f);
        flight.propellers = props.ToArray();
        EditorUtility.SetDirty(flight);

        // 9. Controlador de ensamblado
        var asmGo = new GameObject("DroneAssembly");
        Undo.RegisterCreatedObjectUndo(asmGo, "Asistente Dron");
        var audio = asmGo.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        var asm = asmGo.AddComponent<DroneAssembly>();
        asm.pieces = dronePieces.ToArray();
        asm.slots = slots.ToArray();
        asm.droneRoot = root;
        asm.flight = flight;
        asm.audioSource = audio;
        EditorUtility.SetDirty(asm);

        // 10. Limpieza
        if (kitRoot.childCount == 0) Undo.DestroyObjectImmediate(kitRoot.gameObject);

        Selection.activeGameObject = root.gameObject;
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Asistente Dron",
            "Listo. " + loose.Count + " piezas configuradas.\nPrueba en Play. Si algo sale mal, Ctrl+Z deshace todo.", "OK");
    }
}
