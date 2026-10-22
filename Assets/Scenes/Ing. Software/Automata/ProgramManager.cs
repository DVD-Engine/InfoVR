using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ProgramManager : MonoBehaviour
{
    public List<XRSocketInteractor> slots;   // en orden, izquierda a derecha
    public Drone drone;
    public TMP_Text statusText;
    public int maxIteraciones = 10;          // seguridad contra bucles infinitos

    Coroutine running;

    // Botón EJECUTAR
    public void Run()
    {
        if (running != null) return;
        running = StartCoroutine(Execute());
    }

    // Botón LIMPIAR (junto con BlockResetter.ResetBlocks)
    public void ResetAll()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        drone.ResetDrone();
        SetStatus("Arma tu programa y presiona EJECUTAR.");
    }

    IEnumerator Execute()
    {
        // 1. Leer el programa: (número de socket, comando)
        var program = new List<(int slot, CommandBlock.Cmd cmd)>();
        bool loop = false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].hasSelection) continue;
            var b = slots[i].interactablesSelected[0].transform.GetComponent<CommandBlock>();
            if (b == null) continue;

            if (b.command == CommandBlock.Cmd.Repetir) { loop = true; break; }
            program.Add((i, b.command));
        }

        if (program.Count == 0)
        {
            SetStatus("Programa vacío.");
            running = null;
            yield break;
        }

        // 2. Ejecutar
        SetStatus("Ejecutando...");
        int iter = 0;
        bool failed = false;

        do
        {
            foreach (var step in program)
            {
                yield return drone.Do(step.cmd);

                if (drone.LastError != "")
                {
                    SetStatus("Falló el paso " + (step.slot + 1) + ": " + drone.LastError);
                    failed = true;
                    break;
                }
            }
            iter++;
        }
        while (!failed && loop && !drone.Finished && iter < maxIteraciones);

        if (!failed)
        {
            if (drone.Finished)
                SetStatus("¡Misión cumplida!");
            else if (!loop)
                SetStatus("El programa terminó pero faltan cubos. ¿Cómo lo repites?");
            else
                SetStatus("Se alcanzó el límite de repeticiones y faltan cubos.");
        }

        running = null;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}