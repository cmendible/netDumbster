#region Header

// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

#endregion Header

namespace netDumbster.smtp
{
    using System;

    public class MessageReceivedArgs : EventArgs
    {
        #region Constructors

        public MessageReceivedArgs(SmtpMessage message)
        {
            Message = message;
        }

        #endregion Constructors

        #region Properties

        public SmtpMessage Message
        {
            get;
            set;
        }

        #endregion Properties
    }
}