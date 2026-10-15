using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ProgramManager : MonoBehaviour
{
    public List<XRSocketInteractor> slots;   // en orden, izquierda a derecha
    public Drone drone;
    public TMP_Text statusText;              // opcional
    public int maxIteraciones = 10;          // seguridad contra bucles infinitos
    public Color activeColor = Color.yellow;

    Coroutine running;
    Renderer[] slotRenderers;
    Color[] slotColors;

    void Awake()
    {
        // Para resaltar el socket activo (usa el Renderer del socket o de sus hijos, si tiene)
        slotRenderers = new Renderer[slots.Count];
        slotColors = new Color[slots.Count];
        for (int i = 0; i < slots.Count; i++)
        {
            slotRenderers[i] = slots[i].GetComponentInChildren<Renderer>();
            if (slotRenderers[i] != null) slotColors[i] = slotRenderers[i].material.color;
        }
    }

    // Conecta estos dos a tus botones (Select Entered)
    public void Run()
    {
        if (running != null) return;
        running = StartCoroutine(Execute());
    }

    public void ResetAll()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        drone.ResetDrone();
        Highlight(-1);
        SetStatus("Arma tu programa y presiona EJECUTAR.");
    }

    IEnumerator Execute()
    {
        // 1. Leer el programa: lista de (índice de socket, comando)
        var program = new List<(int slot, CommandBlock.Cmd cmd)>();
        bool loop = false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].hasSelection) continue;
            var b = slots[i].interactablesSelected[0].transform.GetComponent<CommandBlock>();
            if (b == null) continue;

            if (b.command == CommandBlock.Cmd.Repetir) { loop = true; break; } // lo de después se ignora
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
        do
        {
            foreach (var step in program)
            {
                Highlight(step.slot);
                yield return drone.Do(step.cmd);
            }
            iter++;
        }
        while (loop && !drone.Finished && iter < maxIteraciones);

        Highlight(-1);
        SetStatus(drone.Finished ? "¡Misión cumplida!" : "El programa terminó pero faltan cubos.");
        running = null;
    }

    void Highlight(int index)
    {
        for (int i = 0; i < slotRenderers.Length; i++)
            if (slotRenderers[i] != null)
                slotRenderers[i].material.color = (i == index) ? activeColor : slotColors[i];
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }
}