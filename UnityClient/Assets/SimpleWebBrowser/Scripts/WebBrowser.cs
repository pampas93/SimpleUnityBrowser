using System;
using UnityEngine;
using System.Collections;
using System.Text;
//using System.Diagnostics;
using MessageLibrary;
using UnityEngine.UI;
using System.Collections.Generic;

namespace SimpleWebBrowser
{




    public class WebBrowser : MonoBehaviour
    {

        #region General

        [Header("General settings")] public int Width = 1024;

        public int Height = 768;

        public string MemoryFile = "MainSharedMem";

        public bool RandomMemoryFile = true;


        public string InitialURL = "http://www.google.com";

        public bool EnableWebRTC = false;

        [Header("Testing")]
        public bool EnableGPU = false;

        [Multiline]
        public string JSInitializationCode = "";

        #endregion



        [Header("UI settings")]
        [SerializeField]
        public BrowserUI mainUIPanel;

        public bool KeepUIVisible = false;

        public bool UIEnabled = true;
       public Camera MainCamera;

        [Header("Dialog settings")] [SerializeField] public bool DialogEnabled = false;

        [SerializeField]
        public Canvas DialogCanvas;
        [SerializeField]
        public Text DialogText;
        [SerializeField]
        public Button OkButton;
        [SerializeField]
        public Button YesButton;
        [SerializeField]
        public Button NoButton;
        [SerializeField]
        public InputField DialogPrompt;

        //dialog states - threading
        private bool _showDialog = false;
        private string _dialogMessage = "";
        private string _dialogPrompt = "";
        private DialogEventType _dialogEventType;
        //query - threading
        private bool _startQuery = false;
        private string _jsQueryString = "";
        
        //status - threading
        private bool _setUrl = false;
        private string _setUrlString = "";
       

        #region JS Query events

        public delegate void JSQuery(string query);

        public event JSQuery OnJSQuery;

        #endregion


        private Material _mainMaterial;





        private BrowserEngine _mainEngine;



        private bool _focused = false;


        private int posX = 0;
        private int posY = 0;


        //why Unity does not store the links in package?
        void InitPrefabLinks()
        {
            if (mainUIPanel == null)
                mainUIPanel = gameObject.transform.Find("MainUI").gameObject.GetComponent<BrowserUI>();
            if (DialogEnabled)
            {
                if (DialogCanvas == null)
                    DialogCanvas = gameObject.transform.Find("MessageBox").gameObject.GetComponent<Canvas>();
            if (DialogText == null)
                DialogText = DialogCanvas.transform.Find("MessageText").gameObject.GetComponent<Text>();
            if (OkButton == null)
                OkButton = DialogCanvas.transform.Find("OK").gameObject.GetComponent<Button>();
            if (YesButton == null)
                YesButton = DialogCanvas.transform.Find("Yes").gameObject.GetComponent<Button>();
            if (NoButton == null)
                NoButton = DialogCanvas.transform.Find("No").gameObject.GetComponent<Button>();
            if (DialogPrompt == null)
                DialogPrompt = DialogCanvas.transform.Find("Prompt").gameObject.GetComponent<InputField>();

    }
  }

        void Start()
        {
            _mainEngine = new BrowserEngine();

            if (RandomMemoryFile)
            {
                Guid memid = Guid.NewGuid();
                MemoryFile = memid.ToString();
            }
           



  StartCoroutine(          _mainEngine.InitPlugin(Width, Height, MemoryFile, InitialURL,EnableWebRTC,EnableGPU));
            //run initialization
            if (JSInitializationCode.Trim() != "")
                _mainEngine.RunJSOnce(JSInitializationCode);

            //
            if (UIEnabled)
            {
                InitPrefabLinks();
                mainUIPanel.InitPrefabLinks();
            }

            if (MainCamera == null)
            {
                MainCamera = Camera.main;
                if (MainCamera == null)
                    Debug.LogError("Error: can't find main camera");
            }

            _mainMaterial = GetComponent<MeshRenderer>().material;
            _mainMaterial.SetTexture("_MainTex", _mainEngine.BrowserTexture);
            _mainMaterial.SetTextureScale("_MainTex", new Vector2(-1, 1));

            
            if(UIEnabled)
            mainUIPanel.MainCanvas.worldCamera = MainCamera;





              // _mainInput = MainUrlInput.GetComponent<Input>();
            if (UIEnabled)
            {
                mainUIPanel.KeepUIVisible = KeepUIVisible;
                if (!KeepUIVisible)
                    mainUIPanel.Hide();
            }

            //attach dialogs and querys
            _mainEngine.OnJavaScriptDialog += _mainEngine_OnJavaScriptDialog;
            _mainEngine.OnJavaScriptQuery += _mainEngine_OnJavaScriptQuery;
_mainEngine.OnPageLoaded += _mainEngine_OnPageLoaded;

            if (DialogEnabled)
            {
                DialogCanvas.worldCamera = MainCamera;
                DialogCanvas.gameObject.SetActive(false);
            }

        }

