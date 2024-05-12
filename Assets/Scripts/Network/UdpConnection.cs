using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UdpConnection
{
    private struct DataReceived
    {
        public byte[] data;
        public IPEndPoint ipEndPoint;
    }
    public int playerId = -1;

    private UdpClient connection;
    private IReceiveData receiver = null;
    private Queue<DataReceived> dataReceivedQueue = new Queue<DataReceived>();

    object handler = new object();
    public string nameTag;

    public UdpConnection(int port, IReceiveData receiver = null)
    {
        connection = new UdpClient(port);
        connection.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        this.receiver = receiver;
        connection.BeginReceive(OnReceive, null);
    }

    public UdpConnection(IPAddress ip, int port, string tag, IReceiveData receiver = null)
    {
        connection = new UdpClient();
        connection.Connect(ip, port);
        this.receiver = receiver;
        
        connection.BeginReceive(OnReceive, null);

        NetHandShake handShake = new NetHandShake(tag);
        Send(handShake.Serialize());
    }

    public void Close()
    {
        dataReceivedQueue.Clear();
        connection.Dispose();
        connection.Close();
    }
    

    public void FlushReceiveData()
    {
        lock (handler)
        {
            while (dataReceivedQueue.Count > 0)
            {
                DataReceived dataReceived = dataReceivedQueue.Dequeue();
                if (receiver != null)
                    receiver.OnReceiveData(dataReceived.data, dataReceived.ipEndPoint);
            }
        }
    }

    void OnReceive(IAsyncResult ar)
    {
        try
        {
            DataReceived dataReceived = new DataReceived();
            dataReceived.data = connection.EndReceive(ar, ref dataReceived.ipEndPoint);

            lock (handler)
            {
                dataReceivedQueue.Enqueue(dataReceived);
            }
        }
        catch (SocketException e)
        {
            // This happens when a client disconnects, as we fail to send to that port.
            UnityEngine.Debug.LogError("[UdpConnection] " + e.Message);
        }

        connection.BeginReceive(OnReceive, null);
    }

    public void Send(byte[] data)
    {
        connection.Send(data, data.Length);
    }

    public void Send(byte[] data, IPEndPoint ipEndpoint)
    {
        connection.Send(data, data.Length, ipEndpoint);
    }
}