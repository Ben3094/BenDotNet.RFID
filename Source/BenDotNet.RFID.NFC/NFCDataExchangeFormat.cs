using System;
using System.Collections.Generic;

namespace BenDotNet.RFID.NFC
{
    /// <summary>
    /// TODO: Implement assisted construction of NDEF record !!! In progress
    /// </summary>
    public class NDEFRecord
    {
        public readonly byte[] Compiled;

        public NDEFRecord(byte[] compiled) { this.Compiled = compiled; }

        public const int FLAGS_BYTE_INDEX = 0;

        public const byte TYPE_NAME_FORMAT_BIT_INDEX = 0;
        public const byte TYPE_NAME_FORMAT_BIT_MASK = 0b00000111;
        public TypeNameFormat TypeNameFormat
        {
            get => (TypeNameFormat)(this.Compiled[FLAGS_BYTE_INDEX] & TYPE_NAME_FORMAT_BIT_MASK);
            set
            {
                this.Compiled[FLAGS_BYTE_INDEX] = (byte)(this.Compiled[FLAGS_BYTE_INDEX] | ~TYPE_NAME_FORMAT_BIT_MASK);
                this.Compiled[FLAGS_BYTE_INDEX] |= (byte)((byte)value | TYPE_NAME_FORMAT_BIT_MASK);
            }
        }

        public const byte TYPE_LENGTH_BYTE_INDEX = 1;
        public byte TypeLength
        {
            get => this.Compiled[TYPE_LENGTH_BYTE_INDEX];
            set => this.Compiled[TYPE_LENGTH_BYTE_INDEX] = value;
        }

        public const byte ID_LENGTH_INDICATOR_BIT_INDEX = 3;
        public bool IsIDLengthPresent => Helpers.IsTrue(this.Compiled[FLAGS_BYTE_INDEX], ID_LENGTH_INDICATOR_BIT_INDEX);
        public const byte SHORT_RECORD_ID_LENGTH_BYTE_INDEX = 3;
        public const byte LONG_RECORD_ID_LENGTH_BYTE_INDEX = 6;
        public const byte ID_LENGTH_BYTE_LENGTH = 1;
        public int IDLengthByteIndex => this.IsShort ? SHORT_RECORD_ID_LENGTH_BYTE_INDEX : LONG_RECORD_ID_LENGTH_BYTE_INDEX;
        public byte IDLength => this.IsIDLengthPresent ? this.Compiled[IDLengthByteIndex]: (byte)0x00;


        public const byte SHORT_RECORD_INDICATOR_BIT_INDEX = 4;
        public const byte PAYLOAD_LENGTH_BYTE_INDEX = 2;
        public const byte SHORT_RECORD_LENGTH_BYTE_LENGTH = 1;
        public const byte LONG_RECORD_LENGTH_BYTE_LENGTH = 4;
        public const byte LONG_RECORD_LENGTH_END_BYTE_INDEX = PAYLOAD_LENGTH_BYTE_INDEX + LONG_RECORD_LENGTH_BYTE_LENGTH;
        public bool IsShort => Helpers.IsTrue(this.Compiled[FLAGS_BYTE_INDEX], SHORT_RECORD_INDICATOR_BIT_INDEX);
        public int PayloadLength => this.IsShort ? this.Compiled[PAYLOAD_LENGTH_BYTE_INDEX] : Convert.ToInt32(this.Compiled[PAYLOAD_LENGTH_BYTE_INDEX..LONG_RECORD_LENGTH_END_BYTE_INDEX]);

        public const byte CHUNK_FLAG_BIT_INDEX = 5;
        public bool IsPartial => Helpers.IsTrue(this.Compiled[FLAGS_BYTE_INDEX], CHUNK_FLAG_BIT_INDEX);

        public const byte MESSAGE_END_INDICATOR_BIT_INDEX = 6;
        public bool IsMessageEnd => Helpers.IsTrue(this.Compiled[FLAGS_BYTE_INDEX], MESSAGE_END_INDICATOR_BIT_INDEX);

        public const byte MESSAGE_BEGIN_INDICATOR_BIT_INDEX = 7;
        public bool IsMessageStart => Helpers.IsTrue(this.Compiled[FLAGS_BYTE_INDEX], MESSAGE_BEGIN_INDICATOR_BIT_INDEX);

        public int TypeByteIndex => this.IDLengthByteIndex + ID_LENGTH_BYTE_LENGTH;
        public byte[] Type => this.Compiled[this.TypeByteIndex..this.IDByteIndex];

        public int IDByteIndex => this.TypeByteIndex + this.TypeLength;
        public byte[] ID => this.Compiled[this.IDByteIndex..this.PayloadByteIndex];

        public int PayloadByteIndex => this.IDByteIndex + this.IDLength;
        public int TotalByteLength => this.PayloadByteIndex + this.PayloadLength - 1;
        public byte[] Payload => this.Compiled[this.PayloadByteIndex..this.TotalByteLength];


        public static IEnumerable<NDEFRecord> ParseNDEFRecords(byte[] compiledNDEFRecords)
        {
            int currentNDEFRecordByteIndex = 0;
            int followingNDEFRecordByteIndex = 0;
            NDEFRecord currentTempNDEFRecord;
            while (followingNDEFRecordByteIndex < compiledNDEFRecords.Length)
            {
                currentNDEFRecordByteIndex = followingNDEFRecordByteIndex;
                currentTempNDEFRecord = new NDEFRecord(compiledNDEFRecords[currentNDEFRecordByteIndex..]);
                followingNDEFRecordByteIndex = currentNDEFRecordByteIndex + currentTempNDEFRecord.TotalByteLength;
                yield return new NDEFRecord(compiledNDEFRecords[currentNDEFRecordByteIndex..followingNDEFRecordByteIndex]);
            }
        }
    }

    public enum TypeNameFormat
    {
        Empty = 0x00,
        WellKnown = 0x01,
        RFC2046 = 0x02,
        RFC3986 = 0x03,
        External = 0x04,
        Unknown = 0x05,
        Unchanged = 0x06,
        Reserved = 0x07
    }
}
