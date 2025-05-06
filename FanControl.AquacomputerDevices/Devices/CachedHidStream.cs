using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FanControl.Plugins;

namespace FanControl.AquacomputerDevices.Devices
{
    internal class CachedHidStream : IDisposable
    {
        private HidSharp.HidStream hidStream;

        private CancellationTokenSource cancelTokenSource;
        private bool canRead = false;
        private byte[] cachedValue = null;
        private Exception exception = null;

        public CachedHidStream(HidSharp.HidStream stream)
        {
            hidStream = stream;
            cancelTokenSource = new CancellationTokenSource();
            var cancelToken = cancelTokenSource.Token;

            ReadOnce();
            new Thread(() =>
            {
                stream.ReadTimeout = Timeout.Infinite;

                try
                {
                    while (!cancelToken.IsCancellationRequested)
                    {
                        ReadOnce();
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                canRead = false;
            }).Start();
        }

        private void ReadOnce()
        {
            var result = hidStream.Read();
            if (result != null)
            {
                lock (this)
                {
                    canRead = true;
                    cachedValue = result;
                }
            }
        }

        public void Close()
        {
            cancelTokenSource.Cancel();
            hidStream.Close();
        }

        public virtual void Dispose()
        {
            Close();
        }

        private bool GetCanRead()
        {
            lock (this)
            {
                return canRead;
            }
        }

        public bool CanRead
        {
            get => GetCanRead();
        }

        public byte[] Read()
        {
            lock (this)
            {
                if (exception != null)
                {
                    throw exception;
                }
                return cachedValue;
            }
        }

        public void GetFeature(byte[] buffer)
        {
            hidStream.GetFeature(buffer);
        }

        public void SetFeature(byte[] buffer)
        {
            hidStream.SetFeature(buffer);
        }

        public void Write(byte[] buffer)
        {
            hidStream.Write(buffer);
        }
    }
}
