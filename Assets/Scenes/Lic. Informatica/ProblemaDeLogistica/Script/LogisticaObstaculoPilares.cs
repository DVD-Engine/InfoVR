using UnityEngine;

public class LogisticaObstaculoPilares : MonoBehaviour
{
    public GameObject[] pilares = new GameObject[2];
    public float leverRotation;
    public float movementScale;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        Vector3 pos1 = pilares[0].transform.localPosition;
        pilares[0].transform.localPosition = new Vector3(pos1.x,(leverRotation * movementScale),pos1.z);
        Vector3 pos2 = pilares[1].transform.localPosition;
        pilares[1].transform.localPosition = new Vector3(pos2.x, (leverRotation * -movementScale), pos2.z);
    }

    public void SetRotationValue(float value)
    {
        leverRotation = value;
    }
}
