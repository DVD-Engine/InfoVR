using UnityEngine;

public class Satellite : MonoBehaviour
{
    public Transform planet;
    public Renderer[] strengthBars;   // los 4 cilindros de ConStrength
    public Renderer[] loadBlocks;     // los 10 bloques de LoadBar

    public Material offMat, redMat, yellowMat, greenMat;

    public float turnSpeed = 40f;
    public float timePerBlock = 2f;   // segundos por bloque con señal fuerte

    // Los botones ponen estos valores: -1, 0 o 1
    float horizontal;
    float vertical;

    float progress;                   // 0 a loadBlocks.Length

    // ----- Los botones llaman a estas funciones desde el Inspector -----
    public void Left(bool pressed)  { horizontal = pressed ? -1 : 0; }
    public void Right(bool pressed) { horizontal = pressed ? 1 : 0; }
    public void Up(bool pressed)    { vertical = pressed ? 1 : 0; }
    public void Down(bool pressed)  { vertical = pressed ? -1 : 0; }

    void Update()
    {
        // 1. Girar
        transform.Rotate(0, horizontal * turnSpeed * Time.deltaTime, 0, Space.World);
        transform.Rotate(-vertical * turnSpeed * Time.deltaTime, 0, 0, Space.Self);

        // 2. Qué tan bien apunta al planeta
        float angle = Vector3.Angle(transform.forward, planet.position - transform.position);

        int strength = 0;                 // 0 = sin señal, 1 = débil, 2 = media, 3 = fuerte
        if (angle < 5) strength = 3;
        else if (angle < 15) strength = 2;
        else if (angle < 30) strength = 1;

        // 3. Avanzar la barra (más fuerte = más rápido)
        progress += strength * Time.deltaTime / timePerBlock;
        progress = Mathf.Min(progress, loadBlocks.Length);

        // 4. Mostrar
        ShowStrength(strength);
        ShowProgress();
    }

    void ShowStrength(int strength)
    {
        Material color = offMat;
        if (strength == 1) color = redMat;
        if (strength == 2) color = yellowMat;
        if (strength == 3) color = greenMat;

        for (int i = 0; i < strengthBars.Length; i++)
        {
            if (i < strength + (strength > 0 ? 1 : 0))
                strengthBars[i].sharedMaterial = color;
            else
                strengthBars[i].sharedMaterial = offMat;
        }
    }

    void ShowProgress()
    {
        for (int i = 0; i < loadBlocks.Length; i++)
        {
            float done = progress - i;    // cuánto de este bloque está lleno (0 a 1)

            if (done <= 0)         loadBlocks[i].sharedMaterial = offMat;
            else if (done < 0.5f)  loadBlocks[i].sharedMaterial = redMat;
            else if (done < 1f)    loadBlocks[i].sharedMaterial = yellowMat;
            else                   loadBlocks[i].sharedMaterial = greenMat;
        }
    }
}