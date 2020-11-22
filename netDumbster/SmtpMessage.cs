// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp
{
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Mail;

    /// <summary>
    /// Stores an incoming SMTP Message.
    /// </summary>
    public class SmtpMessage
    {
        private RawSmtpMessage rawSmtpMessage;

        /// <summary>
        /// Creates a new message.
        /// </summary>
        public SmtpMessage(RawSmtpMessage rawSmtpMessage)
        {
            this.rawSmtpMessage = rawSmtpMessage;
            var rawMessage = this.rawSmtpMessage.Data.ToString();
            rawMessage = rawMessage.TrimEnd('\r', '\n');
            using (MailMessage mailMessage = MailMessageMimeParser.ParseMessage(rawMessage))
            {
                this.Headers = mailMessage.Headers;
                this.FromAddress = new EmailAddress(mailMessage.From.Address);
                this.ToAddresses = rawSmtpMessage.Recipients.ToArray();
                this.MessageParts = mailMessage.Parts();
                this.LocalIPAddress = rawSmtpMessage.LocalIPAddress;
                this.LocalPort = rawSmtpMessage.LocalPort;
                this.RemoteIPAddress = rawSmtpMessage.RemoteIPAddress;
                this.RemotePort = rawSmtpMessage.RemotePort;
            }
        }

        /// <summary>Message data.</summary>
        public string Data
        {
            get
            {
                return this.rawSmtpMessage.Data.ToString();
            }
        }

        /// <summary>
        /// The email address of the person
        /// that sent this email.
        /// </summary>
        public EmailAddress FromAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// A hash table of all the Headers in the email message.  They keys
        /// are the header names, and the values are the assoicated values, including
        /// any sub key/value pairs is the header.
        /// </summary>
        public NameValueCollection Headers
        {
            get;
            private set;
        }

        public string Importance
        {
            get
            {
                if (this.Headers.AllKeys.Select(k => k.ToLowerInvariant()).Contains("importance"))
                {
                    return this.Headers["importance"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the local IP address.
        /// </summary>
        public IPAddress LocalIPAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the local port.
        /// </summary>
        public int LocalPort
        {
            get;
            private set;
        }

        /// <summary>
        /// Parses the message body and creates an Attachment object
        /// for each attachment in the message.
        /// </summary>
        public SmtpMessagePart[] MessageParts
        {
            get;
            private set;
        }

        public string Priority
        {
            get
            {
                if (this.Headers.AllKeys.Select(k => k.ToLowerInvariant()).Contains("priority"))
                {
                    return this.Headers["priority"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the remote IP address.
        /// </summary>
        public IPAddress RemoteIPAddress
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the remote port.
        /// </summary>
        public int RemotePort
        {
            get;
            private set;
        }

        /// <summary>
        /// The addresses that this message will be
        /// delivered to.
        /// </summary>
        public EmailAddress[] ToAddresses
        {
            get;
            private set;
        }

        public string XPriority
        {
            get
            {
                if (this.Headers.AllKeys.Select(k => k.ToLowerInvariant()).Contains("x-priority"))
                {
                    return this.Headers["x-priority"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}