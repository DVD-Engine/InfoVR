using System.Collections;
using UnityEngine;

public class DonreTest : MonoBehaviour
{
    IEnumerator Start()
    {
        var d = GetComponent<Drone>();
        yield return new WaitForSeconds(1);
        for (int i = 0; i < 3; i++)
        {
            yield return d.Do(CommandBlock.Cmd.IrACubo);
            yield return d.Do(CommandBlock.Cmd.Recoger);
            yield return d.Do(CommandBlock.Cmd.IrADepo);
            yield return d.Do(CommandBlock.Cmd.Soltar);
        }
    }
}