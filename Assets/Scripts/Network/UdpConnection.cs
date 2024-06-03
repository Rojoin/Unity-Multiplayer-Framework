using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using RojoinNetworkSystem;
using UnityEngine;
using UnityEngine.Scripting;

public class UdpConnection
{
    private struct DataReceived
    {
        public byte[] data;
        public IPEndPoint ipEndPoint;
    }

    public int playerId = -1;

    public UdpClient connection;
    private IReceiveData receiver = null;
    private Queue<DataReceived> dataReceivedQueue = new Queue<DataReceived>();
    public event Action<string> OnSocketError;

    object handler = new object();
    public string nameTag;

    public UdpConnection(int port, in Action<string> handler, IReceiveData receiver = null)
    {
        OnSocketError += handler;
        try
        {
            connection = new UdpClient(port);
            connection.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.receiver = receiver;
            connection.BeginReceive(OnReceive, null);
        }
        catch (Exception e)
        {
            OnSocketError?.Invoke($"Error: The port {port} is already use as a Server.");
        }
    }

    public UdpConnection(IPAddress ip, int port, string tag, in Action<string> handler, IReceiveData receiver = null)
    {
        OnSocketError += handler;
        try
        {
            connection = new UdpClient();
            connection.Connect(ip, port);
            this.receiver = receiver;

            connection.BeginReceive(OnReceive, null);

            NetHandShake handShake = new NetHandShake(tag);
            Send(handShake.Serialize());
        }
        catch (Exception e)
        {
            OnSocketError?.Invoke($"Error: The port {port} doesnt have a server initialized.");
            Debug.Log($"Error: The port {port} doesnt have a server initialized.");
        }
    }

    public void Close()
    {
        OnSocketError = null;
        dataReceivedQueue.Clear();
        connection?.Dispose();
        connection?.Close();
    }


    public void FlushReceiveData()
    {
        lock (handler)
        {
            while (dataReceivedQueue.Count > 0)
            {
                DataReceived dataReceived = dataReceivedQueue.Dequeue();
                receiver.OnReceiveData(dataReceived.data, dataReceived.ipEndPoint);
            }
        }
    }

    void OnReceive(IAsyncResult ar)
    {
        DataReceived dataReceived = new DataReceived();
        try
        {
            if (connection.Client.Connected)
            {
                dataReceived.data = connection.EndReceive(ar, ref dataReceived.ipEndPoint);
            }
 
        }
        catch (SocketException e)
        {
            // This happens when a client disconnects, as we fail to send to that port.
            //OnSocketError?.Invoke("[UdpConnection] " + e.Message);
            Debug.Log("[UdpConnection] " + e.Message);
        }


        lock (handler)
        {
            dataReceivedQueue?.Enqueue(dataReceived);
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