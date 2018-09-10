using Xilium.CefGlue;

namespace SharedPluginServer
{
    class WorkerCefApp : CefApp
    {
        private readonly WorkerCefRenderProcessHandler _renderProcessHandler;

        private bool _enableWebRtc = false;

        private bool _enableGPU = false;

        public WorkerCefApp(bool enableWebRtc,bool enableGPU)
        {
            _renderProcessHandler=new WorkerCefRenderProcessHandler();
            _enableWebRtc = enableWebRtc;

            _enableGPU = enableGPU;
        }

        protected override CefRenderProcessHandler GetRenderProcessHandler()
        {
            return _renderProcessHandler;
        }





        //GPU and others
        protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
        {
            if (string.IsNullOrEmpty(processType))
            {
                // commandLine.AppendSwitch("enable-webrtc");
                if (!_enableGPU)
                {
                    commandLine.AppendSwitch("disable-gpu");
                    commandLine.AppendSwitch("disable-gpu-compositing");
                }
                commandLine.AppendSwitch("enable-begin-frame-scheduling");
                commandLine.AppendSwitch("disable-smooth-scrolling");
               if (_enableWebRtc)
                {
                    commandLine.AppendSwitch("enable-media-stream", "true");
                   
                }

            }
            //commandLine.AppendArgument("--enable-media-stream");
        }
    }
}