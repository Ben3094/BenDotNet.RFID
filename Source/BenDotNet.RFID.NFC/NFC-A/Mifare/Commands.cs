using System;
using System.Linq;

namespace BenDotNet.RFID.NFC.NFC_A.Mifare
{
    public abstract class Command : NFC_A.Command
    {
        public Command(byte commandCode) : base(commandCode) { }
    }

    public class SelectSectorPacket1Command : Command
    {
        public const byte COMMAND_CODE = 0xC2;
        public const byte DUMMY_BYTE = 0xFF;
        public static byte[] COMPILED_COMMAND = new byte[] { COMMAND_CODE, DUMMY_BYTE };

        public SelectSectorPacket1Command() : base(COMMAND_CODE) { }

        public override byte[] BytesCompiledCommand => COMPILED_COMMAND;
    }
    public class SelectSectorPacket2Command : Command
    {
        public const byte SECTOR_UPPER_LIMIT = 0xFE;
        public static byte[] DUMMY_BYTES = new byte[] { 0x00, 0x00, 0x00 };

        public SelectSectorPacket2Command() : base(0x00) { }

        private byte queriedSector;
        public byte QueriedSector
        {
            get => this.queriedSector;
            set
            {
                if (value > SECTOR_UPPER_LIMIT)
                    throw new ArgumentOutOfRangeException("");

                this.queriedSector = value;
            }
        }

        public override byte[] BytesCompiledCommand => (new byte[] { this.QueriedSector }).Concat(DUMMY_BYTES).ToArray();
    }

    [ReplyType(typeof(ReadReply))]
    public class ReadCommand : Command
    {
        public const byte COMMAND_CODE = 0x30;

        public ReadCommand() : base(COMMAND_CODE) { }

        public byte Index = 0;

        public override byte[] BytesCompiledCommand => new byte[] { this.CommandCode, this.Index };
    }
    public class ReadReply : Reply
    {
        public ReadReply(ref RFID.Command associatedCommand, byte[] compiledReply = null) : base(ref associatedCommand, compiledReply) { }

        public byte[] Data;

        public override byte[] CompiledReply
        {
            get => this.Data;
            set => this.Data = value[0..15];
        }
    }

    public class WriteCommand : Command
    {
        public const byte COMMAND_CODE = 0xA2;

        public WriteCommand() : base(COMMAND_CODE) { }

        public byte Index = 0;

        public const byte BLOCK_BYTES_SIZE = 4; //In bytes
        private byte[] data = new byte[BLOCK_BYTES_SIZE];
        public byte[] Data
        {
            get => this.data;
            set
            {
                if (value.Length > BLOCK_BYTES_SIZE)
                    throw new ArgumentException("Sent data must be 4 bytes long (one Mifare block)");

                this.data = value;
            }
        }

        public override byte[] BytesCompiledCommand => (new byte[2] { this.CommandCode, this.Index }).Concat(this.Data).ToArray();
    }

    [ReplyType(typeof(GetVersionReply))]
    public class GetVersionCommand : Command
    {
        public const byte COMMAND_CODE = 0x60;
        public static byte[] COMPILED_COMMAND = new byte[] { COMMAND_CODE };

        public GetVersionCommand() : base(COMMAND_CODE) { }

        public override byte[] BytesCompiledCommand => COMPILED_COMMAND;
    }
    public class GetVersionReply : Reply
    {
        public GetVersionReply(ref RFID.Command associatedCommand, byte[] compiledReply = null) : base(ref associatedCommand, compiledReply) { }

        public const byte VERSION_BYTES_SIZE = 8;

        public const byte HEADER_BYTES_INDEX = 0;
        public const byte HEADER = 0x00;

        public const byte VENDOR_ID_BYTES_INDEX = 1;
        private byte vendorID;
        public NFC_V.Manufacturer VendorID
        {
            get => (NFC_V.Manufacturer)this.vendorID;
            set => this.vendorID = (byte)value;
        }

