using System;
using System.Text;

namespace DCS_Nexus {
    public class DCSMessage
    {
        public byte[] Data { get; private set; }

        public DCSMessage(byte[] data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public string Printable
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in Data)
                {
                    if (b >= 32 && b <= 126)
                    {
                        sb.Append((char)b);
                    }
                    else
                    {
                        sb.Append($"[{b:X2}]");
                    }
                }
                return sb.ToString();
            }
        }

        public Google.Protobuf.ByteString ByteString => Google.Protobuf.ByteString.CopyFrom(Data);
    }
}