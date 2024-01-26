using System;
using System.Linq;

namespace BenDotNet.RFID.NFC.NFC_A.Topaz
{
    [ReplyType(typeof(ReadIdentifierCommand))]
    public class ReadIdentifierCommand : NFC_A.Command
    {
        public const byte COMMAND_CODE = 0x78;
        public const byte DUMMY_BYTE = 0x00;
        public static byte[] COMPILED_COMMAND = new byte[] { COMMAND_CODE, DUMMY_BYTE, DUMMY_BYTE, DUMMY_BYTE, DUMMY_BYTE, DUMMY_BYTE, DUMMY_BYTE };

        public ReadIdentifierCommand() : base(COMMAND_CODE) { }

        public override byte[] BytesCompiledCommand => COMPILED_COMMAND;
    }
    public class ReadIdentifierReply : Reply
    {
        public ReadIdentifierReply(ref RFID.Command associatedCommand, byte[] compiledReply = null) : base(ref associatedCommand, compiledReply) { }

        public const byte HEADER_ROM_BYTE_0_NDEF_SUPPORT_BIT_SHIFT = 4;
        public ushort HeaderROM;

        public byte[] UID;

        public override byte[] CompiledReply
        {
            get => BitConverter.GetBytes(this.HeaderROM).Concat(UID).ToArray();
            set
            {
                this.HeaderROM = BitConverter.ToUInt16(value, 0);
                this.UID = value[2..];
            }
        }
    }

    #region Memory operations
    #region Read address operands
    public abstract class AddressOperand { }

    public class ReadAllAddressOperand : AddressOperand
    {
        public const byte COMPILED_READ_ALL_ADDRESS_OPERAND = 0x00;

        public static implicit operator byte(ReadAllAddressOperand readAllAddressOperand) => COMPILED_READ_ALL_ADDRESS_OPERAND;

        public static explicit operator ReadAllAddressOperand(byte compiledReadAllAddressOperand) => new ReadAllAddressOperand();
    }

    public class StaticAddressOperand : AddressOperand
    {
        public const byte DUMMY_HEADER_BIT_SHIFT = 7;

        public const byte BLOCK_ADDRESS_BIT_SHIFT = 3;
        public const byte BLOCK_ADDRESS_BIT_MASK = 0b00001111;
        private byte blockAddress;
        public byte BlockAddress
        {
            get => this.blockAddress;
            set
            {
                if (value > BLOCK_ADDRESS_BIT_MASK)
                    throw new ArgumentException("Segment address must be inferior to 16");

                this.blockAddress = value;
            }
        }

        public const byte BYTE_ADDRESS_BIT_SHIFT = 0;
        public const byte BYTE_ADDRESS_BIT_MASK = 0b00000111;
        private byte byteAddress;
        public byte ByteAddress
        {
            get => this.byteAddress;
            set
            {
                if (value > BYTE_ADDRESS_BIT_MASK)
                    throw new ArgumentException("Segment address must be inferior to 8");

                this.byteAddress = value;
            }
        }

        public static implicit operator byte(StaticAddressOperand addressOperand) => (byte)((addressOperand.byteAddress & BYTE_ADDRESS_BIT_MASK)
                | ((addressOperand.blockAddress & BLOCK_ADDRESS_BIT_MASK) << BLOCK_ADDRESS_BIT_SHIFT));

        public static explicit operator StaticAddressOperand(byte compiledAddressOperand)
        {
            if (Helpers.IsTrue(compiledAddressOperand, DUMMY_HEADER_BIT_SHIFT))
                throw new ArgumentException("Most significant bit must be set to zero");

            return new StaticAddressOperand()
            {
                byteAddress = (byte)(compiledAddressOperand & BYTE_ADDRESS_BIT_MASK),
                blockAddress = (byte)(compiledAddressOperand >> BLOCK_ADDRESS_BIT_SHIFT & BLOCK_ADDRESS_BIT_MASK)
            };
        }
    }

