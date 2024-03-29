// Copyright (c) 2009 snarum, (http://mimeparser.codeplex.com/)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

public static class MailMessageMimeParser
{
    public static MailMessage ParseMessage(string mimeMail)
    {
        var mimeMessage = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(mimeMail)));

        return mimeMessage.ConvertToMailMessage();
    }

    public static MailMessage ParseMessage(StringReader mimeMail)
    {
        return ParseMessage(mimeMail.ReadToEnd());
    }
}
