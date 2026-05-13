using UnityEngine;

public class AnimController : MonoBehaviour
{

    public Animator animator;

    public void EnableAnim()
    {
        animator.SetBool("IsEnable", true);
    }

    public void DisableAnim()
    {
        animator.SetBool("IsEnable", false);        
    }
}
