using UnityEngine;

public class LightMat : MonoBehaviour
{

    public Animator electricBoxAnim;
    public Material lightMat;

    public void EnableAnim()
    {
        electricBoxAnim.SetBool("IsEnable", true);
    }

    public void DisableAnim()
    {
        electricBoxAnim.SetBool("IsEnable", false);        
    }

    public void LightUp()
    {
        lightMat.SetColor("_EmissionColor", Color.yellow);
    }

    public void LightOff()
    {
        lightMat.SetColor("_EmissionColor", Color.black);
    }
}
