using UnityEngine;

public class AnimToggle : MonoBehaviour
{
     Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void EnableAnim()
    {
        animator.SetBool("IsEnable", true);
    } 

    public void DisableAnim()
    {
        animator.SetBool("IsEnable", false);
    }
}
