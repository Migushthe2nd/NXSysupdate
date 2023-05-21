using Be.IO;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpsPort
{
    class IPS32Writer {

        readonly MemoryStream mem = new();
        readonly BeBinaryWriter bin;
        
        public IPS32Writer() 
        {
            bin = new(mem);
            bin.WriteRawString("IPS32");
        }

        public void Add(uint address, byte[] data)
        {
            if (address == 0x45454F46)
                throw new Exception("God why");

            bin.Write((UInt32)address);
            bin.Write((UInt16)data.Length);
            bin.Write(data);
        }
        
        public void FinalizePatch() => bin.WriteRawString("EEOF");

        public byte[] ToArray() => mem.ToArray();
    }

    static class Exten
    {
        public static void WriteRawString(this BeBinaryWriter bin, string s)
        {
            byte[] data = Encoding.ASCII.GetBytes(s);
            foreach (var b in data)
                bin.Write(b);
        }
    }
}
