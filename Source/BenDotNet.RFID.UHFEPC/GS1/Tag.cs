using System;

namespace BenDotNet.RFID.UHFEPC.GS1
{
    public partial class Tag : UHFEPC.Tag
    {
        public sealed override ISO15693ClassIdentifier ISO15693Class => ISO15693ClassIdentifier.GS1;

        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource)
        {
            this.IsTIDExtended = uid[EXTENDED_TID_FLAG_BYTE_INDEX].IsTrue(EXTENDED_TID_FLAG_BYTE_SHIFT);
            this.HasSecurityFlag = uid[SECURITY_FLAG_BYTE_INDEX].IsTrue(SECURITY_FLAG_BYTE_SHIFT);
            this.HasFileFlag = uid[SECURITY_FLAG_BYTE_INDEX].IsTrue(SECURITY_FLAG_BYTE_SHIFT);

            ushort firstPart = (ushort)(uid[DESIGNER_IDENTIFIER_BYTE_INDEX] << DESIGNER_IDENTIFIER_BIT_IN_SECOND_PART);
            byte secondPart = (byte)(uid[DESIGNER_IDENTIFIER_BYTE_INDEX + 1] >> (8 - DESIGNER_IDENTIFIER_BIT_IN_SECOND_PART));
            this.DesignerIdentifier = (ushort)((firstPart | secondPart) & DESIGNER_IDENTIFIER_MAX_VALUE);

            firstPart = (ushort)(uid[MODEL_IDENTIFIER_BYTE_INDEX] << MODEL_IDENTIFIER_BITS_IN_SECOND_PART);
            secondPart = (byte)(uid[MODEL_IDENTIFIER_BYTE_INDEX + 1] >> (8 - MODEL_IDENTIFIER_BITS_IN_SECOND_PART));
            this.ModelIdentifier = (ushort)((firstPart | secondPart) & MODEL_IDENTIFIER_MAX_VALUE);

            if (this.IsTIDExtended)
            {
                this.ExtendedTID = uid[EXTENDED_TID_START_BYTE_INDEX..];

                this.IsXTIDExtended = uid[EXTENDED_XTID_FLAG_BYTE_INDEX].IsTrue(EXTENDED_XTID_FLAG_BYTE_SHIFT);
                this.HasUserMemoryAndBlockPermalockSegment = uid[USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_BYTE_INDEX].IsTrue(USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_BYTE_SHIFT);
                this.HasBlockWriteAndBlockEraseSegment = uid[BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_BYTE_INDEX].IsTrue(BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_BYTE_SHIFT);
                this.HasOptionalCommandSupportSegment = uid[OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_BYTE_INDEX].IsTrue(OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_BYTE_SHIFT);

                int serialisation = (uid[SERIALISATION_BYTE_INDEX] >> SERIALISATION_BYTE_SHIFT) & SERIALIZATION_MAX_VALUE;
                this.SerialNumberWordsLength = serialisation == 0 ? (byte)0 : (byte)(3 + (serialisation - 1));
                this.SerialNumber = uid[EXTENDED_TID_END_BYTE_INDEX..(EXTENDED_TID_END_BYTE_INDEX + (int)this.SerialNumberWordsLength * 2)];
            }

            //TODO: Check permalock of TID memory

            //EPC verification
            //TODO
        }

        public const byte EXTENDED_TID_FLAG_INDEX = 0x08;
        public const byte EXTENDED_TID_FLAG_BYTE_INDEX = EXTENDED_TID_FLAG_INDEX / 8;
        public const byte EXTENDED_TID_FLAG_BYTE_SHIFT = 7 - EXTENDED_TID_FLAG_INDEX % 8;
        public readonly bool IsTIDExtended;

        public const byte SECURITY_FLAG_INDEX = 0x09;
        public const byte SECURITY_FLAG_BYTE_INDEX = SECURITY_FLAG_INDEX / 8;
        public const byte SECURITY_FLAG_BYTE_SHIFT = 7 - SECURITY_FLAG_INDEX % 8;
        public readonly bool HasSecurityFlag;

        public const byte FILE_FLAG_INDEX = 0x0A;
        public const byte FILE_FLAG_BYTE_INDEX = FILE_FLAG_INDEX / 8;
        public const byte FILE_FLAG_BYTE_SHIFT = 7 - FILE_FLAG_INDEX % 8;
        public readonly bool HasFileFlag;

