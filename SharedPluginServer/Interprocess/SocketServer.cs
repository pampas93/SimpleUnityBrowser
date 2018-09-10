using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MessageLibrary;



namespace SharedPluginServer.Interprocess
{
    /*public  class SocketServer
     {

         private static readonly log4net.ILog log =
  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



         public  int MainPort = 8885;
         private TcpListener _listener;

         private Thread listenerThread;

         public delegate void ReceivedMessage(EventPacket msg);

        private static volatile bool isWorking = true;

         public static event ReceivedMessage OnReceivedMessage;

        public UserConnection Client;

         public void Init(int port)
         {

             listenerThread = new Thread(new ThreadStart(ListenCallback));
             listenerThread.IsBackground = true;
             listenerThread.Start();
             MainPort = port;
         }

        private void ListenCallback()
        {
             try
             {
                 _listener = new TcpListener(System.Net.IPAddress.Any, MainPort);
                 _listener.Start();

                 do
                 {
                    DoBeginAcceptTcpClient(_listener,this);

                 } while (isWorking);

                 _listener.Stop();
             }
             catch (Exception) //we have: A blocking operation was interrupted by a call to WSACancelBlockingCall
             {


             }
         }

         #region async stuff

         //helper
        class DataPass
        {
            public TcpListener _listener;
            public SocketServer _server;
        }

         // Thread signal.
         public static ManualResetEvent tcpClientConnected =
     new ManualResetEvent(false);

         // Accept one client connection asynchronously.
         public static void DoBeginAcceptTcpClient(TcpListener
             listener,SocketServer instServer)
         {
             // Set the event to nonsignaled state.
             tcpClientConnected.Reset();


             // Accept the connection. 
             // BeginAcceptSocket() creates the accepted socket.
             listener.BeginAcceptTcpClient(
                 new AsyncCallback(DoAcceptTcpClientCallback),
                 //listener);
                 new DataPass() {_listener = listener, _server = instServer});

             // Wait until a connection is made and processed before 
             // continuing.
             tcpClientConnected.WaitOne();
         }

         // Process the client connection.
         public static void DoAcceptTcpClientCallback(IAsyncResult ar)
         {
             if (isWorking)
             {

                 DataPass data=(DataPass)ar.AsyncState;
                 // Get the listener that handles the client request.
                 // TcpListener listener = (TcpListener) ar.AsyncState;
                 TcpListener listener = data._listener;
                 SocketServer srv = data._server;
                 // End the operation and display the received data on 
                 // the console.
                 TcpClient client = listener.EndAcceptTcpClient(ar);

                  srv.Client = new UserConnection(client);

                 srv.Client.OnLineReceived += OnLineReceived;
                 // Signal the calling thread to continue.
                 tcpClientConnected.Set();
             }

         }
         #endregion

         private static void OnLineReceived(UserConnection sender, byte[] data)
        {
            try
            {
                 MemoryStream mstr = new MemoryStream(data);
                 BinaryFormatter bf = new BinaryFormatter();
                 EventPacket ep=bf.Deserialize(mstr) as EventPacket;
                if (ep != null)
                {
                    OnReceivedMessage?.Invoke(ep);
                }
             }
            catch (Exception ex)
            {
                log.Error("Exception in receive:"+ex.Message );
            }
        }




        public void Shutdown()
        {
            isWorking = false;
            tcpClientConnected.Set();
        }

        ~SocketServer()
        {
           Shutdown();  
        }
     }

 #region client
     public delegate void LineReceive(UserConnection sender, byte[] Data);

     public class UserConnection
     {
         const int READ_BUFFER_SIZE = 2048;
         public UserConnection(TcpClient client)
         {
             this.client = client;
             this.client.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
         }

         private TcpClient client;
         private byte[] readBuffer = new byte[READ_BUFFER_SIZE];

         public event LineReceive OnLineReceived;


         public void SendData(byte[] Data)
         {
             //lock ensure that no other threads try to use the stream at the same time.
             lock (client.GetStream())
             {
                 client.GetStream().Write(Data, 0, Data.Length);
             }
         }



         private void StreamReceiver(IAsyncResult ar)
         {
             int BytesRead;

             try
             {
                 lock (client.GetStream())
                 {
                     // Finish asynchronous read into readBuffer and get number of bytes read.
                     BytesRead = client.GetStream().EndRead(ar);
                 }

                 OnLineReceived?.Invoke(this, readBuffer);
                 lock (client.GetStream())
                 {
                     // Start a new asynchronous read into readBuffer.
                     client.GetStream()
                         .BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
                 }
             }
             catch (Exception e)
             {
             }
         }


     }
     #endregion*/
}
