using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class ImageReceiver : MonoBehaviour {

    Thread startThread;
    Thread stopThread;

    volatile bool keepReading = false;

    [SerializeField]
    private int port;

    [SerializeField]
    private int picWidth;
    [SerializeField]
    private int picHeight;

    private bool serverRunning = false;

    private Socket listener;
    private Socket handler;

    Texture2D tex = null;

    private byte[] picData = null;
    private int picSize = 0;

    void Start() {
        Application.runInBackground = true;

        picData = new byte[picWidth * picHeight * 4];

        StartListener();
    }

    void Update() {
        if (picSize == picHeight * picWidth * 4) {
            try {
                //string picS = BitConverter.ToString(picData);
                //byte[] bytePicData = Encoding.ASCII.GetBytes(picS);
                ProcessMessage(picData);
            }
            finally {
                picSize = 0;
            }
        }

        if (!serverRunning) {
            StartListener();
        }
    }

    public void StartListener() {
        if (!serverRunning) {
            startThread = new System.Threading.Thread(Listen);
            startThread.IsBackground = true;
            startThread.Start();

            serverRunning = true;
            Debug.Log("Listener Started");
        }
    }

    private string GetIPAddress() {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }

    #region NETWORK_CODE

    private void Listen() {
        string data;

        // Data buffer for incoming data.
        //byte[] bytes = new Byte[1024];

        // host running the application.
        Debug.Log("Ip " + GetIPAddress());
        IPAddress[] ipArray = Dns.GetHostAddresses(GetIPAddress());
        IPEndPoint localEndPoint = new IPEndPoint(ipArray[0], port);

        // Create a TCP/IP socket.
        listener = new Socket(ipArray[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        bool listenerShouldRun = true;

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.
        try {
            //Log(new VRMessage("Waiting for Connection"));

            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.
            while (listenerShouldRun) {
                keepReading = true;

                // Program is suspended while waiting for an incoming connection.
                //Log("Waiting for Connection");     //It works

                handler = listener.Accept();
                Debug.Log("Client connected");
                //data = null;

                // An incoming connection needs to be processed.
                while (keepReading) {
                    //bytes = new byte[picWidth * picHeight * 4];
                    int bytesRec = handler.Receive(picData, picSize, picWidth * picHeight * 4 - picSize, SocketFlags.None);
                    picSize += bytesRec;

                    Debug.Log("Received new data (" + picSize + "/" + picWidth * picHeight * 4 + " bytes)");
                    //Debug.Log(picData[picSize] + " " + picData[picSize + 1] + " " + picData[picSize + 2] + " " + picData[picSize + 3] + " " + picData[picSize + 4] + " " + picData[picSize + 5] + " " + picData[picSize + 6] + " " + picData[picSize + 7] + " " + picData[picSize + 8] + " " + picData[picSize + 9]);

                    if (bytesRec <= 0 || picSize >= picWidth * picHeight * 4) {
                        StopListening();
                        break;
                    }

                    //picUpdated = true;
                }
            }
        }
        catch (Exception e) {
            //Debug.Log(e.ToString());
            StopListening();
        }
    }

    public void StopListening() {
        if (serverRunning) {
            keepReading = serverRunning = false;
            //serverRunning = false;

            //stop thread
            if (startThread != null) {
                startThread.Abort();
            }

            if (handler != null && handler.Connected) {
                handler.Disconnect(false);
                //Debug.Log("Disconnected!");
            }

            picSize = 0;

            //Debug.Log("Server STOP");
        }
    }
    #endregion

    private void ProcessMessage(byte[] data) {
        Debug.Log("Processing...: " + data[0] + "," + data[1] + "," + data[2] + "," + data[3] + "," + data[4] + "," + data[5] + "...");

        if (tex == null) {
            tex = new Texture2D(picWidth, picHeight, TextureFormat.RGBA32, false);
            GetComponent<Renderer>().material.mainTexture = tex;
        }
        tex.LoadRawTextureData(data);
        tex.Apply();
    }

    void OnDisable() {
        StopListening();
    }

    private void OnApplicationQuit() {
        StopListening();
    }
}
