using System;
using System.Net;
[Serializable]
public class Client
{
    public bool isActive = false;
    public DateTime timeStamp;
    public int id;
    public IPEndPoint ipEndPoint;
    public string tag;
    public float timer;

    public Client(IPEndPoint ipEndPoint, int id, DateTime timeStamp, string tag)
    {
        this.timeStamp = timeStamp;
        this.id = id;
        this.ipEndPoint = ipEndPoint;
        this.tag = tag;
        timer = 0.0f;
        isActive = true;
    }

    public TimeSpan GetCurrentMS(DateTime currentTimeStamp)
    {
        return currentTimeStamp - this.timeStamp;
    }

    public void ResetTimer(DateTime currentTimeStamp)
    {
        this.timer = 0.0f;
        timeStamp = currentTimeStamp;
    }
}