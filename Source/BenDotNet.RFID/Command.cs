using System;
using System.Collections;

namespace BenDotNet.RFID
{
    public abstract class Command
    {
        public abstract BitArray CompiledCommand { get; }
    }

    public class Reply
    {
        public Reply(ref Command associatedCommand)
        {
            this.OriginalReply = null;
            this.AssociatedCommand = associatedCommand;
        }
        public Reply(ref Command associatedCommand, ref object originalReply)
        {
            this.AssociatedCommand = associatedCommand;
            this.OriginalReply = originalReply;
        }

        public readonly object OriginalReply;
        public readonly Command AssociatedCommand;

        public static Type AssociatedCommandType = null;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ReplyTypeAttribute : Attribute
    {
        public readonly Type PossibleReplyTypes;

        public ReplyTypeAttribute(Type possibleReplyTypes)
        {
            if (typeof(Reply).IsAssignableFrom(possibleReplyTypes) && (possibleReplyTypes != typeof(Reply)))
                this.PossibleReplyTypes = possibleReplyTypes;
            else
                throw new ArgumentException("Is not a Reply derived type");
        }
    }

    public class ErrorRepliedException : Exception
    {
        public readonly Reply ErrorReply;
        public ErrorRepliedException(Reply errorReply, Exception innerException = null) : base("", innerException)
        {
            this.ErrorReply = errorReply;
        }
    }
}
