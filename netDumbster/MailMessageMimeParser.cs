// Copyright (c) 2009 snarum, (http://mimeparser.codeplex.com/)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net.Mail;
    using System.Net.Mime;
    using System.Text;
    using System.Text.RegularExpressions;
    using MimeKit;
    using Extensions;

    public static class MailMessageMimeParser
    {
        public static MailMessage ParseMessage(string mimeMail)
        {
            var mimeMessage = MimeMessage.Load(new MemoryStream(UTF8Encoding.UTF8.GetBytes(mimeMail)));

            return mimeMessage.ConvertToMailMessage();
        }

        public static MailMessage ParseMessage(StringReader mimeMail)
        {
            return ParseMessage(mimeMail.ReadToEnd());
        }
    }
}