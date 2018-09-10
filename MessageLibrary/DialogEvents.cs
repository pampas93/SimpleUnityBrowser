using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace MessageLibrary
{

    public enum DialogEventType
    {
        Alert = 0,
        Confirm = 1,
        Prompt = 2

    }

    //JavaScript dialogs
    [Serializable]
    public class DialogEvent : AbstractEvent
    {
        public DialogEventType Type;
        public string Message;
        public string DefaultPrompt;
        //reply
        public bool success;
        public string input;

        /*protected override bool Compare(AbstractEvent ev2)
        {
            DialogEvent ge = ev2 as DialogEvent;

            return (Type==ge.Type&&Message==ge.Message&&DefaultPrompt==ge.DefaultPrompt&&success==ge.success&&input==ge.input);
        }*/
    }
}
