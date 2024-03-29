// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp;

public static class MailMessageExtensions
{
    public static SmtpMessagePart[] Parts(this MailMessage mailMessage)
    {
        var parts = new List<SmtpMessagePart>();

        if (mailMessage.BodyEncoding != null)
        {
            var part = new SmtpMessagePart(mailMessage.BodyEncoding.ToString(), mailMessage.Body);
            parts.Add(part);
        }

        foreach (AlternateView alternateView in mailMessage.AlternateViews)
        {
            var part = new SmtpMessagePart(alternateView.ContentType.ToString(), StreamToString(alternateView.ContentStream));
            parts.Add(part);
        }

        foreach (Attachment attachment in mailMessage.Attachments)
        {
            var part = new SmtpMessagePart(attachment.ContentType.ToString(), StreamToString(attachment.ContentStream));
            parts.Add(part);
        }

        return [.. parts];
    }

    private static string StreamToString(Stream stream)
    {
        var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
