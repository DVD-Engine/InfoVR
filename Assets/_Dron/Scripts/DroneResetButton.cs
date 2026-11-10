using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// XRI 3.x / Unity 6:
using UnityEngine.XR.Interaction.Toolkit.Interactables;
// Si usas XRI 2.x, borra la linea de arriba (ya basta con la de "Toolkit").
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// Boton fisico de reset. Ponlo en un objeto 3D (por ejemplo un cubo achatado).
/// Se activa de 3 formas:
///  - En VR: seleccionarlo (agarre) o activarlo (gatillo) con rayo o con la mano cerca.
///  - En el simulador / editor: clic de mouse sobre el boton (con el cursor libre, sin mantener
///    las teclas del simulador).
[DefaultExecutionOrder(-100)]   // corre antes que el XRSimpleInteractable, para que ya tenga su collider
[RequireComponent(typeof(XRSimpleInteractable))]
public class DroneResetButton : MonoBehaviour
{
    public DroneAssembly assembly;                       // si esta vacio, lo busca solo
    public float cooldown = 1f;                          // segundos entre pulsaciones
    public Color idleColor = new Color(0.85f, 0.1f, 0.1f);
    public Color hoverColor = new Color(1f, 0.5f, 0.2f);
    public Color pressedColor = new Color(1f, 0.85f, 0.2f);
    public AudioClip clickClip;
    public bool allowMouseClick = true;
    public bool debugLogs = true;

    XRSimpleInteractable interactable;
    Renderer rend;
    float lastPress = -10f;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        rend = GetComponentInChildren<Renderer>();
        SetColor(idleColor);

        var col = GetComponentInChildren<Collider>();
        if (col == null) gameObject.AddComponent<BoxCollider>();
        else if (col.isTrigger) col.isTrigger = false;

        if (assembly == null)
        {
#if UNITY_2023_1_OR_NEWER
            assembly = FindFirstObjectByType<DroneAssembly>();
#else
            assembly = FindObjectOfType<DroneAssembly>();
#endif
        }
        if (assembly == null)
            Debug.LogWarning("[DroneResetButton] No encontre el DroneAssembly en la escena.", this);
    }

    IEnumerator Start()
    {
        yield return null;

        // Si el interactable quedo sin colliders, registrarlos a mano.
        var cols = GetComponentsInChildren<Collider>();
        if (interactable.colliders.Count == 0 && cols.Length > 0)
        {
            foreach (var c in cols) interactable.colliders.Add(c);
            interactable.enabled = false;
            interactable.enabled = true;
            Debug.LogWarning("[DroneResetButton] El interactable no tenia colliders; los registre a mano.", this);
        }

        if (debugLogs)
            Debug.Log("[DroneResetButton] Listo. Colliders registrados: " + interactable.colliders.Count +
                      " | InteractionManager: " + (interactable.interactionManager != null ? "OK" : "NO ENCONTRADO") +
                      " | Assembly: " + (assembly != null ? "OK" : "NO"));
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectEntered.AddListener(OnSelect);
        interactable.activated.AddListener(OnActivate);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
        interactable.selectEntered.RemoveListener(OnSelect);
        interactable.activated.RemoveListener(OnActivate);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (!allowMouseClick || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        var cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 20f) && hit.collider.transform.IsChildOf(transform))
            Press("clic de mouse");
#endif
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        SetColor(hoverColor);
        if (debugLogs) Debug.Log("[DroneResetButton] El controlador esta apuntando al boton.");
    }

    void OnHoverExit(HoverExitEventArgs args) { SetColor(idleColor); }
    void OnSelect(SelectEnterEventArgs args)  { Press("seleccionar (agarre)"); }
    void OnActivate(ActivateEventArgs args)   { Press("activar (gatillo)"); }

    void Press(string how)
    {
        if (Time.time - lastPress < cooldown) return;
        lastPress = Time.time;

        if (debugLogs) Debug.Log("[DroneResetButton] Boton presionado con: " + how);

        if (assembly == null)
        {
            Debug.LogWarning("[DroneResetButton] No hay DroneAssembly asignado.", this);
            return;
        }

        if (clickClip != null) AudioSource.PlayClipAtPoint(clickClip, transform.position);
        StartCoroutine(Flash());
        assembly.ResetAll();
    }

    IEnumerator Flash()
    {
        SetColor(pressedColor);
        yield return new WaitForSeconds(0.25f);
        SetColor(idleColor);
    }

    // URP usa _BaseColor; el shader clasico usa _Color. Se aplican los dos si existen.
    void SetColor(Color c)
    {
        if (rend == null) return;
        var mat = rend.material;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }
}
