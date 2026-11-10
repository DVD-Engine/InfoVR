using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Menu: Tools > Dron > Arreglar giro de aspas
/// Este archivo debe estar en una carpeta llamada "Editor" (la misma donde esta DroneSetupWizard).
///
/// Para cada helice del DroneFlight:
///  1. Activa Read/Write en el modelo de su malla (hace falta para medirla).
///  2. Calcula el centro del eje (el cubo) y crea un hijo vacio llamado "Hub" ahi.
/// En juego, DroneFlight gira cada helice alrededor de su "Hub" (el centro del motor).
public static class DroneHubTool
{
    [MenuItem("Tools/Dron/Arreglar giro de aspas")]
    static void Run()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Giro de aspas", "Sal del modo Play primero.", "OK");
            return;
        }

#if UNITY_2023_1_OR_NEWER
        var flight = Object.FindFirstObjectByType<DroneFlight>();
#else
        var flight = Object.FindObjectOfType<DroneFlight>();
#endif
        if (flight == null || flight.propellers == null || flight.propellers.Length == 0)
        {
            EditorUtility.DisplayDialog("Giro de aspas",
                "No encontre el DroneFlight (en Dron_Root) con helices asignadas.", "OK");
            return;
        }

        // 1) Activar Read/Write en los modelos de las helices
        var done = new System.Collections.Generic.HashSet<string>();
        foreach (var prop in flight.propellers)
        {
            if (prop == null) continue;
            foreach (var mf in prop.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || mesh.isReadable) continue;

                string path = AssetDatabase.GetAssetPath(mesh);
                if (string.IsNullOrEmpty(path) || !done.Add(path)) continue;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[DroneHubTool] No pude activar Read/Write de '" + mesh.name + "' (" + path + ").");
                    continue;
                }
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log("[DroneHubTool] Read/Write activado en " + path);
            }
        }

        // 2) Crear/actualizar el "Hub" de cada helice
        int ok = 0, failed = 0;
        foreach (var prop in flight.propellers)
        {
            if (prop == null) continue;

            Vector3 sum = Vector3.zero;
            long count = 0;
            foreach (var mf in prop.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null || !mesh.isReadable) continue;
                var verts = mesh.vertices;
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < verts.Length; i++) sum += m.MultiplyPoint3x4(verts[i]);
                count += verts.Length;
            }

            if (count == 0)
            {
                Debug.LogWarning("[DroneHubTool] No pude medir '" + prop.name + "'. Crea a mano un hijo vacio llamado 'Hub' en el centro del motor.", prop);
                failed++;
                continue;
            }

            Transform hub = prop.Find("Hub");
            if (hub == null)
            {
                var go = new GameObject("Hub");
                Undo.RegisterCreatedObjectUndo(go, "Arreglar giro de aspas");
                hub = go.transform;
                hub.SetParent(prop, false);
            }
            else
            {
                Undo.RecordObject(hub, "Arreglar giro de aspas");
            }
            hub.position = sum / count;
            hub.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(hub);
            ok++;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Giro de aspas",
            "Listo: " + ok + " helice(s) con su 'Hub'." + (failed > 0 ? "\n" + failed + " fallaron (revisa la Consola)." : "") +
            "\n\nSelecciona Dron_Root para ver los centros de giro (esferas amarillas).", "OK");
    }
}