        private void _mainEngine_OnPageLoaded(string url)
        {
            _setUrl = true;
            _setUrlString = url;
           
        }

        //make it thread-safe
        private void _mainEngine_OnJavaScriptQuery(string message)
        {
            _jsQueryString = message;
            _startQuery = true;
        }

        public void RespondToJSQuery(string response)
        {
            _mainEngine.SendQueryResponse(response);
        }

        private void _mainEngine_OnJavaScriptDialog(string message, string prompt, DialogEventType type)
        {
            _showDialog = true;
            _dialogEventType = type;
            _dialogMessage = message;
            _dialogPrompt = prompt;

        }

         private void ShowDialog()
        {
            if (DialogEnabled)
            {
                switch (_dialogEventType)
                {
                case DialogEventType.Alert:
                {
                    DialogCanvas.gameObject.SetActive(true);
                    OkButton.gameObject.SetActive(true);
                    YesButton.gameObject.SetActive(false);
                    NoButton.gameObject.SetActive(false);
                    DialogPrompt.text = "";
                    DialogPrompt.gameObject.SetActive(false);
                    DialogText.text = _dialogMessage;
                    break;
                }
                case DialogEventType.Confirm:
                {
                    DialogCanvas.gameObject.SetActive(true);
                    OkButton.gameObject.SetActive(false);
                    YesButton.gameObject.SetActive(true);
                    NoButton.gameObject.SetActive(true);
                    DialogPrompt.text = "";
                    DialogPrompt.gameObject.SetActive(false);
                    DialogText.text = _dialogMessage;
                    break;
                }
                case DialogEventType.Prompt:
                {
                    DialogCanvas.gameObject.SetActive(true);
                    OkButton.gameObject.SetActive(false);
                    YesButton.gameObject.SetActive(true);
                    NoButton.gameObject.SetActive(true);
                    DialogPrompt.text = _dialogPrompt;
                    DialogPrompt.gameObject.SetActive(true);
                    DialogText.text = _dialogMessage;
                    break;
                }
            }
                _showDialog = false;
            }
        }

        #region UI

        public void OnNavigate()
        {
            // MainUrlInput.isFocused
            _mainEngine.SendNavigateEvent(mainUIPanel.UrlField.text, false, false);

        }

        public void RunJavaScript(string js)
        {
            _mainEngine.SendExecuteJSEvent(js);
        }

        public void GoBackForward(bool forward)
        {
            if (forward)
                _mainEngine.SendNavigateEvent("", false, true);
            else
                _mainEngine.SendNavigateEvent("", true, false);
        }

        #endregion

        #region Dialogs

        public void DialogResult(bool result)
        {
            if (DialogEnabled)
            {
                DialogCanvas.gameObject.SetActive(false);
                _mainEngine.SendDialogResponse(result, DialogPrompt.text);
            }

        }

        #endregion


        #region Events (3D)

        void OnMouseEnter()
        {
            _focused = true;
            if(UIEnabled)
            mainUIPanel.Show();
        }

        void OnMouseExit()
        {
            _focused = false;
           if(UIEnabled)
            mainUIPanel.Hide();
        }

