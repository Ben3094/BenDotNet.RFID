using System;
using System.Collections;

namespace BenDotNet.RFID.NFC.NFC_V
{
    public abstract class Command : NFC.Command
    {
        public Command(NFC.Tag targetTag) : base() { }

        public const byte IS_1_SLOT_MODE_FLAG_INDEX = 5;
        public bool Is1SlotMode => this.is1SlotMode;
        protected bool is1SlotMode;
        public const byte IS_INVENTORY_REQUESTED_FLAG_INDEX = 2;
        public bool IsInventoryRequested => this.isInventoryRequested;
        protected bool isInventoryRequested;
        public const byte HIGH_DATA_RATE_FLAG_INDEX = 1;
        public bool HighDataRate => this.highDataRate;
        protected bool highDataRate;
        public const byte RESPONSE_TO_USE_SINGLE_SUBCARRIER_FLAG_INDEX = 0;
        public bool ResponseToUseSingleSubcarrier => this.responseToUseSingleSubcarrier;
        protected bool responseToUseSingleSubcarrier;

        public byte Flags => (byte)(
                                       ((this.Is1SlotMode ? 0b1 : 0b0) << IS_1_SLOT_MODE_FLAG_INDEX)
                                       & ((this.IsInventoryRequested ? 0b1 : 0b0) << IS_INVENTORY_REQUESTED_FLAG_INDEX)
                                       & ((this.HighDataRate ? 0b1 : 0b0) << HIGH_DATA_RATE_FLAG_INDEX)
                                       & ((this.ResponseToUseSingleSubcarrier ? 0b0 : 0b1) << RESPONSE_TO_USE_SINGLE_SUBCARRIER_FLAG_INDEX)
                                   );

        public override BitArray CompiledCommand => new BitArray(this.commandCode);
        protected byte[] commandCode;

        public readonly NFC.Tag TargetTag;

        public abstract byte[] Parameters
        {
            get;
        }

        public override byte[] BytesCompiledCommand
        {
            get
            {
                byte[] result = new byte[1 + this.commandCode.Length + this.TargetTag.UID.Length + this.Parameters.Length];
                result[0] = this.Flags;
                Array.Copy(this.commandCode, 0, result, 1, this.commandCode.Length);
                Array.Copy(this.TargetTag.UID, 0, result, 1 + this.commandCode.Length, this.TargetTag.UID.Length);
                Array.Copy(this.Parameters, 0, result, 1 + this.commandCode.Length + this.TargetTag.UID.Length, this.Parameters.Length);
                return result;
            }
        }
    }

    public abstract class Type5Command : Command
    {
        public Type5Command(NFC.Tag targetTag) : base(targetTag)
        {
            this.is1SlotMode = true;
            this.isInventoryRequested = false;
            this.highDataRate = false;
            this.responseToUseSingleSubcarrier = true;
        }
    }


    public abstract class Reply : RFID.Reply
    {
        public Reply(ref RFID.Command associatedCommand, byte[] compiledReply) : base(ref associatedCommand) { this.CompiledReply = compiledReply; }
        public Reply(ref RFID.Command associatedCommand, byte[] compiledReply, ref object originalReply) : base(ref associatedCommand, ref originalReply) { this.CompiledReply = compiledReply; }

        public readonly byte[] CompiledReply;
    }
}
