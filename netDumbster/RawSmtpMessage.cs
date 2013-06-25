#region Header

// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

#endregion Header

namespace netDumbster.smtp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using netDumbster.smtp;

    public class RawSmtpMessage
    {
        List<EmailAddress> recipients;

        public RawSmtpMessage()
        {
            this.Data = new StringBuilder();
            this.recipients = new List<EmailAddress>();
        }
        
        public StringBuilder Data
        {
            get;
            private set;
        }

        public IEnumerable<EmailAddress> Recipients
        {
            get
            {
                return this.recipients;
            }
        }

        public void AddRecipient(EmailAddress recipient)
        {
            this.recipients.Add(recipient);
        }
    }
}
