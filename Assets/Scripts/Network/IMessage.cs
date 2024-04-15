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
    String = 2
}
//TODO: Cambiar a Clase base

public abstract class BaseMessage<PayloadType>
{
    protected MessageType Type;
    protected PayloadType Data;

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
    protected static Dictionary<MessageType, ulong> lastSendMessage;
    protected ulong messageId;

   
    
    protected ulong GetMessageID(byte[] data)
    {
        return BitConverter.ToUInt64(data, 4);
    }
}

public class NetHandShake : BaseMessage<(long, int)>
{
    public (long, int) data;

    public NetHandShake(long data1, int id)
    {
        data.Item1 = data1;
        data.Item2 = id;
        Type = MessageType.HandShake;
    }

    public override (long, int) Deserialize(byte[] message)
    {
        (long, int) outData;

        outData.Item1 = BitConverter.ToInt64(message, 4);
        outData.Item2 = BitConverter.ToInt32(message, 12);

        return outData;
    }

    

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));

        outData.AddRange(BitConverter.GetBytes(data.Item1));
        outData.AddRange(BitConverter.GetBytes(data.Item2));


        return outData.ToArray();
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

        outData.x = BitConverter.ToSingle(message, 8);
        outData.y = BitConverter.ToSingle(message, 12);
        outData.z = BitConverter.ToSingle(message, 16);

        return outData;
    }

    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();

        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
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
        int messageLength = BitConverter.ToInt32(message, 12);
        Debug.Log(messageLength);
        for (int i = 0; i < messageLength; i++)
        {
            //outData += BitConverter.ToChar(message, 16 + i);
            outData += (char)message[16 + i];
        }

        return outData;
    }



    public override byte[] Serialize()
    {
        List<byte> outData = new List<byte>();
        outData.AddRange(BitConverter.GetBytes((int)GetMessageType()));
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