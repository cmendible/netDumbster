#region Header

// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

#endregion Header

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