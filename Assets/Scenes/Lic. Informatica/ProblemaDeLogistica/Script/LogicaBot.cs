using UnityEngine;
using UnityEngine.Splines;

public class LogicaBot : MonoBehaviour
{
    private SplineAnimate pathFollow;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pathFollow = GetComponent<SplineAnimate>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        pathFollow.Pause();
    }
    private void OnTriggerExit(Collider other)
    {
        pathFollow.Play();
    }

    public void game_start()
    {
        pathFollow.Play();
    }
}
