// Credits to: https://github.com/GediminasMasaitis/crc-dot-net
using System;
using System.Collections.Generic;
using System.Text;

namespace Crc
{
    class CrcConfigurationException : Exception
    {
        public CrcConfigurationException(string message) : base(message)
        {
        }
    }
}