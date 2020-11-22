// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp
{
    using System.Collections.Specialized;

    /// <summary>
    /// Stores a single part of a multipart message.
    /// </summary>
    public class SmtpMessagePart
    {
        private string bodyData = string.Empty;
        private string headerData = string.Empty;
        private NameValueCollection headerFields = null;

        public SmtpMessagePart(string header, string body)
        {
            this.headerData = header;
            this.bodyData = body;
        }

        /// <summary>
        /// The raw text that represents the actual mime part.
        /// </summary>
        public string BodyData
        {
            get
            {
                return this.bodyData;
            }
        }

        /// <summary>
        /// The raw text that represents the header of the mime part.
        /// </summary>
        public string HeaderData
        {
            get
            {
                return this.headerData;
            }
        }

        /// <summary>
        /// A hash table of all the Headers in the email message.  They keys
        /// are the header names, and the values are the assoicated values, including
        /// any sub key/value pairs is the header.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                if (this.headerFields == null)
                {
                    // headerFields = SmtpMessage.ParseHeaders( headerData );
                }

                return this.headerFields;
            }
        }
    }
}