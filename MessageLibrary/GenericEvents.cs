using System;



namespace MessageLibrary
{

    public enum BrowserEventType
    {
        Ping=-1,
        Generic=0,
        Mouse=1,
        Keyboard=2,
        Dialog = 3,
        StopPacket=4
    }

    [Serializable]
    public abstract class AbstractEvent
    {
        public BrowserEventType GenericType;//?

       /* protected abstract bool Compare(AbstractEvent ev2);

        public static bool operator !=(AbstractEvent ep1, AbstractEvent ep2)
        {
            if (ep1.GenericType != ep2.GenericType)
                return true;
            else
                return !ep1.Compare(ep2);
        }

        public static bool operator ==(AbstractEvent ep1, AbstractEvent ep2)
        {
            if (ep1.GenericType == ep2.GenericType)

                return ep1.Compare(ep2);
            else
                return false;
        }*/
    }

    [Serializable]
    public class EventPacket
    {
        public BrowserEventType Type;

        public AbstractEvent Event;

      /*  public static bool operator != (EventPacket ep1, EventPacket ep2)
        {
            return !(ep1.Type == ep2.Type && ep1.Event != ep2.Event);
        }

        public static bool operator ==(EventPacket ep1, EventPacket ep2)
        {
            return (ep1.Type == ep2.Type && ep1.Event != ep2.Event);
        }*/
    }

    public enum GenericEventType
    {
        Shutdown=0,
        Navigate=1,
        GoBack=2,
        GoForward=3,
        ExecuteJS=4,
        JSQuery=5,
        JSQueryResponse=6,
        PageLoaded=7
       

        
    }

   

    [Serializable]
    public class GenericEvent : AbstractEvent
    {
        public GenericEventType Type;

        public string NavigateUrl;

        public string JsCode;

        public string JsQuery;

        public string JsQueryResponse;

        /*protected override bool Compare(AbstractEvent ev2)
        {
            GenericEvent ge = ev2 as GenericEvent;

            return (NavigateUrl == ge.NavigateUrl && JsCode == ge.JsCode && JsQuery == ge.JsQuery && JsQueryResponse == ge.JsQueryResponse);
        }*/
    }
}
