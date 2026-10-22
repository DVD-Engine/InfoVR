using UnityEngine;

public class SimpleAnimate : MonoBehaviour
{
    public Animation animation;

    public void Animate()
    {
        // Funcion que previene el spam
        if (!animation.isPlaying)
        {
            animation.Play();
        }
    }
}
