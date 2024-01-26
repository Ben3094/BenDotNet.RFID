using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BenDotNet.RFID.NFC.NFC_A.Mifare
{
    /// <summary>
    /// NFC-A type 2 tag (codenamed Mifare)
    /// </summary>
    public class Tag : NFC_A.Tag
    {
        public const byte UID_BLOCKS_INDEX = 0;
        public const byte UID_BLOCKS_LENGTH = 2;

        public Tag(byte[] uid, DetectionSource detectionSource) : base(uid, detectionSource)
        {
            GetVersionReply getVersionReply = (GetVersionReply)this.Execute(new GetVersionCommand());
            this.VendorID = getVersionReply.VendorID;
            this.ProductType = getVersionReply.ProductType;
            this.Implementation = getVersionReply.Implementation;
            this.ProductSubType = getVersionReply.ProductSubType;
            this.Version = getVersionReply.Version;
            this.MemorySize = getVersionReply.MemorySize;
            this.Protocol = getVersionReply.Protocol;

            this.updateCapacityContainer();
        }

        public readonly NFC_V.Manufacturer VendorID;
        public readonly ProductType ProductType;
        public readonly Implementation Implementation;
        public readonly ProductSubType ProductSubType;
        public readonly Version Version;
        public readonly byte MemorySize;
        public readonly Protocol Protocol;

        public const byte CAPACITY_CONTAINER_BLOCK_INDEX = 3;
        public const byte IS_NFC_FORUM_FORMATTED_CAPACITY_CONTAINER_BLOCK_INDEX = 0;
        public const byte IS_NFC_FORUM_FORMATTED_CAPACITY_CONTAINER_BLOCK = 0xE1;
        private bool isNFCForumFormatted;
        public bool IsNFCForumFormatted => this.isNFCForumFormatted;
        public const byte NDEF_VERSION_CAPACITY_CONTAINER_BLOCK_INDEX = 1;
        public const byte NDEF_MAJOR_VERSION_CAPACITY_CONTAINER_BLOCK_MASK = 0b11110000;
        public const byte NDEF_MAJOR_VERSION_CAPACITY_CONTAINER_BLOCK_BIT_INDEX = 4;
        public const byte NDEF_MINOR_VERSION_CAPACITY_CONTAINER_BLOCK_MASK = 0b00001111;
        public const byte NDEF_MINOR_VERSION_CAPACITY_CONTAINER_BLOCK_BIT_INDEX = 0;
        private Version ndefVersion;
        public Version NDEFVersion => this.ndefVersion;
        public const byte MEMORY_SIZE_IN_BLOCKS_CAPACITY_CONTAINER_BLOCK_INDEX = 2;
        private byte memorySizeInBlocks;
        public byte MemorySizeInBlocks => this.memorySizeInBlocks;
        public byte DynamicLockBytes => (byte)Math.Ceiling((double)((this.memorySizeInBlocks * 4) - 48) / 64);
        public const byte MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_INDEX = 3;
        public const byte READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_MASK = 0b11110000;
        public const byte READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_BIT_INDEX = 4;
        public const byte GRANTED_READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK = 0x0;
        public const byte WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_MASK = 0b00001111;
        public const byte WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_BIT_INDEX = 0;
        public const byte GRANTED_WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK = 0x0;
        public const byte DENIED_WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK = 0xF;
        private bool readMemoryAccess;
        public bool ReadMemoryAccess => this.readMemoryAccess;
        private bool writeMemoryAccess;
        public bool WriteMemoryAccess => this.writeMemoryAccess;

        private void updateCapacityContainer()
        {
            ReadReply readReply = (ReadReply)this.Execute(new ReadCommand() { Index = CAPACITY_CONTAINER_BLOCK_INDEX });
            this.isNFCForumFormatted = readReply.Data[IS_NFC_FORUM_FORMATTED_CAPACITY_CONTAINER_BLOCK_INDEX] == IS_NFC_FORUM_FORMATTED_CAPACITY_CONTAINER_BLOCK;

            int ndefMajorVersion = (readReply.Data[NDEF_VERSION_CAPACITY_CONTAINER_BLOCK_INDEX] & NDEF_MAJOR_VERSION_CAPACITY_CONTAINER_BLOCK_MASK) >> NDEF_MAJOR_VERSION_CAPACITY_CONTAINER_BLOCK_BIT_INDEX;
            int ndefMinorVersion = (readReply.Data[NDEF_VERSION_CAPACITY_CONTAINER_BLOCK_INDEX] & NDEF_MINOR_VERSION_CAPACITY_CONTAINER_BLOCK_MASK) >> NDEF_MINOR_VERSION_CAPACITY_CONTAINER_BLOCK_BIT_INDEX;
            this.ndefVersion = new Version(ndefMajorVersion, ndefMinorVersion);

            this.memorySizeInBlocks = readReply.Data[MEMORY_SIZE_IN_BLOCKS_CAPACITY_CONTAINER_BLOCK_INDEX];

            this.readMemoryAccess = ((readReply.Data[MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_INDEX] & READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_MASK) >> READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_BIT_INDEX) == GRANTED_READ_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK;
            this.writeMemoryAccess = ((readReply.Data[MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_INDEX] & WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_MASK) >> WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK_BIT_INDEX) == GRANTED_WRITE_MEMORY_ACCESS_CAPACITY_CONTAINER_BLOCK;
        }

        public const byte BLOCK_BYTE_SIZE = 4;
        public const byte READ_COMMAND_RETURNED_BLOCKS = 4;

        public void Write(int byteOffset, byte[] value)
        {
            if (byteOffset < 0)
                throw new ArgumentOutOfRangeException("Byte offset cannont be negative");

            //First niddle...
            int blockOffset = byteOffset / BLOCK_BYTE_SIZE;
            int byteOffsetInBlock = byteOffset % BLOCK_BYTE_SIZE;
            bool isFirstByteAligned = byteOffsetInBlock == 0;
            int nextBlockByteIndex = BLOCK_BYTE_SIZE - byteOffsetInBlock;
            if (isFirstByteAligned) //...if needed.
            {
                byte[] firstNonAlignedBlock = ((ReadReply)this.Execute(new ReadCommand() { Index = (byte)blockOffset })).Data;
                firstNonAlignedBlock = firstNonAlignedBlock[0..BLOCK_BYTE_SIZE];
                value[0..nextBlockByteIndex].CopyTo(firstNonAlignedBlock, byteOffsetInBlock);
                this.Execute(new WriteCommand() { Index = (byte)blockOffset, Data = firstNonAlignedBlock });
            }

            //Well aligned blocks
            int alignedBlockOffset = blockOffset + (isFirstByteAligned ? 0 : 1);
            int alignedBlocks = (value.Length / BLOCK_BYTE_SIZE) - (isFirstByteAligned ? 0 : 1);
            int nextBlockIndex = alignedBlockOffset;
            for (; nextBlockIndex < alignedBlockOffset + alignedBlocks; nextBlockIndex++)
            {
                this.Execute(new WriteCommand() { Index = (byte)nextBlockIndex, Data = value[nextBlockByteIndex..(nextBlockByteIndex + BLOCK_BYTE_SIZE)] });
                nextBlockByteIndex += BLOCK_BYTE_SIZE;
            }

            //Trailing niddle...
            if (nextBlockByteIndex < value.Length) //...if needed.
            {
                byte[] trailingBlock = ((ReadReply)this.Execute(new ReadCommand() { Index = (byte)nextBlockIndex })).Data;
                trailingBlock = trailingBlock[0..BLOCK_BYTE_SIZE];
                value[0..(value.Length - nextBlockByteIndex)].CopyTo(trailingBlock, 0);
                this.Execute(new WriteCommand() { Index = (byte)nextBlockIndex, Data = trailingBlock });
            }
        }

        public IEnumerable<byte> Read(int index, int length)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("Byte offset cannont be negative");

            List<byte> data = new List<byte>();

            try
            {
                //First niddle...
                int blockOffset = index / BLOCK_BYTE_SIZE;
                int byteOffsetInBlock = index % BLOCK_BYTE_SIZE;
                bool isFirstByteAligned = byteOffsetInBlock == 0;
                int nextBlockByteIndex = BLOCK_BYTE_SIZE - byteOffsetInBlock;
                if (!isFirstByteAligned) //...if needed.
                {
                    byte[] firstBlock = ((ReadReply)this.Execute(new ReadCommand() { Index = (byte)blockOffset })).Data;
                    firstBlock = firstBlock[0..BLOCK_BYTE_SIZE];
                    firstBlock = firstBlock[byteOffsetInBlock..];
                    data.AddRange(firstBlock);
                }

                //Well aligned blocks
                int alignedBlockOffset = blockOffset + (isFirstByteAligned ? 0 : 1);
                int alignedBlocks = (length / BLOCK_BYTE_SIZE) - (isFirstByteAligned ? 0 : 1);
                int nextBlockIndex = alignedBlockOffset;
                for (; nextBlockIndex < alignedBlockOffset + alignedBlocks; nextBlockIndex++)
                {
                    byte[] block = ((ReadReply)this.Execute(new ReadCommand() { Index = (byte)nextBlockIndex })).Data;
                    block = block[0..BLOCK_BYTE_SIZE];
                    data.AddRange(block);
                }

                //Trailing niddle...
                if (nextBlockByteIndex < length) //...if needed.
                {
                    byte[] trailingBlock = ((ReadReply)this.Execute(new ReadCommand() { Index = (byte)nextBlockIndex })).Data;
                    trailingBlock = trailingBlock[0..BLOCK_BYTE_SIZE];
                    data.AddRange(trailingBlock[0..(length - nextBlockByteIndex)]);
                }
            }
            catch (Exception e) { }

            return data.AsEnumerable();
        }

        public const byte DATA_BLOCK_START_INDEX = 4;
        public IEnumerable<TLVBlock> GetTVLBlocksInMemory(bool breakOnTerminatorTLVBlock = true)
        {
            MemoryEnumerator mifareMemoryEnumerator = new MemoryEnumerator(this);
            bool tlvBlockMalformed = true;
            int previousWellFormedTLVBlockIndex = DATA_BLOCK_START_INDEX * BLOCK_BYTE_SIZE;

            while (true)
            {
                if (tlvBlockMalformed)
                {
                    mifareMemoryEnumerator.Reset();
                    for (int byteIndex = -1; byteIndex < previousWellFormedTLVBlockIndex; byteIndex++)
                        if (!mifareMemoryEnumerator.MoveNext()) yield break;
                    tlvBlockMalformed = false;
                }

                TLVBlock currentTLVBlock = null;

                TagField currentTagField = (TagField)mifareMemoryEnumerator.Current;
                if (!mifareMemoryEnumerator.MoveNext()) yield break;
                previousWellFormedTLVBlockIndex = mifareMemoryEnumerator.Index;
                switch (currentTagField)
                {
                    case TagField.Null:
                        currentTLVBlock = new NullTLVBlock();
                        break;
                    case TagField.LockControl:
                        currentTLVBlock = new LockControlTLVBlock();
                        break;
                    case TagField.MemoryControl:
                        currentTLVBlock = new MemoryControlTLVBlock();
                        break;
                    case TagField.NDEFMessage:
                        currentTLVBlock = new NDEFTLVBlock();
                        break;
                    case TagField.Proprietary:
                        currentTLVBlock = new ProprietaryTLVBlock();
                        break;
                    case TagField.Terminator:
                        currentTLVBlock = new TerminatorTLVBlock();
                        break;
                }

                switch (currentTagField)
                {
                    case TagField.Null:
                    case TagField.Terminator:
                        break;

                    case TagField.LockControl:
                    case TagField.MemoryControl:
                    case TagField.NDEFMessage:
                    case TagField.Proprietary:
                        try
                        {
                            ushort length;
                            if (mifareMemoryEnumerator.Current == TLVBlock.MORE_LENGTH_INDICATOR)
                            {
                                if (!mifareMemoryEnumerator.MoveNext()) yield break;
                                byte msb = mifareMemoryEnumerator.Current;
                                if (!mifareMemoryEnumerator.MoveNext()) yield break;
                                byte lsb = mifareMemoryEnumerator.Current;

                                length = (ushort)((msb << 8) | lsb);
                            }
                            else length = mifareMemoryEnumerator.Current;

                            byte[] value = new byte[length];
                            for (ushort i = 0; i < length; i++)
                            {
                                if (!mifareMemoryEnumerator.MoveNext()) yield break;
                                value[i] = mifareMemoryEnumerator.Current;
                            }
                            currentTLVBlock.Value = value;

                            break;
                        }
                        catch (Exception ex)
                        {
                            tlvBlockMalformed = true;
                            break;
                        }
                }

                if (breakOnTerminatorTLVBlock && (currentTLVBlock is TerminatorTLVBlock))
                    yield break;
                else if (currentTLVBlock != null)
                    yield return currentTLVBlock;
            }
        }
    }

    public class MemoryEnumerator : IEnumerator<byte>
    {
        public const byte BLOCK_BYTE_SIZE = 4;

        public readonly Tag Tag;

        public MemoryEnumerator(Tag tag) { this.Tag = tag; }
        public void Dispose() { }


        private int blockBufferIndex = -1;
        public int BlockBufferIndex => this.blockBufferIndex;

        private byte[] blockBuffer = new byte[BLOCK_BYTE_SIZE];
        public byte[] BlockBuffer => this.blockBuffer;

        public const int DEFAULT_INDEX = -1;
        private int index = DEFAULT_INDEX;
        public int Index => this.index;

        public byte Current => this.blockBuffer[index % BLOCK_BYTE_SIZE];
        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            index++;
            int blockIndex = index / BLOCK_BYTE_SIZE;
            if (blockIndex != blockBufferIndex)
            {
                try
                {
                    blockBuffer = ((ReadReply)this.Tag.Execute(new ReadCommand() { Index = (byte)blockIndex })).Data;
                    blockBufferIndex = blockIndex;
                }
                catch (Exception e) { return false; }
            }
            return true;
        }

        public void Reset()
        {
            this.index = DEFAULT_INDEX;
        }
    }

    public enum TagField
    {
        Null = 0x00,
        LockControl = 0x01,
        MemoryControl = 0x02,
        NDEFMessage = 0x03,
        Proprietary = 0xFD,
        Terminator = 0xFE
    }
    public abstract class TLVBlock
    {
        public readonly TagField TagField;

        public TLVBlock(TagField tagField)
        {
            this.TagField = tagField;
        }

        public int? Length => this.value?.Length == 0 ? null : this.value?.Length;

        protected byte[] value;
        public virtual byte[] Value
        {
            get => this.value;
            set => this.value = value;
        }

        public const byte MORE_LENGTH_INDICATOR = 0xFF;
    }
    public class NullTLVBlock : TLVBlock
    {
        public NullTLVBlock() : base(TagField.Null) { }
    }
    public abstract class ControlTLVBlock : TLVBlock
    {
        public const byte LENGTH = 3;

        public ControlTLVBlock(TagField tagField) : base(tagField) { }

        public override byte[] Value
        {
            get => base.Value;
            set
            {
                if (value.Length != LENGTH)
                    throw new ArgumentOutOfRangeException("Control TLV block value must be 3 bytes long");
                this.value = value;
            }
        }

        public const byte POSITION_BYTE_VALUE_INDEX = 2;
        public const byte POSITION_BYTE_PAGE_ADDRESS_BIT_MASK = 0b11110000;
        public const byte POSITION_BYTE_PAGE_ADDRESS_BIT_INDEX = 4;
        public const byte POSITION_BYTE_BYTE_OFFSET_BIT_MASK = 0b00001111;
        public const byte POSITION_BYTE_BYTE_OFFSET_BIT_INDEX = 0;
        public byte PageAddress => (byte)((this.value[POSITION_BYTE_VALUE_INDEX] | POSITION_BYTE_PAGE_ADDRESS_BIT_MASK) >> POSITION_BYTE_PAGE_ADDRESS_BIT_INDEX);
        public byte ByteOffset => (byte)((this.value[POSITION_BYTE_VALUE_INDEX] | POSITION_BYTE_BYTE_OFFSET_BIT_MASK) >> POSITION_BYTE_BYTE_OFFSET_BIT_INDEX);

        public const byte SIZE_BYTE_VALUE_INDEX = 1;
        public uint Size => (uint)(this.value[SIZE_BYTE_VALUE_INDEX] == 0x00 ? 256 : this.value[SIZE_BYTE_VALUE_INDEX]);

        public const byte PAGE_CONTROL_BYTE_VALUE_INDEX = 0;
        public const byte PAGE_CONTROL_BYTES_PER_PAGE_BIT_MASK = 0b00001111;
        public const byte PAGE_CONTROL_BYTES_PER_PAGE_BIT_INDEX = 0;
        public byte BytesPerPage => (byte)((this.value[PAGE_CONTROL_BYTE_VALUE_INDEX] | PAGE_CONTROL_BYTES_PER_PAGE_BIT_MASK) >> PAGE_CONTROL_BYTES_PER_PAGE_BIT_INDEX);

        public uint ByteAddress => (uint)((this.PageAddress * (1 << this.BytesPerPage)) + this.ByteOffset);
    }
    public class LockControlTLVBlock : ControlTLVBlock
    {
        public LockControlTLVBlock() : base(TagField.LockControl) { }

        public const byte PAGE_CONTROL_BYTES_LOCKED_PER_LOCK_BIT_BIT_MASK = 0b11110000;
        public const byte PAGE_CONTROL_BYTES_LOCKED_PER_LOCK_BIT_BIT_INDEX = 4;
        public byte BytesLockedPerLockBit => (byte)((this.value[PAGE_CONTROL_BYTE_VALUE_INDEX] | PAGE_CONTROL_BYTES_LOCKED_PER_LOCK_BIT_BIT_MASK) >> PAGE_CONTROL_BYTES_LOCKED_PER_LOCK_BIT_BIT_INDEX);
    }
    public class MemoryControlTLVBlock : ControlTLVBlock
    {
        public MemoryControlTLVBlock() : base(TagField.MemoryControl) { }
    }
    public abstract class DataTLVBlock : TLVBlock
    {
        public DataTLVBlock(TagField tagField) : base(tagField) { }
    }
    public class NDEFTLVBlock : DataTLVBlock
    {
        public NDEFTLVBlock() : base(TagField.NDEFMessage) { }

        //TODO: add NDEF parsing methods
        public NDEFRecord NDEFRecord => new NDEFRecord(this.Value);
    }
    public class ProprietaryTLVBlock : DataTLVBlock
    {
        public ProprietaryTLVBlock() : base(TagField.Proprietary) { }
    }
    /// <summary>
    /// (It will be back...)
    /// </summary>
    public class TerminatorTLVBlock : TLVBlock
    {
        public TerminatorTLVBlock() : base(TagField.Terminator) { }
    }
}
