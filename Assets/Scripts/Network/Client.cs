using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Client : IMessageChecker
{
    public bool isActive = false;
    public DateTime timeStamp;
    public int id;
    public IPEndPoint ipEndPoint;
    public string tag;
    public float timer;
    protected Dictionary<MessageType, ulong> lastReceiveMessage = new();
    public Dictionary<MessageType, List<MessageCache>> pendingMessages = new();
    public List<MessageCache> lastImportantMessages = new();
    private IMessageChecker _messageCheckerImplementation;
    UnityEvent<byte[], IPEndPoint> IMessageChecker.OnPreviousData { get; set;}= new();
    public Client(IPEndPoint ipEndPoint, int id, DateTime timeStamp, string tag)
    {
        this.timeStamp = timeStamp;
        this.id = id;
        this.ipEndPoint = ipEndPoint;
        this.tag = tag;
        timer = 0.0f;
        isActive = true;
    }
    public void OnDestroy()
    {
        lastImportantMessages.Clear();
        pendingMessages.Clear();
        lastReceiveMessage.Clear();
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
            lastReceiveMessage[messageType] = value;
            return true;
        }

        return true;
    }

    public bool IsTheNextMessage(MessageType messageType, ulong value, BaseMessage baseMessage)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }
        
        if (lastReceiveMessage[messageType] + 1 == value)
        {
            lastReceiveMessage[messageType] = value;
            CheckPendingMessages(messageType, value);

            return true;
        }
        else
        {
            pendingMessages.TryAdd(messageType, new List<MessageCache>());
            pendingMessages[messageType].Add(new MessageCache(messageType, value));
            pendingMessages[messageType].Sort(Utilities.Sorter);
            return false;
        }
    }

  

    public void CheckPendingMessages(MessageType messageType, ulong value)
    {
        if (pendingMessages.ContainsKey(messageType) && pendingMessages[messageType].Count > 0)
        {
            pendingMessages[messageType].Sort(Utilities.Sorter);
            if (value - pendingMessages[messageType][0].messageId + 1 == 0)
            {
                ((IMessageChecker)this).OnPreviousData.Invoke(pendingMessages[messageType][0].data.ToArray(), ipEndPoint);
                pendingMessages[messageType].RemoveAt(0);
            }
        }
    }




    public void CheckImportantMessageConfirmation((MessageType, ulong) data)
    {
        foreach (var cached in lastImportantMessages)
        {
            Debug.Log($"Id Comparison {cached.messageId} & {data.Item2}");
            if (cached.messageId == data.Item2 && cached.type == data.Item1)
            {
                cached.startTimer = true;
                cached.canBeResend = false;
                Debug.Log($"Confirmation from client {id} of {cached.type} with id {cached.messageId} was received.");
                lastImportantMessages?.Remove(cached);
                break;
            }
        }
    }
}