        public const byte PRODUCT_TYPE_BYTES_INDEX = 2;
        public static byte CompileProductType(ProductType product, Implementation implementation)
        {
            return (byte)
            (
                (((byte)implementation) << IMPLEMENTATION_PRODUCT_TYPE_BIT_SHIFT)
                & (((byte)product) << PRODUCT_PRODUCT_TYPE_BIT_SHIFT)
            );
        }
        private byte productType = 0x00;
        public const byte PRODUCT_PRODUCT_TYPE_BIT_SHIFT = 0;
        public const byte PRODUCT_PRODUCT_TYPE_BIT_MASK = 0b1111;
        public ProductType ProductType
        {
            get => (ProductType)((this.productType >> PRODUCT_PRODUCT_TYPE_BIT_SHIFT) & PRODUCT_PRODUCT_TYPE_BIT_MASK);
            set => this.productType = CompileProductType(value, this.Implementation);
        }
        public const byte IMPLEMENTATION_PRODUCT_TYPE_BIT_SHIFT = 4;
        public const byte IMPLEMENTATION_PRODUCT_TYPE_BIT_MASK = 0b1111;
        public Implementation Implementation
        {
            get => (Implementation)((this.productType >> IMPLEMENTATION_PRODUCT_TYPE_BIT_SHIFT) & IMPLEMENTATION_PRODUCT_TYPE_BIT_MASK);
            set => this.productType = CompileProductType(this.ProductType, value);
        }

        public const byte PRODUCT_SUBTYPE_BYTES_INDEX = 3;
        private byte productSubType;
        public ProductSubType ProductSubType
        {
            get => (ProductSubType)this.productSubType;
            set => this.productSubType = (byte)value;
        }

        public const byte MAJOR_PRODUCT_VERSION_BYTES_INDEX = 4;
        private byte majorVersion;
        public const byte MINOR_PRODUCT_VERSION_BYTES_INDEX = 5;
        private byte minorVersion;
        public Version Version
        {
            get => new Version(this.majorVersion, this.minorVersion);
            set
            {
                this.majorVersion = (byte)value.Major;
                this.minorVersion = (byte)value.Minor;
            }
        }

        public const byte SIZE_BYTES_INDEX = 6;
        public byte MemorySize;

        public const byte PROTOCOL_TYPE_BYTES_INDEX = 7;
        private byte protocol;
        public Protocol Protocol
        {
            get => (Protocol)this.protocol;
            set => this.protocol = (byte)value;
        }

        public override byte[] CompiledReply
        {
            get => new byte[] { HEADER, this.vendorID, this.productType, this.productSubType, this.majorVersion, this.minorVersion, this.MemorySize, this.protocol };
            set
            {
                if (value.Length != VERSION_BYTES_SIZE)
                    throw new ArgumentException("Version must be 8 bytes long");
                if (value[HEADER_BYTES_INDEX] != HEADER)
                    throw new ArgumentException("Version header should be 0x00");

                this.vendorID = value[VENDOR_ID_BYTES_INDEX];
                this.productType = value[PRODUCT_TYPE_BYTES_INDEX];
                this.productSubType = value[PRODUCT_SUBTYPE_BYTES_INDEX];
                this.majorVersion = value[MAJOR_PRODUCT_VERSION_BYTES_INDEX];
                this.minorVersion = value[MINOR_PRODUCT_VERSION_BYTES_INDEX];
                this.MemorySize = value[SIZE_BYTES_INDEX];
                this.protocol = value[PROTOCOL_TYPE_BYTES_INDEX];
            }
        }
    }

    public enum ProductType
    {
        MifareDESFire = 0x1,
        MifarePlus = 0x2,
        MifareUltralight = 0x3,
        NTAG = 0x4,
        NTAG_I2C = 0x7,
        MifareDESFireLight = 0x8
    }
    public enum Implementation
    {
        Native = 0x0,
        ThirdParty = 0x8,
        JavaCardApplet = 0x9,
        Mifare2GO = 0xA
    }
    public enum ProductSubType
    {
        NTAG210_212 = 0x01,
        NTAG213_215_216_223DNA_224DNA_424DNA = 0x02,
        NHS = 0x06,
        NTAG223DNA_224DNA_StatusDetect_424DNATT = 0x08
    }
    public enum Protocol
    {
        ISO14443_3 = 0x03
    }
}
