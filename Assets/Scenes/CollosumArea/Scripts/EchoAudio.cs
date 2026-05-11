using UnityEngine;

public class EchoAudio : MonoBehaviour
{
    public AudioSource AudioSource;
    public ParticleSystem ParticleSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioSource = GetComponent<AudioSource>();
        ParticleSystem = GetComponent<ParticleSystem>();
    }
    public void play()
    {
        AudioSource.Play();
        ParticleSystem.Play();
    }
    public void stop()
    {
        AudioSource.Stop();
        ParticleSystem.Stop();
    }
}
