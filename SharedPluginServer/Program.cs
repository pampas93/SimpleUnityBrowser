#define USE_ARGS

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using MessageLibrary;
using SharedPluginServer.Interprocess;
using Xilium.CefGlue;

namespace SharedPluginServer
{

    //Main application

    public class App
    {
        private static readonly log4net.ILog log =
 log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


      

        private bool _enableWebRtc = false;

        private SharedMemServer _memServer;

        //SharedMem comms
        private SharedCommServer _inCommServer;
        private SharedCommServer _outCommServer;

        private CefWorker _mainWorker;

        private System.Windows.Forms.Timer _exitTimer;

        public bool IsRunning;

        /// <summary>
        /// App constructor
        /// </summary>
        /// <param name="worker">Main CEF worker</param>
        /// <param name="memServer">Shared memory file</param>
        /// <param name="commServer">TCP server</param>
       // public App(CefWorker worker, SharedMemServer memServer, SocketServer commServer,bool enableWebRtc)
        public App(CefWorker worker, SharedMemServer memServer, SharedCommServer inServer,SharedCommServer outServer, bool enableWebRtc)
        {
        //    _renderProcessHandler = new WorkerCefRenderProcessHandler();
            _enableWebRtc = enableWebRtc;

            _memServer = memServer;
            _mainWorker = worker;
           //init SharedMem comms
            _inCommServer = inServer;
            _outCommServer = outServer;

            _mainWorker.SetMemServer(_memServer);

            //attach dialogs and queries
            _mainWorker.OnJSDialog += _mainWorker_OnJSDialog;
            _mainWorker.OnBrowserJSQuery += _mainWorker_OnBrowserJSQuery;

            //attach page events
            _mainWorker.OnPageLoaded += _mainWorker_OnPageLoaded;

           

            IsRunning = true;

           _exitTimer=new Timer();
            _exitTimer.Interval = 10000;
            _exitTimer.Tick += _exitTimer_Tick;
            _exitTimer.Start();
        }

        public void CheckMessage()
        {
            _outCommServer.PushMessages();

            EventPacket ep = _inCommServer.GetMessage();
            if (ep != null)
                HandleMessage(ep);
           
        }

     
        private void _mainWorker_OnPageLoaded(string url, int status)
        {
           // log.Info("Navigated to:"+url);

            GenericEvent msg = new GenericEvent()
            {
                NavigateUrl = url,
                GenericType = BrowserEventType.Generic,
                Type = GenericEventType.PageLoaded
            };

            EventPacket ep = new EventPacket
            {
                Event = msg,
                Type = BrowserEventType.Generic
            };

            _outCommServer.WriteMessage(ep);
        }

        //shut down by timer, in case of client crash/hang
        private void _exitTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                log.Info("Exiting by timer,timeout:"+_exitTimer.Interval);
                log.Info("==============SHUTTING DOWN==========");
             
                _mainWorker.Shutdown();

                _memServer.Dispose();

               
                _inCommServer.Dispose();
                _outCommServer.Dispose();

