﻿// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
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
        public SmtpMessagePart(string header, string body)
        {
            this.HeaderData = header;
            this.BodyData = body;
        }

        /// <summary>
        /// The raw text that represents the actual mime part.
        /// </summary>
        public string BodyData
        {
            get;
        }

        /// <summary>
        /// The raw text that represents the header of the mime part.
        /// </summary>
        public string HeaderData
        {
            get;
        }
    }
}