        void OnMouseDown()
        {

            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Left, MouseEventType.ButtonDown);

                }
            }

        }




        void OnMouseUp()
        {
            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                   SendMouseButtonEvent((int) pixelUV.x, (int) pixelUV.y, MouseButton.Left, MouseEventType.ButtonUp);
                }
            }
        }

        void OnMouseOver()
        {
            if (_mainEngine.Initialized)
            {
                Vector2 pixelUV = GetScreenCoords();

                if (pixelUV.x > 0)
                {
                    int px = (int) pixelUV.x;
                    int py = (int) pixelUV.y;

                    ProcessScrollInput(px, py);

                    if (posX != px || posY != py)
                    {
                        MouseMessage msg = new MouseMessage
                        {
                            Type = MouseEventType.Move,
                            X = px,
                            Y = py,
                            GenericType = MessageLibrary.BrowserEventType.Mouse,
                            // Delta = e.Delta,
                            Button = MouseButton.None
                        };

                        if (Input.GetMouseButton(0))
                            msg.Button = MouseButton.Left;
                        if (Input.GetMouseButton(1))
                            msg.Button = MouseButton.Right;
                        if (Input.GetMouseButton(1))
                            msg.Button = MouseButton.Middle;

                        posX = px;
                        posY = py;
                        _mainEngine.SendMouseEvent(msg);
                    }

                    //check other buttons...
                    if (Input.GetMouseButtonDown(1))
                       SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonDown);
                    if (Input.GetMouseButtonUp(1))
                       SendMouseButtonEvent(px, py, MouseButton.Right, MouseEventType.ButtonUp);
                    if (Input.GetMouseButtonDown(2))
                        SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonDown);
                    if (Input.GetMouseButtonUp(2))
                        SendMouseButtonEvent(px, py, MouseButton.Middle, MouseEventType.ButtonUp);
                }
            }

            // Debug.Log(pixelUV);
        }

        #endregion

        #region Helpers

        private Vector2 GetScreenCoords()
        {


            RaycastHit hit;
            if (
                !Physics.Raycast(
                    MainCamera.ScreenPointToRay(Input.mousePosition), out hit))
                return new Vector2(-1f, -1f);
            Texture tex = _mainMaterial.mainTexture;


            Vector2 pixelUV = hit.textureCoord;
            pixelUV.x = (1 - pixelUV.x)*tex.width;
            pixelUV.y *= tex.height;
            return pixelUV;




        }

       void SendMouseButtonEvent(int x, int y, MouseButton btn, MouseEventType type)
        {
           
            MouseMessage msg = new MouseMessage
            {
                Type = type,
                X = x,
                Y = y,
                GenericType = MessageLibrary.BrowserEventType.Mouse,
                // Delta = e.Delta,
                Button = btn
            };
            _mainEngine.SendMouseEvent(msg);
        }

        private void ProcessScrollInput(int px, int py)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            scroll = scroll*_mainEngine.BrowserTexture.height;

            int scInt = (int) scroll;

            if (scInt != 0)
            {
                MouseMessage msg = new MouseMessage
                {
                    Type = MouseEventType.Wheel,
                    X = px,
                    Y = py,
                    GenericType = MessageLibrary.BrowserEventType.Mouse,
                    Delta = scInt,
                    Button = MouseButton.None
                };

                if (Input.GetMouseButton(0))
                    msg.Button = MouseButton.Left;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Right;
                if (Input.GetMouseButton(1))
                    msg.Button = MouseButton.Middle;

                _mainEngine.SendMouseEvent(msg);
            }
        }

        #endregion

        private void FixedUpdate()
        {
            _mainEngine.PushMessages(); //
        }

        // Update is called once per frame
        void Update()
        {

            _mainEngine.UpdateTexture();

           

            //Dialog
            if (_showDialog)
            {
                ShowDialog();
            }

            //Query
            if (_startQuery)
            {
                _startQuery = false;
                if (OnJSQuery != null)
                    OnJSQuery(_jsQueryString);

            }

            //Status
            if (_setUrl)
            {
                _setUrl = false;
                if(UIEnabled)
                mainUIPanel.UrlField.text = _setUrlString;

            }

if (UIEnabled)
            {


            if (_focused && !mainUIPanel.UrlField.isFocused) //keys
            {
                foreach (char c in Input.inputString)
                {

                    _mainEngine.SendCharEvent((int) c, KeyboardEventType.CharKey);
                }
                ProcessKeyEvents();
             }




            }


            _mainEngine.CheckMessage();

        }

        #region Keys

        private void ProcessKeyEvents()
        {
            foreach (KeyCode k in Enum.GetValues(typeof (KeyCode)))
            {
                CheckKey(k);
            }

        }

        private void CheckKey(KeyCode code)
        {
            if (Input.GetKeyDown(code))
                _mainEngine.SendCharEvent((int) code, KeyboardEventType.Down);
            if (Input.GetKeyUp(KeyCode.Backspace))
                _mainEngine.SendCharEvent((int) code, KeyboardEventType.Up);
        }

        #endregion

        void OnDisable()
        {
            _mainEngine.Shutdown();
        }


        public event BrowserEngine.PageLoaded OnPageLoaded;
    }
}