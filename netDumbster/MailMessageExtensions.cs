// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Mail;

    public static class MailMessageExtensions
    {
        public static SmtpMessagePart[] Parts(this MailMessage mailMessage)
        {
            List<SmtpMessagePart> parts = new List<SmtpMessagePart>();

            if (mailMessage.BodyEncoding != null)
            {
                SmtpMessagePart part = new SmtpMessagePart(mailMessage.BodyEncoding.ToString(), mailMessage.Body);
                parts.Add(part);
            }

            foreach (AlternateView alternateView in mailMessage.AlternateViews)
            {
                SmtpMessagePart part = new SmtpMessagePart(alternateView.ContentType.ToString(), StreamToString(alternateView.ContentStream));
                parts.Add(part);
            }

            foreach (Attachment attachment in mailMessage.Attachments)
            {
                SmtpMessagePart part = new SmtpMessagePart(attachment.ContentType.ToString(), StreamToString(attachment.ContentStream));
                parts.Add(part);
            }

            return parts.ToArray();
        }

        private static string StreamToString(Stream stream)
        {
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}