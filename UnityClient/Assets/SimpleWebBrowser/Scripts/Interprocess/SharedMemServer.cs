using System;
using SharedMemory;

public class SharedMemServer : IDisposable
{
    private SharedArray<byte> _sharedBuf;

    private bool _isOpen;

    public string Filename;

 




    public void Init(int size, string filename)
    {
        _sharedBuf = new SharedArray<byte>(filename, size);
        _isOpen = true;
        Filename = filename;

    }

    public void Connect(string filename)
    {
        try
        {
            _sharedBuf = new SharedArray<byte>(filename);
            Filename = filename;
            _isOpen = true;
        }
        catch (Exception ex)
        {
            _isOpen = false;
        }
    }

    public bool GetIsOpen()
    {
        return _isOpen;
    }

    public void Resize(int newSize)
    {


        if (_sharedBuf.Length != newSize)
        {
            _sharedBuf.Close();
            _sharedBuf = new SharedArray<byte>(Filename, newSize);
        }
    }

    public void WriteBytes(byte[] bytes)
    {
        if (_isOpen)
        {
            if (bytes.Length > _sharedBuf.Length)
            {
                Resize(bytes.Length);
            }
            _sharedBuf.Write(bytes);
        }
    }

    public void Dispose()
    {
        _isOpen = false;
        _sharedBuf.Close();
    }

    public byte[] ReadBytes()
    {
        byte[] ret = null;
        if (_isOpen)
        {
            ret = new byte[_sharedBuf.Count];
            _sharedBuf.CopyTo(ret);

            //_sharedBuf.
        }

        return ret;
    }


}
