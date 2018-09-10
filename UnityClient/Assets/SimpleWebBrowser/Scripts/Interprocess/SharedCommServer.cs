using System;
using System.Collections.Generic;
using MessageLibrary;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SharedCommServer : SharedMemServer
{
  

    //EventPacket _lastPacket = null;

    Queue<EventPacket> _packetsToSend;


    bool _isWrite = false;

    public SharedCommServer(bool write) : base()
    {
        _isWrite = write;
        _packetsToSend = new Queue<EventPacket>();
    }

    public void InitComm(int size, string filename)
    {
        base.Init(size, filename);
        WriteStop();
    }

    private bool CheckIfReady()
    {
        byte[] arr = ReadBytes();
        if (arr != null)
        {
            try
            {
                MemoryStream mstr = new MemoryStream(arr);
                BinaryFormatter bf = new BinaryFormatter();
                EventPacket ep = bf.Deserialize(mstr) as EventPacket;

                if (ep.Type == BrowserEventType.StopPacket)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        return false;
    }


    public EventPacket GetMessage()
    {
        if (_isWrite)
            return null;

        byte[] arr = ReadBytes();
        //  EventPacket ret = null;

        if (arr != null)
        {
            try
            {
                MemoryStream mstr = new MemoryStream(arr);
                BinaryFormatter bf = new BinaryFormatter();
                EventPacket ep = bf.Deserialize(mstr) as EventPacket;

                if (ep != null && ep.Type != BrowserEventType.StopPacket)
                {
                    //_lastPacket = ep;
                    //log.Info("_____RETURNING PACKET:" + ep.Type.ToString());
                    WriteStop();
                    return ep;
                }
                else
                {

                    return null;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        return null;
    }//

    private void WriteStop()
    {
        EventPacket e = new EventPacket
        {
            Type = BrowserEventType.StopPacket
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, e);
        byte[] b = mstr.GetBuffer();
        WriteBytes(b);
    }

    public void WriteMessage(EventPacket ep)
    {

        bool sent = false;
        while (!sent)
        {
            if (CheckIfReady())
            {
                MemoryStream mstr = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(mstr, ep);
                byte[] b = mstr.GetBuffer();
                WriteBytes(b);
                sent = true;
            }
        }
        /* if(_isWrite)
         {
             _packetsToSend.Enqueue(ep);
         }*/
    }

    public void PushMessages()
    {
        if (_packetsToSend.Count != 0)
        {
            if (CheckIfReady())
            {
                EventPacket ep = _packetsToSend.Dequeue();

                MemoryStream mstr = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(mstr, ep);
                byte[] b = mstr.GetBuffer();
                WriteBytes(b);
            }
        }
    }

}
