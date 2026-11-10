// SyncManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SyncManager : MonoBehaviour
{
    public Radio[] radios;
    public AudioSource audioSource;
    public AudioClip musicClip, staticClip;

    bool busy;

    public void StartSync()
    {
        if (!busy) StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        busy = true;

        // Setup check: tells you exactly which radio is misconfigured
        foreach (var r in radios)
        {
            if (r == null || r.slot == null || r.leds == null || r.leds.Length == 0)
            {
                Debug.LogError($"Radio setup incomplete on: {(r != null ? r.name : "null entry")}");
                busy = false;
                yield break;
            }
        }

        audioSource.Stop();

        // 1. Read the results BEFORE locking (an empty slot counts as incorrect)
        bool[] results = radios.Select(r => r.IsCorrect).ToArray();
        bool allCorrect = results.All(x => x);

        // 2. Lock the pieces in place
        foreach (var r in radios) r.slot.Lock();

        // 3. Each radio plays its own result in parallel
        var running = new List<Coroutine>();
        for (int i = 0; i < radios.Length; i++)
            running.Add(StartCoroutine(radios[i].PlaySync(results[i])));
        foreach (var c in running) yield return c;

        // 4. Music if everything synced, static otherwise
        audioSource.clip = allCorrect ? musicClip : staticClip;
        audioSource.loop = !allCorrect;
        audioSource.Play();

        // 5. On failure, let the player try again
        if (!allCorrect)
        {
            yield return new WaitForSeconds(3f);
            audioSource.Stop();
            foreach (var r in radios)
            {
                r.slot.Unlock();
                r.AllLeds(r.offMat);
            }
        }

        busy = false;
    }
}