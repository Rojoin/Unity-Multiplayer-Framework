using System;
using System.Collections.Generic;
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
    protected Dictionary<MessageType, ulong> lastReceiveMessage = new();
    public Dictionary<MessageType, List<object>> pendingMesagges= new();

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

    public bool IsTheLastMesagge(MessageType messageType, ulong value)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }

        if (lastReceiveMessage[messageType] < value)
        {
            return false;
        }

        lastReceiveMessage[messageType] = value;
        return true;
    }

    public ulong IsTheNextMessage<T>(MessageType messageType, ulong value, OrderableMessage<T> baseMessage)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return 0;
        }


        if (lastReceiveMessage[messageType] + 1 == value)
        {
            lastReceiveMessage[messageType] = value;
            return 0;
        }
        else
        {
            pendingMesagges[messageType].Add(baseMessage);
            return lastReceiveMessage[messageType] + 1;
        }
    }
    

 
}