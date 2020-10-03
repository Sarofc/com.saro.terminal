using UnityEngine;
using System.Collections;
using System;

public class MonoTestCommand : MonoBehaviour
{
    IEnumerator Start()
    {
        Saro.Terminal.Terminal.Shell.AddCommandInstance(typeof(MonoTestCommand), this);

        Debug.Log("<color=green>log info</color>");
        Debug.LogWarning("<color=yellow>log warning</color>");
        Debug.LogError("<color=red>log error</color>");

        var thread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadLog));
        thread.IsBackground = true;
        thread.Start();

        var sec = new WaitForSeconds(0.5f);
        for (int i = 0; i < 50; i++)
        {
            Saro.Terminal.Terminal.Log("[main thread] tick");
            yield return sec;
        }
    }

    private void ThreadLog()
    {
        var count = 5;
        while (count > 0)
        {
            count--;
            Saro.Terminal.Terminal.Log("[worker thread] tick");
            System.Threading.Thread.Sleep(500);
        }
    }

    // instance method
    [Saro.Terminal.Command("print_vector3", "Print vector3 value")]
    public void Print(Vector3 v3)
    {
        print(v3);
    }

    // static method
    [Saro.Terminal.Command("print_int", "Print int value")]
    public static void Print(int v)
    {
        print(v);
    }

    [Saro.Terminal.Command("print_bool", "Print boolean value")]
    public static void Print(bool v)
    {
        print(v);
    }
}
