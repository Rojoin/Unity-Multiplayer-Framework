using System;
using System.IO;
using System.Text;
using UnityEngine;

public class UnityConsoleWriter : TextWriter
{
    public override void Write(char value)
    {
        base.Write(value);
        Debug.Log(value);
    }

    public override void Write(string value)
    {
        base.Write(value);
        Debug.Log(value);
    }

    public override void WriteLine(string value)
    {
        base.WriteLine(value);
        Debug.Log(value);
    }

    public override Encoding Encoding
    {
        get { return Encoding.UTF8; }
    }
}
public class RedirectConsoleOutput : MonoBehaviour
{
    void Awake()
    {
        Console.SetOut(new UnityConsoleWriter());
    }

}