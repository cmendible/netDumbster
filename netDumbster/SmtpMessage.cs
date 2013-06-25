#region Header

// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

#endregion Header

namespace netDumbster.smtp
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net.Mail;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Stores an incoming SMTP Message.
    /// </summary>
    public class SmtpMessage
    {
        #region Fields

        private static readonly string DOUBLE_NEWLINE = Environment.NewLine + Environment.NewLine;

        private RawSmtpMessage rawSmtpMessage;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Creates a new message.
        /// </summary>
        public SmtpMessage(RawSmtpMessage rawSmtpMessage)
        {
            this.rawSmtpMessage = rawSmtpMessage;
            using (MailMessage mailMessage = MailMessageMimeParser.ParseMessage(new System.IO.StringReader(this.rawSmtpMessage.Data.ToString())))
            {
                this.Headers = mailMessage.Headers;
                this.FromAddress = new EmailAddress(mailMessage.From.Address);
                this.ToAddresses = this.rawSmtpMessage.Recipients.ToArray();
                this.MessageParts = mailMessage.Parts();
            }
        }

        #endregion Constructors

        #region Properties

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
                    return Headers["importance"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
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
                    return Headers["priority"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
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
                if (Headers.AllKeys.Select(k => k.ToLowerInvariant()).Contains("x-priority"))
                {
                    return Headers["x-priority"].ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        #endregion Properties
    }
}