        public const byte DESIGNER_IDENTIFIER_START_INDEX = 0x0B;
        public const byte DESIGNER_IDENTIFIER_BYTE_INDEX = DESIGNER_IDENTIFIER_START_INDEX / 8;
        public const byte DESIGNER_IDENTIFIER_BIT_IN_FIRST_PART = 8 - (DESIGNER_IDENTIFIER_START_INDEX % 8);
        public const byte DESIGNER_IDENTIFIER_MAX_BIT_LENGTH = 9;
        public const byte DESIGNER_IDENTIFIER_BIT_IN_SECOND_PART = DESIGNER_IDENTIFIER_MAX_BIT_LENGTH - DESIGNER_IDENTIFIER_BIT_IN_FIRST_PART;
        public const ushort DESIGNER_IDENTIFIER_MAX_VALUE = (1 << DESIGNER_IDENTIFIER_MAX_BIT_LENGTH) - 1;
        public readonly ushort DesignerIdentifier;
        /// <remarks>Prefer a method than a readonly field because if the designer mask is not available then just the method fails instead of the constructor</remarks>
        public MaskDesignerIdentifier KnownDesignerIdentifier
        {
            get { return (MaskDesignerIdentifier)this.DesignerIdentifier; }
        }

        public const byte MODEL_IDENTIFIER_START_INDEX = 0x14;
        public const int MODEL_IDENTIFIER_BYTE_INDEX = MODEL_IDENTIFIER_START_INDEX / 8;
        public const int MODEL_IDENTIFIER_BITS_IN_FIRST_PART = 8 - (MODEL_IDENTIFIER_START_INDEX % 8);
        public const byte MODEL_IDENTIFIER_MAX_BIT_LENGTH = 12;
        public const byte MODEL_IDENTIFIER_BITS_IN_SECOND_PART = MODEL_IDENTIFIER_MAX_BIT_LENGTH - MODEL_IDENTIFIER_BITS_IN_FIRST_PART;
        public const ushort MODEL_IDENTIFIER_MAX_VALUE = (1 << MODEL_IDENTIFIER_MAX_BIT_LENGTH) - 1;
        public readonly ushort ModelIdentifier;

        public const byte EXTENDED_TID_START_BIT_INDEX = 0x1F;
        public const byte EXTENDED_TID_START_BYTE_INDEX = EXTENDED_TID_START_BIT_INDEX / 8;
        public const byte EXTENDED_TID_END_BIT_INDEX = 0x30;
        public const byte EXTENDED_TID_END_BYTE_INDEX = EXTENDED_TID_END_BIT_INDEX / 8;
        public readonly byte[] ExtendedTID;

        public const byte EXTENDED_XTID_FLAG_INDEX = 0x2F;
        public const byte EXTENDED_XTID_FLAG_BYTE_INDEX = EXTENDED_XTID_FLAG_INDEX / 8;
        public const byte EXTENDED_XTID_FLAG_BYTE_SHIFT = 7 - EXTENDED_XTID_FLAG_INDEX % 8;
        public readonly bool IsXTIDExtended = false;

        public const byte USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_INDEX = 0x25;
        public const byte USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_BYTE_INDEX = USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_INDEX / 8;
        public const byte USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_BYTE_SHIFT = 7 - USER_MEMORY_AND_BLOCK_PERMALOCK_SEGMENT_FLAG_INDEX % 8;
        public readonly bool HasUserMemoryAndBlockPermalockSegment = false;

        public const byte BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_INDEX = 0x24;
        public const byte BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_BYTE_INDEX = BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_INDEX / 8;
        public const byte BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_BYTE_SHIFT = 7 - BLOCKWRITE_AND_BLOCKERASE_SEGMENT_FLAG_INDEX % 8;
        public readonly bool HasBlockWriteAndBlockEraseSegment = false;

        public const byte OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_INDEX = 0x23;
        public const byte OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_BYTE_INDEX = OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_INDEX / 8;
        public const byte OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_BYTE_SHIFT = 7 - OPTIONAL_COMMAND_SUPPORT_SEGMENT_FLAG_INDEX % 8;
        public readonly bool HasOptionalCommandSupportSegment = false;

        public const byte SERIALISATION_START_INDEX = 0x22;
        public const int SERIALISATION_BYTE_INDEX = SERIALISATION_START_INDEX / 8;
        public const byte SERIALISATION_BYTE_SHIFT = 7 - SERIALISATION_START_INDEX % 8;
        public const byte SERIALIZATION_MAX_BIT_LENGTH = 3;
        public const ushort SERIALIZATION_MAX_VALUE = (1 << SERIALIZATION_MAX_BIT_LENGTH) - 1;
        public readonly byte? SerialNumberWordsLength = null;

        public readonly byte[] SerialNumber = null;
    }
}
