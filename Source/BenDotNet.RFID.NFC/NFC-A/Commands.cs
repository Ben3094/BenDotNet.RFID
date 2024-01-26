using System;
using System.Collections.Generic;
using System.Text;

namespace BenDotNet.RFID.NFC.NFC_A
{
    public abstract class Command : NFC.Command
    {
        public Command(byte commandCode) { this.CommandCode = commandCode; }

        public const byte COMMAND_CODE_BIT_MASK = 0b01111111;
        public readonly byte CommandCode;
    }

    [ReplyType(typeof(AnswerToRequestTypeAReply))]
    public class ALLCommand : Command
    {
        public const byte COMMAND_CODE = 0x52;
        public static byte[] COMPILED_COMMAND = new byte[] { COMMAND_CODE };

        public ALLCommand() : base(COMMAND_CODE) { }

        public override byte[] BytesCompiledCommand => COMPILED_COMMAND;
    }
    [ReplyType(typeof(AnswerToRequestTypeAReply))]
    public class SENSCommand : Command
    {
        public const byte COMMAND_CODE = 0x26;
        public static byte[] COMPILED_COMMAND = new byte[] { COMMAND_CODE };

        public SENSCommand() : base(COMMAND_CODE) { }

        public override byte[] BytesCompiledCommand => COMPILED_COMMAND;
    }
    public class AnswerToRequestTypeAReply : Reply
    {
        public const byte IDENTIFIER_SIZE_ANTICOLLISION_INFORMATION_BIT_MASK = 0b11;
        public const byte IDENTIFIER_SIZE_ANTICOLLISION_INFORMATION_BIT_SHIFT = 6;

        public const byte BIT_FRAME_SDD_ANTICOLLISION_INFORMATION_BIT_MASK = 0b11111;
        public const byte BIT_FRAME_SDD_ANTICOLLISION_INFORMATION_BIT_SHIFT = 0;
        public const byte TYPE_1_BIT_FRAME_SDD = 0b00000;

        public const byte TYPE_1_PLATFORM_INFORMATION = 0b00001100;

        public AnswerToRequestTypeAReply(ref RFID.Command associatedCommand, byte[] compiledReply = null) : base(ref associatedCommand, compiledReply) { }

        public byte AnticollisionInformation;
        public uint? IdentifierBytesLength
        {
            get
            {
                switch ((this.AnticollisionInformation >> IDENTIFIER_SIZE_ANTICOLLISION_INFORMATION_BIT_SHIFT) & IDENTIFIER_SIZE_ANTICOLLISION_INFORMATION_BIT_MASK)
                {
                    case 0b00:
                        return 4;
                    case 0b01:
                        return 7;
                    case 0b10:
                        return 10;
                    default:
                        return null;
                }

            }
        }
        public byte BitFrameSDD
        {
            get => (byte)((this.AnticollisionInformation >> BIT_FRAME_SDD_ANTICOLLISION_INFORMATION_BIT_SHIFT) & BIT_FRAME_SDD_ANTICOLLISION_INFORMATION_BIT_MASK);
        }

        public byte PlatformInformation;
        public bool IsType1Tag => this.PlatformInformation == TYPE_1_PLATFORM_INFORMATION;

        public override byte[] CompiledReply
        {
            get => new byte[] { this.AnticollisionInformation, this.PlatformInformation };
            set
            {
                this.AnticollisionInformation = value[0];
                this.PlatformInformation = value[1];
            }
        }
    }
}