    public class SegmentAddressOperand : AddressOperand
    {
        public const byte SEGMENT_ADDRESS_BIT_MASK = 0b00001111;
        public const byte SEGMENT_ADDRESS_BIT_SHIFT = 4;
        private byte segmentAddress;
        public byte SegmentAddress
        {
            get => this.segmentAddress;
            set
            {
                if (value > SEGMENT_ADDRESS_BIT_MASK)
                    throw new ArgumentException("Segment address must be inferior to 16");

                this.segmentAddress = value;
            }
        }

        public const byte DUMMY_BITS_BIT_MASK = 0b00001111;

        public static implicit operator byte(SegmentAddressOperand segmentAddressOperand) =>
            (byte)((segmentAddressOperand.segmentAddress & SEGMENT_ADDRESS_BIT_MASK) << SEGMENT_ADDRESS_BIT_SHIFT);

        public static explicit operator SegmentAddressOperand(byte compiledSegmentAddressOperand)
        {
            if ((compiledSegmentAddressOperand & DUMMY_BITS_BIT_MASK) != 0)
                throw new ArgumentException("Four least significant bits must be set to zero");

            return new SegmentAddressOperand() { segmentAddress = (byte)((compiledSegmentAddressOperand >> SEGMENT_ADDRESS_BIT_SHIFT) & SEGMENT_ADDRESS_BIT_MASK) };
        }
    }

    public class BlockAddressOperand : AddressOperand
    {
        private byte blockAddress;
        public byte BlockAddress
        {
            get => this.blockAddress;
            set => this.blockAddress = value;
        }

        public static implicit operator byte(BlockAddressOperand blockAddressOperand) => blockAddressOperand.BlockAddress;

        public static explicit operator BlockAddressOperand(byte compiledBlockAddressOperand) => new BlockAddressOperand() { BlockAddress = compiledBlockAddressOperand };
    }
    #endregion

    public abstract class ReadCommand : NFC_A.Command
    {
        public ReadCommand(byte commandCode, AddressOperand addressOperand) : base(commandCode) { this.AddressOperand = addressOperand; }

        public readonly AddressOperand AddressOperand;
    }

    public class ReadAllCommand : ReadCommand
    {
        public const byte READ_ALL_COMMAND_CODE = 0x00;

        public ReadAllCommand() : base(READ_ALL_COMMAND_CODE, new ReadAllAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    public class ReadByteCommand : ReadCommand
    {
        public const byte READ_BYTE_COMMAND_CODE = 0x01;

        public ReadByteCommand() : base(READ_BYTE_COMMAND_CODE, new StaticAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    /// <summary>
    /// Correspond to Write-Erase Byte (WRITE-E) command
    /// </summary>
    public class ReplaceByteCommand : ReadCommand
    {
        public const byte WRITE_E_COMMAND_CODE = 0x53;

        public ReplaceByteCommand() : base(WRITE_E_COMMAND_CODE, new StaticAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    /// <summary>
    /// Correspond to Write-No-Erase Byte (WRITE-NE) command
    /// </summary>
    public class SetByteCommand : ReadCommand
    {
        public const byte WRITE_NE_COMMAND_CODE = 0x1A;

        public SetByteCommand() : base(WRITE_NE_COMMAND_CODE, new StaticAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    public class ReadSegmentCommand : ReadCommand
    {
        public const byte READ_SEGMENT_COMMAND_CODE = 0x10;

        public ReadSegmentCommand() : base(READ_SEGMENT_COMMAND_CODE, new SegmentAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    public class Read8BytesCommand : ReadCommand
    {
        public const byte READ_8_BYTES_COMMAND_CODE = 0x02;

        public Read8BytesCommand() : base(READ_8_BYTES_COMMAND_CODE, new BlockAddressOperand()) { }

        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    public class Replace8BytesCommand : ReadCommand
    {
        public const byte REPLACE_8_BYTES_COMMAND_CODE = 0x54;

        public Replace8BytesCommand() : base(REPLACE_8_BYTES_COMMAND_CODE, new BlockAddressOperand()) { }
        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }

    public class Set8BytesCommand : ReadCommand
    {
        public const byte SET_8_BYTES_COMMAND_CODE = 0x1B;

        public Set8BytesCommand() : base(SET_8_BYTES_COMMAND_CODE, new BlockAddressOperand()) { }
        public override byte[] BytesCompiledCommand => throw new NotImplementedException();
    }
    #endregion
}
