using UnityEngine;

public class CommandBlock : MonoBehaviour
{
    public enum Cmd { IrACubo, Recoger, IrADepo, Soltar, Repetir }
    public Cmd command;
}