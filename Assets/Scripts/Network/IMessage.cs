using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;
using System.ComponentModel;

public enum MessageType
{
    HandShake = -1,
    Console = 0,
    Position = 1,
    String = 2,
    Exit = 3
}
//TODO: Cambiar a Clase base

public abstract class BaseMessage<PayloadType>
{
    protected MessageType Type;
    protected PayloadType Data;
    protected int PlayerID;

    protected BaseMessage(PayloadType data)
    {
        Data = data;
    }

    protected BaseMessage()
    {
    }

    public MessageType GetMessageType()
    {
        return Type;
    }

    public abstract byte[] Serialize();
    public abstract PayloadType Deserialize(byte[] message);

    public PayloadType GetData()
    {
        return Data;
    }
}

public abstract class OrderableMessage<PayloadType> : BaseMessage<PayloadType>
{
    protected static ulong lastMsgID = 0;
    protected static Dictionary<MessageType, ulong> lastSendMessage = new();
    protected ulong messageId;

    public bool IsTheNewestMessage()
    {
        if (lastSendMessage[Type] < messageId)
        {
            return false;
        }

        lastSendMessage[Type] = messageId;
        return true;
    }
}

public class NetHandShake : BaseMessage<int>
{

    public NetHandShake( int id)
    {
        PlayerID = id;
        Type = MessageType.HandShake;
    } 
    public NetHandShake()
    {
        Type = MessageType.HandShake;
    }

    public override int Deserialize(byte[] message)
    {
        PlayerID = BitConverter.ToInt32(message, 4);
        return PlayerID;
    }
    
    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

        outData.AddRange(BitConverter.GetBytes(PlayerID));
        
        return outData.ToArray();
    }
}

public class NetExit : BaseMessage<int>
{
    public NetExit()
    {
        Type = MessageType.Exit;
    }
    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();
        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

        outData.AddRange(BitConverter.GetBytes(PlayerID));
        return outData.ToArray();
    }

    public override int Deserialize(byte[] message)
    {
        PlayerID = BitConverter.ToInt32(message, 4);
        return PlayerID;
    }
}
public class NetVector3 : OrderableMessage<UnityEngine.Vector3>
{
    private Vector3 data;

    public NetVector3(Vector3 data)
    {
        this.data = data;
        Type = MessageType.Position;
    }

    public override Vector3 Deserialize(byte[] message)
    {
        Vector3 outData;

        messageId = BitConverter.ToUInt64(message, 8);
        outData.x = BitConverter.ToSingle(message, 16);
        outData.y = BitConverter.ToSingle(message, 20);
        outData.z = BitConverter.ToSingle(message, 24);

        return outData;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(PlayerID));
        outData.AddRange(BitConverter.GetBytes(lastMsgID++));
        outData.AddRange(BitConverter.GetBytes(data.x));
        outData.AddRange(BitConverter.GetBytes(data.y));
        outData.AddRange(BitConverter.GetBytes(data.z));

        return outData.ToArray();
    }

    //Dictionary<Client,Dictionary<msgType,int>>
}

public class NetConsole : OrderableMessage<string>
{
    private string data;

    public NetConsole()
    {
    }

    public NetConsole(string data)
    {
        this.data = data;
        Type = MessageType.String;
    }


    public override string Deserialize(byte[] message)
    {
        string outData = "";
        messageId = BitConverter.ToUInt64(message, 8);
        int messageLength = BitConverter.ToInt32(message, 16);
        Debug.Log(messageLength);
        for (int i = 0; i < messageLength; i++)
        {
            //outData += BitConverter.ToChar(message, 16 + i);
            outData += (char)message[20 + i];
        }

        return outData;
    }


    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();
        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
        outData.AddRange(BitConverter.GetBytes(PlayerID));
        outData.AddRange(BitConverter.GetBytes(lastMsgID++));
        Debug.Log("Array Data" + data.Length);
        outData.AddRange(BitConverter.GetBytes(data.Length));
        for (int i = 0; i < data.Length; i++)
        {
            outData.Add((byte)data[i]);
        }

        return outData.ToArray();
    }
}

public class NetByteTranslator
{
    public static MessageType getNetworkType(byte[] data)
    {
        (long, int) dataOut;
        dataOut.Item1 = BitConverter.ToInt32(data, 0);
        return (MessageType)dataOut.Item1;
    }
}