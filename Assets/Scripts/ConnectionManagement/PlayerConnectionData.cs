using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Unity.Netcode;
using UnityEngine;


[Serializable]
public class PlayerConnectionDataIp : PlayerConnectionDataBase, IEquatable<PlayerConnectionDataIp>
{
    public int port;
    public string playerName;
    public string ipaddress;

    public PlayerConnectionDataIp()
    {
        port = 0;
        ipaddress = "";
        playerName = "";
    }

    public PlayerConnectionDataIp(int port, string playerName, string ipaddress)
    {
        this.port = port;
        this.ipaddress = ipaddress;
        this.playerName = playerName;
    }

    public PlayerConnectionDataIp(int port, string playerName)
    {
        this.port = port;
        this.playerName = playerName;

        ipaddress = GetLocalAddress();
    }

    public bool Equals(PlayerConnectionDataIp other)
    {
        return other.port == port && other.playerName.ToString() == playerName.ToString() && other.ipaddress.ToString() == ipaddress.ToString();
    }

    public override void NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref port);
        serializer.SerializeValue(ref ipaddress);
        serializer.SerializeValue(ref playerName);
    }

    public static string GetLocalAddress()
    {
        string localAddress = "";
        string myAddressGlobal = "";
        //Get the local IP
        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in hostEntry.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localAddress = ip.ToString();
                break;
            } //if
        }
        //Get the global IP
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.ipify.org");
        request.Method = "GET";
        request.Timeout = 1000; //time in ms
        try
        {
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                myAddressGlobal = reader.ReadToEnd();
            } //if
            else
            {
                Debug.LogError("Timed out? " + response.StatusDescription);
                myAddressGlobal = "127.0.0.1";
            } //else
        } //try
        catch (WebException ex)
        {
            Debug.Log("Likely no internet connection: " + ex.Message);
            myAddressGlobal = "127.0.0.1";
        } //catch
        //myAddressGlobal=new System.Net.WebClient().DownloadString("https://api.ipify.org"); //single-line solution for the global IP, but long time-out when there is no internet connection, so I prefer to do the method above where I can set a short time-out time

        return localAddress;
    }
}

//Cannot use abstract :(
public class PlayerConnectionDataBase : INetworkSerializable
{
    virtual public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter { }

    public PlayerConnectionDataBase() { }
}
