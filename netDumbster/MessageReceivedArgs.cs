// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp;

public class MessageReceivedArgs : EventArgs
{
    public MessageReceivedArgs(SmtpMessage message)
    {
        Message = message;
    }

    public SmtpMessage Message
    {
        get;
        set;
    }
}
