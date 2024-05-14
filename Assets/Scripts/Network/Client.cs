using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

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

    public ulong IsTheNextMessage(MessageType messageType, ulong value, BaseMessage baseMessage)
    {
        if (lastReceiveMessage.TryAdd(messageType, value))
        {
            return 0;
        }


        if (lastReceiveMessage[messageType] + 1 == value)
        {
            lastReceiveMessage[messageType] = value;
            if (pendingMessages[messageType].Count > 0)
            {
                //Todo: Check if the message is the next.
            }

            return 0;
        }
        else
        {
            //TODO: Ask for the message that is left
            pendingMessages.TryAdd(messageType, new List<MessageCache>());
            pendingMessages[messageType].Add(new MessageCache(messageType, value));
            //Todo: Hacer más lindo
            pendingMessages[messageType].Sort(Sorter);
            return lastReceiveMessage[messageType] + 1;
        }
    }

    private int Sorter(MessageCache cache1,MessageCache  cache2)
    {
        return cache1.messageId > cache2.messageId ? (int)cache1.messageId :  (int)cache2.messageId;
    }
    private void CheckPendingMessages(MessageType messageType, int value)
    {
        foreach (var messages in pendingMessages[messageType])
        {
            
        }
    }


    public void CheckImportantMessageConfirmation((MessageType, ulong) data)
    {
        foreach (var cached in lastImportantMessages)
        {
            if (cached.messageId == data.Item2 && cached.type == data.Item1)
            {
                Debug.Log($"Confirmation from client {id} of {cached.type} with id {cached.messageId}");
                lastImportantMessages?.Remove(cached);
                break;
            }
        }
    }
}

public class NewList<T> : ICollection<T>

{
    private ICollection<T> _collectionImplementation;

    public IEnumerator<T> GetEnumerator()
    {
        return _collectionImplementation.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_collectionImplementation).GetEnumerator();
    }

    public void Add(T item)
    {
        _collectionImplementation.Add(item);
    }

    public void Clear()
    {
        _collectionImplementation.Clear();
    }

    public bool Contains(T item)
    {
        return _collectionImplementation.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _collectionImplementation.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _collectionImplementation.Remove(item);
    }

    public int Count => _collectionImplementation.Count;

    public bool IsReadOnly => _collectionImplementation.IsReadOnly;
}