// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

/// <summary>
/// Stores an incoming SMTP Message.
/// </summary>
public class SmtpMessage
{
    private readonly RawSmtpMessage rawSmtpMessage;

    /// <summary>
    /// Creates a new message.
    /// </summary>
    public SmtpMessage(RawSmtpMessage rawSmtpMessage)
    {
        this.rawSmtpMessage = rawSmtpMessage;
        var rawMessage = this.rawSmtpMessage.Data.ToString();
        rawMessage = rawMessage.TrimEnd('\r', '\n');
        using MailMessage mailMessage = MailMessageMimeParser.ParseMessage(rawMessage);
        Headers = mailMessage.Headers;
        FromAddress = new EmailAddress(mailMessage.From.Address);
        ReplyToAddresses = mailMessage.ReplyToList
            .Select(m => new EmailAddress(m.Address)).ToArray();
        ToAddresses = mailMessage.To
            .Select(m => new EmailAddress(m.Address)).ToArray();
        CcAddresses = mailMessage.CC
            .Select(m => new EmailAddress(m.Address)).ToArray();
        // Bcc recipients are not part of the SMTP data object, so we need to calculate them
        var comparer = new EmailAddressComparer();
        BccAddresses = rawSmtpMessage.Recipients.Except(ToAddresses, comparer).Except(CcAddresses, comparer).ToArray();
        MessageParts = mailMessage.Parts();
        LocalIPAddress = rawSmtpMessage.LocalIPAddress;
        LocalPort = rawSmtpMessage.LocalPort;
        RemoteIPAddress = rawSmtpMessage.RemoteIPAddress;
        RemotePort = rawSmtpMessage.RemotePort;
        Subject = mailMessage.Subject;
    }

    /// <summary>Message data.</summary>
    public string Data
    {
        get
        {
            return rawSmtpMessage.Data.ToString();
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
            if (Headers.AllKeys.SkipWhile(k => k is null).Select(k => k.ToLowerInvariant()).Contains("importance"))
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
            if (Headers.AllKeys.SkipWhile(k => k is null).Select(k => k.ToLowerInvariant()).Contains("priority"))
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
    /// Subject
    /// </summary>
    public string Subject
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

    /// <summary>
    /// The addresses that this message will be
    /// delivered to in CC
    /// </summary>
    public EmailAddress[] CcAddresses
    {
        get;
        private set;
    }

    /// <summary>
    /// The addresses that this message will be
    /// delivered to in BCC
    /// </summary>
    public EmailAddress[] BccAddresses
    {
        get;
        private set;
    }

    /// <summary>
    /// The addresses that this message will be
    /// replied to.
    /// </summary>
    public EmailAddress[] ReplyToAddresses
    {
        get;
        private set;
    }

    public string XPriority
    {
        get
        {
            if (Headers.AllKeys.SkipWhile(k => k is null).Select(k => k.ToLowerInvariant()).Contains("x-priority"))
            {
                return Headers["x-priority"].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
