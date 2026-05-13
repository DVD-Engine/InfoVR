using UnityEngine;

public class LanderScript : MonoBehaviour
{
    public Animator LanderAnim;
    public Animator BotonAnim;

    public void Pressed()
    {
        BotonAnim.SetTrigger("Play");
    }

        public void PlayLander()
    {
        LanderAnim.SetTrigger("Play");
    }
}