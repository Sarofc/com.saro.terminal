using UnityEngine;
using System.Collections;

public class MonoTestCommand : MonoBehaviour
{
    void Start()
    {
        Saro.Console.ConsoleCommand.AddCommandInstance(typeof(MonoTestCommand), this);

        Debug.Log("<color=green>log info</color>");
        Debug.LogWarning("<color=yellow>log warning</color>");
        Debug.LogError("<color=red>log error</color>");
    }

    // instance method
    [Saro.Console.Command("print_vector3", "Print vector3 value")]
    public void Print(Vector3 v3)
    {
        print(v3);
    }

    // static method
    [Saro.Console.Command("print_int", "Print int value")]
    public static void Print(int v)
    {
        print(v);
    }
    
    [Saro.Console.Command("print_bool", "Print boolean value")]
    public static void Print(bool v)
    {
        print(v);
    }
}
