// Radio.cs
using System.Collections;
using UnityEngine;

public class Radio : MonoBehaviour
{
    public RadioSlot slot;
    public Renderer[] leds;              // Led0..Led4
    public Material offMat, syncedMat, unsyncedMat;

    public bool IsCorrect => slot.IsCorrect;

    void SetLed(int i, Material m) => leds[i].sharedMaterial = m;

    public void AllLeds(Material m)
    {
        for (int i = 0; i < leds.Length; i++) SetLed(i, m);
    }

    public IEnumerator PlaySync(bool success)
    {
        AllLeds(offMat);
        yield return new WaitForSeconds(0.5f);

        if (success)
        {
            // LEDs light up one by one
            for (int i = 0; i < leds.Length; i++)
            {
                SetLed(i, syncedMat);
                yield return new WaitForSeconds(0.4f);
            }
        }
        else
        {
            // Blink red, then stay red
            for (int n = 0; n < 3; n++)
            {
                AllLeds(unsyncedMat); yield return new WaitForSeconds(0.2f);
                AllLeds(offMat);      yield return new WaitForSeconds(0.2f);
            }
            AllLeds(unsyncedMat);
        }
    }
}