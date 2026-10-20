using Unity.VRTemplate;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class RoundTableComponent : MonoBehaviour
{
    public GameObject controller;
    public float offset = 120.0f;

    XRKnob XRKnob;
    void Start()
    {
        if (controller != null)
        {
            XRKnob = controller.GetComponent<XRKnob>();
        }
    }
    void Update()
    {
        if (XRKnob != null)
        {
            float value = XRKnob.value;
            Vector3 currentRot = gameObject.transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(currentRot.x, value * offset, currentRot.z);
        }
        
    }
}
