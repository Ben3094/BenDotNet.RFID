using System;
using System.Collections.Generic;
using System.Text;

namespace BenDotNet.RFID.NFC.NFC_A
{
    public class Tag : NFC.Tag
    {
        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource)
        {

        }
    }
}
