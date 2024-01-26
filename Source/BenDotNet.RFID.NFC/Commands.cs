using System.Collections;

namespace BenDotNet.RFID.NFC
{
    public abstract class Command : RFID.Command
    {
        public override BitArray CompiledCommand => new BitArray(this.BytesCompiledCommand);
        public abstract byte[] BytesCompiledCommand { get; }
    }

    public abstract class Reply : RFID.Reply
    {
        public Reply(ref RFID.Command associatedCommand, byte[] compiledReply = null) : base(ref associatedCommand)
        {
            this.CompiledReply = compiledReply;
        }

        public abstract byte[] CompiledReply { get; set; }
    }
}
