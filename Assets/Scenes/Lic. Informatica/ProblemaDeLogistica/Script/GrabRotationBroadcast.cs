using UnityEngine;

public class GrabRotationBroadcast : MonoBehaviour
{
    public GameObject reciever;

    HingeJoint joint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        joint = GetComponent<HingeJoint>();
    }
    // Update is called once per frame
    void Update()
    {
        if(reciever != null)
        {
            float progreso = Mathf.InverseLerp(joint.limits.min,joint.limits.max,joint.angle);
            reciever.SendMessage("SetRotationValue",progreso);
        }
    }
}
