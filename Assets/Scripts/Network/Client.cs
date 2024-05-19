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
    public Dictionary<MessageType, MessageCache> lastReceiveMessage = new();
    public Dictionary<MessageType, List<MessageCache>> pendingMessages = new();
    public List<MessageCache> lastImportantMessages = new();
    private IMessageChecker _messageCheckerImplementation;
    UnityEvent<byte[], IPEndPoint> IMessageChecker.OnPreviousData { get; set; } = new();

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

    public MessageCache GetLastMessage(MessageType msg)
    {
        return lastReceiveMessage[msg];
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

    public bool IsTheLastMesagge(MessageType messageType, MessageCache msgToCache)
    {
        if (lastReceiveMessage.TryAdd(messageType, msgToCache))
        {
            return true;
        }

        if (lastReceiveMessage[messageType].messageId < msgToCache.messageId)
        {
            lastReceiveMessage[messageType] = msgToCache;
            return true;
        }

        return true;
    }

    public bool IsTheNextMessage(MessageType messageType, MessageCache value, BaseMessage baseMessage)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return true;
        }

        if (lastReceiveMessage[messageType].messageId + 1 == value.messageId)
        {
            lastReceiveMessage[messageType] = value;
            CheckPendingMessages(messageType, value.messageId);

            return true;
        }
        else
        {
            pendingMessages.TryAdd(messageType, new List<MessageCache>());
            pendingMessages[messageType].Add(new MessageCache(messageType, value.messageId));
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
                ((IMessageChecker)this).OnPreviousData.Invoke(pendingMessages[messageType][0].data.ToArray(),
                    ipEndPoint);
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