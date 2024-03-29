namespace Extensions;

public static class MimeKitExtensions
{
    static System.Net.Mime.ContentType GetContentType(MimeKit.ContentType contentType)
    {
        var ctype = new System.Net.Mime.ContentType
        {
            MediaType = string.Format("{0}/{1}", contentType.MediaType, contentType.MediaSubtype)
        };

        foreach (var param in contentType.Parameters)
            ctype.Parameters.Add(param.Name, param.Value);

        return ctype;
    }

    static TransferEncoding GetTransferEncoding(MimeKit.ContentEncoding encoding)
    {
        return encoding switch
        {
            MimeKit.ContentEncoding.QuotedPrintable or MimeKit.ContentEncoding.EightBit => TransferEncoding.QuotedPrintable,
            MimeKit.ContentEncoding.SevenBit => TransferEncoding.SevenBit,
            _ => TransferEncoding.Base64,
        };
    }

    static void AddBodyPart(MailMessage message, MimeKit.MimeEntity entity)
    {
        if (entity is MimeKit.MessagePart)
        {
            // FIXME: how should this be converted into a MailMessage?
        }
        else if (entity is MimeKit.Multipart multipart)
        {
            if (multipart.ContentType.IsMimeType("multipart", "alternative"))
            {
                foreach (var part in multipart.OfType<MimeKit.MimePart>())
                {
                    // clone the content
                    var content = new MemoryStream();
                    part.Content.DecodeTo(content);
                    content.Position = 0;

                    var view = new AlternateView(content, GetContentType(part.ContentType))
                    {
                        TransferEncoding = GetTransferEncoding(part.ContentTransferEncoding)
                    };
                    if (!string.IsNullOrEmpty(part.ContentId))
                        view.ContentId = part.ContentId;

                    message.AlternateViews.Add(view);
                }
            }
            else
            {
                foreach (var part in multipart)
                    AddBodyPart(message, part);
            }
        }
        else
        {
            var part = (MimeKit.MimePart)entity;

            if (part.IsAttachment || !string.IsNullOrEmpty(message.Body) || part is not MimeKit.TextPart textPart)
            {
                // clone the content
                var content = new MemoryStream();
                part.Content.DecodeTo(content);
                content.Position = 0;

                var attachment = new Attachment(content, GetContentType(part.ContentType));

                if (part.ContentDisposition != null)
                {
                    attachment.ContentDisposition.DispositionType = part.ContentDisposition.Disposition;
                    foreach (var param in part.ContentDisposition.Parameters)
                        attachment.ContentDisposition.Parameters.Add(param.Name, param.Value);
                }

                attachment.TransferEncoding = GetTransferEncoding(part.ContentTransferEncoding);

                if (!string.IsNullOrEmpty(part.ContentId))
                    attachment.ContentId = part.ContentId;

                message.Attachments.Add(attachment);
            }
            else
            {
                message.IsBodyHtml = part.ContentType.IsMimeType("text", "html");
                message.Body = textPart.Text;
            }
        }
    }

    static MailAddress ConvertToMailAddress(MimeKit.MailboxAddress address)
    {
        return new MailAddress(address.Address, address.Name ?? string.Empty, address.Encoding ?? Encoding.UTF8);
    }

    /// <summary>
    /// Explicit cast to convert a <see cref="MimeKit.MimeMessage"/> to a
    /// <see cref="MailMessage"/>.
    /// </summary>
    /// <remarks>
    /// <para>Casting a <see cref="MimeKit.MimeMessage"/> to a <see cref="MailMessage"/>
    /// makes it possible to use MimeKit with <see cref="SmtpClient"/>.</para>
    /// <para>It should be noted, however, that <see cref="MailMessage"/>
    /// cannot represent all MIME structures that can be constructed using MimeKit,
    /// so the conversion may not be perfect.</para>
    /// <para>A better approach would be to use MailKit's SmtpClient instead.</para>
    /// </remarks>
    /// <returns>A <see cref="MailMessage"/>.</returns>
    /// <param name="message">The message.</param>
    public static MailMessage ConvertToMailMessage(this MimeKit.MimeMessage message)
    {
        var msg = new MailMessage();

        var from = message.From.Mailboxes.FirstOrDefault();

        var sender = message.Sender;

        foreach (var header in message.Headers)
        {
            if (string.IsNullOrEmpty(header.Value)) continue;
            msg.Headers.Add(header.Field, header.Value);
        }

        if (sender != null)
            msg.Sender = ConvertToMailAddress(sender);

        if (from != null)
            msg.From = ConvertToMailAddress(from);

        foreach (var mailbox in message.ReplyTo.Mailboxes)
            msg.ReplyToList.Add(ConvertToMailAddress(mailbox));

        foreach (var mailbox in message.To.Mailboxes)
            msg.To.Add(ConvertToMailAddress(mailbox));

        foreach (var mailbox in message.Cc.Mailboxes)
            msg.CC.Add(ConvertToMailAddress(mailbox));

        foreach (var mailbox in message.Bcc.Mailboxes)
            msg.Bcc.Add(ConvertToMailAddress(mailbox));

        msg.Subject = message.Subject;

        if (message.Body != null)
            AddBodyPart(msg, message.Body);

        return msg;
    }
}