                IsRunning = false;
             

            }
            catch (Exception ex)
            {

                log.Info("Exception on shutdown:" + ex.StackTrace);
            }
        }

        private void _mainWorker_OnBrowserJSQuery(string query)
        {
            GenericEvent msg = new GenericEvent()
            {
                JsQuery = query,
                GenericType = BrowserEventType.Generic,
                Type = GenericEventType.JSQuery
            };

            EventPacket ep = new EventPacket
            {
                Event = msg,
                Type = BrowserEventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);

            _outCommServer.WriteBytes(mstr.GetBuffer());
        }

        private void _mainWorker_OnJSDialog(string message, string prompt, DialogEventType type)
        {
            DialogEvent msg = new DialogEvent()
            {
                DefaultPrompt = prompt,
                Message = message,
                Type = type,
                GenericType = BrowserEventType.Dialog
            };

            EventPacket ep = new EventPacket
            {
                Event = msg,
                Type = BrowserEventType.Dialog
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);

            _outCommServer.WriteBytes(mstr.GetBuffer());
        }

        /// <summary>
        /// Main message handler
        /// </summary>
        /// <param name="msg">Message from client app</param>
        public void HandleMessage(EventPacket msg)
        {

            //reset timer
               _exitTimer.Stop();
              _exitTimer.Start();

         

            switch (msg.Type)
            {
                case BrowserEventType.Ping:
                {
                 
                        break;
                }

                case BrowserEventType.Generic:
                {
                    GenericEvent genericEvent=msg.Event as GenericEvent;
                    if (genericEvent != null)
                    {
                        switch (genericEvent.Type)
                        {
                             case GenericEventType.Shutdown:
                            {
                                try
                                {
                                    log.Info("==============SHUTTING DOWN==========");
                                   
                                       _mainWorker.Shutdown();
                                    
                                     _memServer.Dispose();

                                           
                                            _outCommServer.Dispose();
                                            _inCommServer.Dispose();

                                            IsRunning = false;
                                          

                                        }
                                catch (Exception e)
                                {

                                    log.Info("Exception on shutdown:"+e.StackTrace);
                                }

                                break;
                            }
                               case GenericEventType.Navigate:
                                    
                                    _mainWorker.Navigate(genericEvent.NavigateUrl);
                                break;

                                case GenericEventType.GoBack:
                                    _mainWorker.GoBack();
                                break;

                                case GenericEventType.GoForward:
                                        _mainWorker.GoForward();
                                break;

                                case GenericEventType.ExecuteJS:
                                    _mainWorker.ExecuteJavaScript(genericEvent.JsCode);
                                break;
                               
                            case GenericEventType.JSQueryResponse:
                            {
                                        _mainWorker.AnswerQuery(genericEvent.JsQueryResponse);
                             break;   
                            }
                               
                        }
                    }
                    break;
                }

                case BrowserEventType.Dialog:
                {
                        DialogEvent de=msg.Event as DialogEvent;
                    if (de != null)
                    {
                        _mainWorker.ContinueDialog(de.success,de.input);
                    }
                    break;
                    
                }

                case BrowserEventType.Keyboard:
                {
                        KeyboardEvent keyboardEvent=msg.Event as KeyboardEvent;

                      

                    if (keyboardEvent != null)
                    {
                        if (keyboardEvent.Type != KeyboardEventType.Focus)
                            _mainWorker.KeyboardEvent(keyboardEvent.Key, keyboardEvent.Type);
                        else
                            _mainWorker.FocusEvent(keyboardEvent.Key);

                    }
                    break;
                }
                case BrowserEventType.Mouse:
                    {
                        MouseMessage mouseMessage=msg.Event as MouseMessage;
                        if (mouseMessage != null)
                        {
                          
                            switch (mouseMessage.Type)
                            {
                                case MouseEventType.ButtonDown:
                                    _mainWorker.MouseEvent(mouseMessage.X, mouseMessage.Y, false,mouseMessage.Button);
                                    break;
                                case MouseEventType.ButtonUp:
                                    _mainWorker.MouseEvent(mouseMessage.X, mouseMessage.Y, true,mouseMessage.Button);
                                    break;
                                case MouseEventType.Move:
                                    _mainWorker.MouseMoveEvent(mouseMessage.X, mouseMessage.Y, mouseMessage.Button);
                                    break;
                                    case MouseEventType.Leave:
                                    _mainWorker.MouseLeaveEvent();
                                    break;
                                    case MouseEventType.Wheel:
                                    _mainWorker.MouseWheelEvent(mouseMessage.X,mouseMessage.Y,mouseMessage.Delta);
                                    break;
                            }
                        }

                        break;
                    }
            }

           
        }

     
    }

    


    static class Program
    {
        private static readonly log4net.ILog log =
log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);



        /// <summary>
        /// The main entry point for the application.
        /// args:
        /// width,
        /// height,
        /// initialURL,
        /// memory file name,
        /// in memory comm file name,
        /// out memory comm file name,
        /// WebRTC?1:0
        /// Enable GPU? 1:0
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
           

            log.Info("===============START================");

            //////// CEF RUNTIME
            try
            {
                CefRuntime.Load();
            }
            catch (DllNotFoundException ex)
            {
                log.ErrorFormat("{0} error", ex.Message);
            }
            catch (CefRuntimeException ex)
            {
                log.ErrorFormat("{0} error", ex.Message);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("{0} error", ex.Message);

            }

           


            int defWidth = 1280;
            int defHeight = 720;
            string defUrl = "http://test.webrtc.org";
            string defFileName = "MainSharedMem";

            string defInFileName = "InSharedMem";
            string defOutFileName = "OutSharedMem";

            bool useWebRTC = false;

            bool EnableGPU = false;

            if (args.Length>0&&args[0] != "--type=renderer")
            {
               

                if (args.Length > 1)
                {
                    defWidth = Int32.Parse(args[0]);
                    defHeight = Int32.Parse(args[1]);
                }
                if (args.Length > 2)
                    defUrl = args[2];
                if (args.Length > 3)
                    defFileName = args[3];
                if (args.Length > 4)
                    defInFileName = args[4];
                if (args.Length > 5)
                    defOutFileName = args[5];
                if (args.Length>6)
                    if (args[6] == "1")
                        useWebRTC = true;
                if (args.Length > 7)
                    if (args[7] == "1")
                        EnableGPU = true;
            }

            log.InfoFormat("Starting plugin, settings:width:{0},height:{1},url:{2},memfile:{3},inMem:{4},outMem:{5}, WebRtc:{6},Enable GPU:{7}",
                defWidth, defHeight, defUrl, defFileName,defInFileName,defOutFileName, useWebRTC,EnableGPU);

            try
            {

             CefMainArgs cefMainArgs;
                cefMainArgs = new CefMainArgs(args);
             var cefApp = new WorkerCefApp(useWebRTC,EnableGPU);

             

             int exit_code = CefRuntime.ExecuteProcess(cefMainArgs, cefApp,IntPtr.Zero);

            if ( exit_code>=0)
            {
                    log.ErrorFormat("CefRuntime return "+exit_code);
                    return exit_code;
            }
            var cefSettings = new CefSettings
            {
                SingleProcess = false,
                MultiThreadedMessageLoop = true,
                WindowlessRenderingEnabled = true,
                LogSeverity = CefLogSeverity.Info,

            };



            try
            {
                    
              CefRuntime.Initialize(cefMainArgs, cefSettings, cefApp, IntPtr.Zero);
                   
            }
            catch (CefRuntimeException ex)
            {
                log.ErrorFormat("{0} error", ex.Message);

            }
                /////////////
            }
            catch (Exception ex)
            {
                log.Info("EXCEPTION ON CEF INITIALIZATION:"+ex.Message+"\n"+ex.StackTrace);
                throw;
            }



                CefWorker worker = new CefWorker();
                worker.Init(defWidth, defHeight, defUrl);

                SharedMemServer server = new SharedMemServer();
                server.Init(defWidth * defHeight * 4, defFileName);


          
            SharedCommServer inSrv = new SharedCommServer(false);

            //TODO: the sizes may vary, but 10k should be enough?
            inSrv.InitComm(10000, defInFileName);

            SharedCommServer outSrv = new SharedCommServer(true);
            outSrv.InitComm(10000, defOutFileName);

            var app = new App(worker, server, inSrv, outSrv, false);

          
           while(app.IsRunning)
            {
                Application.DoEvents();
                //check incoming messages and push outcoming
                app.CheckMessage();
            }
          

            CefRuntime.Shutdown();

            return 0;

        }
    }
}
