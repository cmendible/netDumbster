namespace netDumbster.smtp
{
    using System;

    public class MessageReceivedArgs : EventArgs
    {
        public SmtpMessage Message { get; set; }

        public MessageReceivedArgs(SmtpMessage message)
        {
            Message = message;
        }
    }
}