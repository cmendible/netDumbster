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
            var headers = GetAttachmentHeaders(alternateView);
            var part = new SmtpMessagePart(headers, StreamToString(alternateView.ContentStream));
            parts.Add(part);
        }

        foreach (Attachment attachment in mailMessage.Attachments)
        {
            var headers = GetAttachmentHeaders(attachment);
            var part = new SmtpMessagePart(headers, StreamToString(attachment.ContentStream));
            parts.Add(part);
        }

        return [.. parts];
    }

    private static string StreamToString(Stream stream)
    {
        var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string GetAttachmentHeaders(AttachmentBase attachmentBase)
    {
        var headers = new StringBuilder();
        if (!string.IsNullOrEmpty(attachmentBase.ContentType.ToString()))
        {
            headers.AppendLine($"Content-Type: {attachmentBase.ContentType.ToString()}");
        }
        if (!string.IsNullOrEmpty(attachmentBase.TransferEncoding.ToString()))
        {
            headers.AppendLine($"Content-Transfer-Encoding: {attachmentBase.TransferEncoding.ToString()}");
        }
        if (!string.IsNullOrEmpty(attachmentBase.ContentId.ToString()))
        {
            headers.AppendLine($"Content-ID: {attachmentBase.ContentId.ToString()}");
        }

        if (attachmentBase is Attachment attachment)
        {
            if (!string.IsNullOrEmpty(attachment.ContentDisposition.ToString()))
            {
                headers.AppendLine("Content-Disposition: " + attachment.ContentDisposition.ToString());
            }
        }

        return headers.ToString();
    }
}
