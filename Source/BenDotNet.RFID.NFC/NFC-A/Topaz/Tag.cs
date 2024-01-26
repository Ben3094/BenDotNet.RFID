using System;
using System.Collections.Generic;
using System.Text;

namespace BenDotNet.RFID.NFC.NFC_A.Topaz
{
    public class Tag : NFC_A.Tag
    {
        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource) { }
    }
}
