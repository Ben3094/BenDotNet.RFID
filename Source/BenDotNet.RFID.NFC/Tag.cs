using System;
using System.IO;

namespace BenDotNet.RFID.NFC
{
    public class Tag : RFID.Tag
    {
        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource)
        {
            
        }

        public override Stream Memory => throw new NotImplementedException();
    }
}
