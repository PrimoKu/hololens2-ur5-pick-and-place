using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
#endif

public class TcpClient : MonoBehaviour
{

    public Text TCPStatus;
    public MainController mainController;
    private PoseMsg pose;

    #region Unity Functions

    private void Awake()
    {
        ConnectionStatusLED.material.color = Color.red;
        TCPStatus.text = $"Waiting to connect...";
    }
    private void OnApplicationFocus(bool focus)
    {
        if (!focus)
        {
#if WINDOWS_UWP
            StopConnection();
#endif
        }
    }
    #endregion

    [SerializeField]
    string hostIPAddress, port;

    public Renderer ConnectionStatusLED;
    private bool connected = false;
    public bool Connected
    {
        get { return connected; }
    }

#if WINDOWS_UWP
    StreamSocket socket = null;
    public DataWriter dw;
    public DataReader dr;
    private async void StartConnection()
    {
        if (socket != null)
        {
            socket.Dispose();
            ConnectionStatusLED.material.color = Color.red;
        }
        Debug.Log("Connecting to " + hostIPAddress);
        try
        {
            socket = new StreamSocket();
            var hostName = new Windows.Networking.HostName(hostIPAddress);
            await socket.ConnectAsync(hostName, port);
            dw = new DataWriter(socket.OutputStream);
            dr = new DataReader(socket.InputStream);
            dr.InputStreamOptions = InputStreamOptions.Partial;
            connected = true;
            ConnectionStatusLED.material.color = Color.green;
        }
        catch (Exception ex)
        {
            TCPStatus.text = $"Error sending: {ex}";
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
        TCPStatus.text = $"Connected to Server";
    }

    private void StopConnection()
    {
        dw?.DetachStream();
        dw?.Dispose();
        dw = null;

        dr?.DetachStream();
        dr?.Dispose();
        dr = null;

        socket?.Dispose();
        connected = false;
        ConnectionStatusLED.material.color = Color.red;
    }

    bool lastMessageSent = true;
    public async void SendString(string type)
    {
        if (!lastMessageSent) return;
        lastMessageSent = false;
        
        switch(type) {
            case "Marker":
                pose = mainController.MarkerPose();
                break;
            case "Pick":
                pose = mainController.PickPose();
                break;
            case "Place":
                pose = mainController.PlacePose();
                break;
            default:
                break;
        }

        if(pose == null) {
            TCPStatus.text = $"NULL Data";
            lastMessageSent = true;
            return;
        }

        try
        {
            string message = pose.ToJson();
            byte[] data = Encoding.UTF8.GetBytes(message);
            dw.WriteBytes(data);
            await dw.StoreAsync();
            await dw.FlushAsync();
        }
        catch (Exception ex)
        {
            SocketErrorStatus webErrorStatus = SocketError.GetStatus(ex.GetBaseException().HResult);
            TCPStatus.text = $"Error send: {ex}";
            Debug.Log(webErrorStatus.ToString() != "Unknown" ? webErrorStatus.ToString() : ex.Message);
        }
        lastMessageSent = true;
        TCPStatus.text = $"Data Send Successed";
    }

#endif

    #region Button Callback
    public void ConnectToServerEvent()
    {
#if WINDOWS_UWP
        if (!connected) StartConnection();
        else StopConnection();
#endif
    }

    public void SendMarkerPoseMsg() {
#if WINDOWS_UWP
        if (!connected) {
            TCPStatus.text = $"Connect to server first";
        }
        else SendString("Marker");
#endif
    }

    public void SendPickPoseMsg() {
#if WINDOWS_UWP
        if (!connected) {
            TCPStatus.text = $"Connect to server first";
        }
        else SendString("Pick");
#endif
    }

    public void SendPlacePoseMsg() {
#if WINDOWS_UWP
        if (!connected) {
            TCPStatus.text = $"Connect to server first";
        }
        else SendString("Place");
#endif
    }

    #endregion
}
