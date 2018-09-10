using System;
using System.Diagnostics;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MessageLibrary;
using SharedMemory;

using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace SimpleWebBrowser
{



    public class BrowserEngine
    {

        private SharedArray<byte> _mainTexArray;

        private SharedCommServer _inCommServer;
        private SharedCommServer _outCommServer;

        private Process _pluginProcess;

        private static Object sPixelLock;

        public Texture2D BrowserTexture = null;
        public bool Initialized = false;


        private bool _needToRunOnce = false;
        private string _runOnceJS = "";

        //Image buffer
        private byte[] _bufferBytes = null;
        private long _arraySize = 0;

        private bool _connected = false;


        #region Status events

        public delegate void PageLoaded(string url);

        public event PageLoaded OnPageLoaded;

        #endregion


        #region Settings

        public int kWidth = 512;
        public int kHeight = 512;

        private string _sharedFileName;

        //comm files
        private string _inCommFile;
        private string _outCommFile;

        private string _initialURL;
        private bool _enableWebRTC;
        private bool _enableGPU;

        #endregion

        #region Dialogs

        public delegate void JavaScriptDialog(string message, string prompt, DialogEventType type);

        public event JavaScriptDialog OnJavaScriptDialog;

        #endregion

        #region JSQuery

        public delegate void JavaScriptQuery(string message);

        public event JavaScriptQuery OnJavaScriptQuery;

        #endregion



        #region Init

        //A really hackish way to avoid thread error. Should be better way
      /*  public bool ConnectTcp(out TcpClient tcp)
        {
            TcpClient ret = null;
            try
            {
                ret = new TcpClient("127.0.0.1", _port);
            }
            catch (Exception ex)
            {
                tcp = null;
                return false;
            }

            tcp = ret;
            return true;

        }*/


        public IEnumerator InitPlugin(int width, int height, string sharedfilename,string initialURL,bool enableWebRTC,bool enableGPU)
        {

            //Initialization (for now) requires a predefined path to PluginServer,
            //so change this section if you move the folder
            //Also change the path in deployment script.

#if UNITY_EDITOR_64
            string PluginServerPath = Application.dataPath + @"\SimpleWebBrowser\PluginServer\x64";
#else
#if UNITY_EDITOR_32
        string PluginServerPath = Application.dataPath + @"\SimpleWebBrowser\PluginServer\x86";
#else


        //HACK
        string AssemblyPath=System.Reflection.Assembly.GetExecutingAssembly().Location;
        //log this for error handling
        Debug.Log("Assembly path:"+AssemblyPath);

        AssemblyPath = Path.GetDirectoryName(AssemblyPath); //Managed
      
        AssemblyPath = Directory.GetParent(AssemblyPath).FullName; //<project>_Data
        AssemblyPath = Directory.GetParent(AssemblyPath).FullName;//required

        string PluginServerPath=AssemblyPath+@"\PluginServer";
#endif
#endif



            Debug.Log("Starting server from:" + PluginServerPath);

            kWidth = width;
            kHeight = height;



            _sharedFileName = sharedfilename;

            //randoms
            Guid inID = Guid.NewGuid();
            _outCommFile = inID.ToString();

            Guid outID = Guid.NewGuid();
            _inCommFile = outID.ToString();

            _initialURL = initialURL;
            _enableWebRTC = enableWebRTC;
            _enableGPU = enableGPU;

              if (BrowserTexture == null)
                BrowserTexture = new Texture2D(kWidth, kHeight, TextureFormat.BGRA32, false, true);



            sPixelLock = new object();


            string args = BuildParamsString();

           _connected = false;

            _inCommServer = new SharedCommServer(false);
            _outCommServer = new SharedCommServer(true);

            while (!_connected)
            {
                try
                {
                    _pluginProcess = new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            WorkingDirectory = PluginServerPath,
                            FileName = PluginServerPath + @"\SharedPluginServer.exe",
                            Arguments = args

                        }
                    };



                    _pluginProcess.Start();
                    Initialized = false;
                }
                catch (Exception ex)
                {
                    //log the file
                    Debug.Log("FAILED TO START SERVER FROM:" + PluginServerPath + @"\SharedPluginServer.exe");
                    throw;
                }
                yield return new WaitForSeconds(1.0f);
               //connected = ConnectTcp(out _clientSocket);

                _inCommServer.Connect(_inCommFile);
                bool b1 = _inCommServer.GetIsOpen();
                _outCommServer.Connect(_outCommFile);
                bool b2 = _outCommServer.GetIsOpen();
                _connected = b1 && b2;
               
            }



        }

        private string BuildParamsString()
        {
            string ret = kWidth.ToString() + " " + kHeight.ToString() + " ";
            ret = ret + _initialURL + " ";
            ret = ret + _sharedFileName + " ";
            ret = ret + _outCommFile + " ";
            ret = ret + _inCommFile + " ";

            if (_enableWebRTC)
                ret = ret + " 1"+" ";
            else
                ret = ret + " 0"+" ";

            if(_enableGPU)
                ret = ret + " 1" + " ";
            else
                ret = ret + " 0" + " ";

            return ret;
        }

        #endregion



        #region SendEvents

        public void SendNavigateEvent(string url, bool back, bool forward)
        {
            if (Initialized)
            {
                GenericEvent ge = new GenericEvent()
                {
                    Type = GenericEventType.Navigate,
                    GenericType = MessageLibrary.BrowserEventType.Generic,
                    NavigateUrl = url
                };

                if (back)
                    ge.Type = GenericEventType.GoBack;
                else if (forward)
                    ge.Type = GenericEventType.GoForward;

                EventPacket ep = new EventPacket()
                {
                    Event = ge,
                    Type = MessageLibrary.BrowserEventType.Generic
                };

                /*MemoryStream mstr = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(mstr, ep);
                byte[] b = mstr.GetBuffer();
                _outCommServer.WriteBytes(b);*/
                _outCommServer.WriteMessage(ep);
            }
        }

        public void SendShutdownEvent()
        {
            if (Initialized)
            {
                GenericEvent ge = new GenericEvent()
                {
                    Type = GenericEventType.Shutdown,
                    GenericType = MessageLibrary.BrowserEventType.Generic
                };

                EventPacket ep = new EventPacket()
                {
                    Event = ge,
                    Type = MessageLibrary.BrowserEventType.Generic
                };

                _outCommServer.WriteMessage(ep);
            }
        }

        public void PushMessages()
        {
            if(Initialized)
            _outCommServer.PushMessages();
        }

       public void SendDialogResponse(bool ok, string dinput)
        {
            if (Initialized)
            {
                DialogEvent de = new DialogEvent()
                {
                    GenericType = MessageLibrary.BrowserEventType.Dialog,
                    success = ok,
                    input = dinput
                };

                EventPacket ep = new EventPacket
                {
                    Event = de,
                    Type = MessageLibrary.BrowserEventType.Dialog
                };

                _outCommServer.WriteMessage(ep);
            }
        }

        public void SendQueryResponse(string response)
        {
            if (Initialized)
            {
                GenericEvent ge = new GenericEvent()
                {
                    Type = GenericEventType.JSQueryResponse,
                    GenericType = BrowserEventType.Generic,
                    JsQueryResponse = response
                };

                EventPacket ep = new EventPacket()
                {
                    Event = ge,
                    Type = BrowserEventType.Generic
                };

                _outCommServer.WriteMessage(ep);
            }
        }

        public void SendCharEvent(int character, KeyboardEventType type)
        {
            if (Initialized)
            {
                KeyboardEvent keyboardEvent = new KeyboardEvent()
                {
                    Type = type,
                    Key = character
                };
                EventPacket ep = new EventPacket()
                {
                    Event = keyboardEvent,
                    Type = MessageLibrary.BrowserEventType.Keyboard
                };

                _outCommServer.WriteMessage(ep);
            }
        }

        public void SendMouseEvent(MouseMessage msg)
        {
            if (Initialized)
            {
                EventPacket ep = new EventPacket
                {
                    Event = msg,
                    Type = MessageLibrary.BrowserEventType.Mouse
                };

                _outCommServer.WriteMessage(ep);
            }

        }

        public void SendExecuteJSEvent(string js)
        {
            if (Initialized)
            {
                GenericEvent ge = new GenericEvent()
                {
                    Type = GenericEventType.ExecuteJS,
                    GenericType = BrowserEventType.Generic,
                    JsCode = js
                };

                EventPacket ep = new EventPacket()
                {
                    Event = ge,
                    Type = BrowserEventType.Generic
                };

                _outCommServer.WriteMessage(ep);
            }
        }

        public void SendPing()
       {
            if (Initialized)
            {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Navigate, //could be any
                GenericType = BrowserEventType.Ping,

            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = BrowserEventType.Ping
            };

            _outCommServer.WriteMessage(ep);
       }
        }


        #endregion


        #region Helpers

        /// <summary>
        /// Used to run JS on initialization, for example, to set CSS
        /// </summary>
        /// <param name="js">JS code</param>
       public void RunJSOnce(string js )
        {
            _needToRunOnce = true;
            _runOnceJS = js;
        }

        #endregion

        



     public void UpdateTexture()
        {

            if (Initialized)
            {


                UpdateInitialized();



                //execute run-once functions
                if (_needToRunOnce)
                {
                    SendExecuteJSEvent(_runOnceJS);
                    _needToRunOnce = false;
                }
            }
            else
            {
                //  yield return new WaitForSeconds(2);
                if(_connected)
                { 
               
                   
                    
                        //Thread.Sleep(200); //give it some time to initialize

                        try
                        {


                            //init memory file
                            _mainTexArray = new SharedArray<byte>(_sharedFileName);

                            Initialized = true;
                        }
                        catch (Exception ex)
                        {
                            //SharedMem and TCP exceptions
                            Debug.Log("Exception on init:" + ex.Message + ".Waiting for plugin server");
                        }

                }
               

            }
        }

        //Receiver
        public void CheckMessage()
        {

            if (Initialized)
            {
                try
                {
                    // Ensure that no other threads try to use the stream at the same time.
                    EventPacket ep = _inCommServer.GetMessage();


                    if (ep != null)
                    {
                        //main handlers
                        if (ep.Type == BrowserEventType.Dialog)
                        {
                            DialogEvent dev = ep.Event as DialogEvent;
                            if (dev != null)
                            {
                                if (OnJavaScriptDialog != null)
                                    OnJavaScriptDialog(dev.Message, dev.DefaultPrompt, dev.Type);
                            }
                        }
                        if (ep.Type == BrowserEventType.Generic)
                        {
                            GenericEvent ge = ep.Event as GenericEvent;
                            if (ge != null)
                            {
                                if (ge.Type == GenericEventType.JSQuery)
                                {
                                    if (OnJavaScriptQuery != null)
                                        OnJavaScriptQuery(ge.JsQuery);
                                }
                            }

                            if (ge.Type == GenericEventType.PageLoaded)
                            {
                                if (OnPageLoaded != null)
                                    OnPageLoaded(ge.NavigateUrl);
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.Log("Error reading from socket,waiting for plugin server to start...");
                }
            }
        }


        public void Shutdown()
        {
            SendShutdownEvent();
        }

        //////////Added//////////
        public void UpdateInitialized()
        {
            if (Initialized)
            {
                SendPing();

                if (_bufferBytes == null)
                {
                    long arraySize = _mainTexArray.Length;
                    Debug.Log("Memory array size:" + arraySize);
                    _bufferBytes = new byte[arraySize];
                }
                _mainTexArray.CopyTo(_bufferBytes, 0);

                lock (sPixelLock)
                {

                    BrowserTexture.LoadRawTextureData(_bufferBytes);
                    BrowserTexture.Apply();

                }
            }
        }
    }
   
}