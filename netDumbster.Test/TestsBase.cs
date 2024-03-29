namespace netDumbster.Test;

public class TestsBase : IDisposable
{
    protected SimpleSmtpServer server;

    public TestsBase()
    {
        LogManager.GetLogger = type => new ConsoleLogger(type);
        server = StartServer();
    }

    protected virtual SimpleSmtpServer StartServer()
    {
        return SimpleSmtpServer.Start();
    }

    protected virtual SimpleSmtpServer StartServer(int port)
    {
        return SimpleSmtpServer.Start(port);
    }

    [Fact]
    public void Subject_Is_Not_Empty()
    {
        SendMail();
        Assert.Equal("test", server.ReceivedEmail[0].Subject);
    }

    [Fact]
    public void Addresses_Are_Correct()
    {
        using SmtpClient client = new();
        client.Connect("localhost", server.Configuration.Port, false);
        var from = new MailboxAddress("from", "carlos@netdumbster.com");
        var to = new[]
        {
                new MailboxAddress("to-1", "karina@netdumbster.com"),
                new MailboxAddress("to-2", "john@netdumbster.com"),
            };
        var replyTo = new[]
        {
                new MailboxAddress("replyTo-1", "jane@netdumbster.com"),
                new MailboxAddress("replyTo-2", "edward@netdumbster.com"),
            };
        var cc = new[] {
                new MailboxAddress("cc-1", "ludmilla@netdumbster.com"),
                new MailboxAddress("cc-2", "edgar@netdumbster.com"),
            };
        var bcc = new[] {
                new MailboxAddress("bcc-1", "bernd@netdumbster.com"),
                new MailboxAddress("bcc-2", "brenda@netdumbster.com"),
            };

        var message = new MimeMessage();

        message.From.Add(from);
        message.To.AddRange(to);
        message.ReplyTo.AddRange(replyTo);
        message.Cc.AddRange(cc);
        message.Bcc.AddRange(bcc);
        message.Subject = "test";

        client.Send(message);

        var receivedMessage = server.ReceivedEmail[0];

        Assert.Equal(receivedMessage.FromAddress.Address, from.Address);
        Assert.Equal(
            receivedMessage.ToAddresses.Select(a => a.Address),
            to.Select(a => a.Address)
        );
        Assert.Equal(
            receivedMessage.ReplyToAddresses.Select(a => a.Address),
            replyTo.Select(a => a.Address)
        );
        Assert.Equal(
            receivedMessage.CcAddresses.Select(a => a.Address),
            cc.Select(a => a.Address)
        );
        Assert.Equal(
            receivedMessage.BccAddresses.Select(a => a.Address),
            bcc.Select(a => a.Address)
        );
    }

    [Fact]
    public void Send_10_Mails()
    {
        for (var i = 0; i < 10; i++)
        {
            SendMail();
            Assert.Equal(i + 1, server.ReceivedEmailCount);
        }

        Assert.Equal(10, server.ReceivedEmailCount);
    }

    [Fact]
    public void Send_10_Mail_With_SmtpAuth()
    {
        for (var i = 0; i < 10; i++)
        {
            SendMail(true);
            Assert.Equal(i + 1, server.ReceivedEmailCount);
        }

        Assert.Equal(10, server.ReceivedEmailCount);
    }

    [Fact]
    public void Send_Email_And_Restart_Server_Using_The_Same_Port()
    {
        int port = 5003;
        SimpleSmtpServer fixedPortServer = StartServer(port);

        SendMail(false, false, null, port);

        Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
        Assert.Equal("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

        fixedPortServer.Stop();

        fixedPortServer = StartServer(port);

        SendMail(false, false, null, port);

        Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
        Assert.Equal("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

        fixedPortServer.Stop();
    }

    [Fact]
    public void Send_Email_When_Server_Not_Running()
    {
        server.Stop();
        var ex = Record.Exception(() => SendMail());
        Assert.IsAssignableFrom<SocketException>(ex);
    }

    [Fact]
    public void Send_Email_With_AlternateViews()
    {
        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";

            var builder = new BodyBuilder
            {
                TextBody = "this is the body",
                HtmlBody = "FooBar"
            };

            message.Body = builder.ToMessageBody();

            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        var smtpMessage = server.ReceivedEmail[0];

        Assert.Equal(2, smtpMessage.MessageParts.Length);
        Assert.Contains("text/plain", smtpMessage.MessageParts[0].HeaderData);
        Assert.Equal("this is the body", smtpMessage.MessageParts[0].BodyData);
        Assert.Contains("text/html", smtpMessage.MessageParts[1].HeaderData);
        Assert.Equal("FooBar", smtpMessage.MessageParts[1].BodyData);
    }

    [Fact]
    public void Send_Email_With_AlternateViews_And_Attachments()
    {
        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";

            var builder = new BodyBuilder
            {
                TextBody = "this is the body",
                HtmlBody = "FooBar"
            };

            builder.Attachments.Add("Attachment1", Encoding.UTF8.GetBytes("Attachment1"),
                ContentType.Parse("application/octet-stream"));
            builder.Attachments.Add("Attachment2", Encoding.UTF8.GetBytes("Attachment2"),
                ContentType.Parse("application/octet-stream"));
            builder.Attachments.Add("Attachment3", Encoding.UTF8.GetBytes("Attachment3"),
                ContentType.Parse("application/octet-stream"));

            message.Body = builder.ToMessageBody();

            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        var smtpMessage = server.ReceivedEmail[0];
        Assert.Equal(5, smtpMessage.MessageParts.Length);
        Assert.Contains("text/plain", smtpMessage.MessageParts[0].HeaderData);
        Assert.Equal("this is the body", smtpMessage.MessageParts[0].BodyData);
        Assert.Contains("text/html", smtpMessage.MessageParts[1].HeaderData);
        Assert.Equal("FooBar", smtpMessage.MessageParts[1].BodyData);
    }

    [Fact]
    public void Send_Email_With_Attachment()
    {
        var data = new byte[] { 0x1 };

        SendMail(false, true, data);
        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal("this is the html body", server.ReceivedEmail[0].MessageParts[0].BodyData);
        Assert.NotNull(server.ReceivedEmail[0].MessageParts[1]);
        Assert.NotNull(server.ReceivedEmail[0].MessageParts[1].BodyData);
        Assert.NotEmpty(server.ReceivedEmail[0].MessageParts[1].BodyData);
        Assert.Equal(data, Encoding.UTF8.GetBytes(server.ReceivedEmail[0].MessageParts[1].BodyData));
    }

    [Fact]
    public void Send_Multiline_Email()
    {
        var expectedBody = $"this is the body{Environment.NewLine}line2{Environment.NewLine}line3";

        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";

            var builder = new BodyBuilder
            {
                TextBody = expectedBody
            };

            message.Body = builder.ToMessageBody();
            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal(expectedBody, server.ReceivedEmail[0].MessageParts[0].BodyData);
    }

    [Fact]
    public void Send_Email_With_Priority()
    {
        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";
            message.Body = new TextPart("plain") { Text = "this is the body" };
            message.Priority = MessagePriority.Urgent;
            message.XPriority = XMessagePriority.Highest;
            message.Importance = MessageImportance.High;

            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
        Assert.Equal("1 (Highest)", server.ReceivedEmail[0].XPriority);
        Assert.Equal("urgent", server.ReceivedEmail[0].Priority);
        Assert.Equal("high", server.ReceivedEmail[0].Importance);
    }

    [Fact]
    public void Send_Email_With_RussianText()
    {
        string body = string.Empty;
        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";

            var builder = new BodyBuilder();
            body = "Съешь ещё этих мягких французских булок, да выпей чаю" +
                   "Съешь ещё этих мягких французских булок, да выпей чаю" +
                   "Съешь ещё этих мягких французских булок, да выпей чаю" +
                   "Съешь ещё этих мягких французских булок, да выпей чаю" +
                   "Съешь ещё этих мягких французских булок, да выпей чаю" +
                   "Съешь ещё этих мягких французских булок, да выпей чаю";
            builder.TextBody = body;

            message.Body = builder.ToMessageBody();

            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal(body, server.ReceivedEmail[0].MessageParts[0].BodyData);

        server.Stop();
    }

    [Fact]
    public void Send_Email_With_UTF8_Chars()
    {
        string body = string.Empty;
        using (SmtpClient client = new())
        {
            client.Connect("localhost", server.Configuration.Port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "test";

            var builder = new BodyBuilder();
            body = "µ¶®¥§";
            builder.TextBody = body;

            message.Body = builder.ToMessageBody();

            client.Send(message);
        }

        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal(body, server.ReceivedEmail[0].MessageParts[0].BodyData);

        server.Stop();
    }

    [Fact]
    public void Send_Fires_Message_Received_Event()
    {
        int port = GetRandomUnusedPort();
        SimpleSmtpServer fixedPortServer = StartServer(port);
        fixedPortServer.MessageReceived += (sender, args) =>
        {
            Assert.NotNull(args.Message);
            Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
            Assert.Equal("this is the body", args.Message.MessageParts[0].BodyData);
        };

        SendMail(false, false, null, port);

        fixedPortServer.Stop();
    }

    [Fact]
    public void Send_Html_Email()
    {
        SendMail(false, true, null);
        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal("this is the html body", server.ReceivedEmail[0].MessageParts[0].BodyData);
    }

    [Fact]
    public void Send_One_Mail()
    {
        SendMail();
        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
    }

    [Fact]
    public void If_Client_Is_Not_Disposed_Server_Everything_Keeps_Working()
    {
        var port = GetRandomUnusedPort();
        var host = "localhost";
        using (SimpleSmtpServer emailServer = StartServer(port))
        {
            var client = new SmtpClient();
            client.Connect(host, port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "This is an email";
            var builder = new BodyBuilder
            {
                TextBody = "body of email"
            };
            message.Body = builder.ToMessageBody();

            client.Send(message);
        }

        using (SimpleSmtpServer emailServer = StartServer(port))
        {
            using SmtpClient client = new();
            client.Connect(host, port, false);
            var from = new MailboxAddress("from", "carlos@netdumbster.com");
            var to = new MailboxAddress("to", "karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "This is an email";
            var builder = new BodyBuilder
            {
                TextBody = "body of email"
            };
            message.Body = builder.ToMessageBody();

            client.Send(message);
        }
    }

    [Fact]
    public void Send_One_Mail_Clear_Send_Another_Mail()
    {
        SendMail();
        Assert.Equal(1, server.ReceivedEmailCount);
        Assert.Equal("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
        server.ClearReceivedEmail();
        SendMail();
        Assert.Equal("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
    }

    [Fact]
    public void Send_One_Mail_With_SmtpAuth()
    {
        SendMail(true);
        Assert.Equal(1, server.ReceivedEmailCount);
    }

    [Fact]
    public void Start_Server_Random_Port()
    {
        SimpleSmtpServer randomPortServer = StartServer();
        Assert.True(randomPortServer.Configuration.Port > 0);
        randomPortServer.Stop();
    }

    [Fact]
    public void Send_Attachments_Mails_SpecialChars()
    {
        var files = Directory.GetFiles("Content");
        SendMail(false, files);
        Assert.Equal(files.Length, server.ReceivedEmailCount);
        var smtpMail = server.ReceivedEmail[0];
        using var mailMessage = MailMessageMimeParser.ParseMessage(new StringReader(smtpMail.Data));
        foreach (var m in mailMessage.Attachments)
        {
            Console.WriteLine(m.Name);
            Assert.EndsWith(m.Name, files[0]);
        }
    }

    [Fact(Timeout = 10000)]
    public async Task Reusing_Smtp_Client_Should_Not_Fail()
    {
        var config = Configuration.Configure();
        using var simpleServer = SimpleSmtpServer.Start(config.WithRandomPort().Port);
        SmtpClient client = new();
        client.Connect("localhost", simpleServer.Configuration.Port, false);

        var from = new MailboxAddress("from", "carlos@netdumbster.com");
        var to = new MailboxAddress("to", "karina@netdumbster.com");

        var message = new MimeMessage();

        message.From.Add(from);
        message.To.Add(to);
        message.Subject = "This is an email";
        var builder = new BodyBuilder
        {
            TextBody = "body of email"
        };
        message.Body = builder.ToMessageBody();

        for (int messageNo = 0; messageNo < 2; messageNo++)
        {
            await client.SendAsync(message);

            Assert.Equal(messageNo + 1, simpleServer.ReceivedEmailCount);
        }
    }

    protected void SendMail()
    {
        SendMail(false);
    }

    protected void SendMail(bool smtpAuth)
    {
        SendMail(smtpAuth, false, null);
    }

    protected void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment)
    {
        SendMail(smtpAuth, isBodyHtml, attachment, server.Configuration.Port);
    }

    protected static void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment, int serverPort)
    {
        using SmtpClient client = new();
        client.Connect("localhost", serverPort, false);
        var from = new MailboxAddress("from", "carlos@netdumbster.com");
        var to = new MailboxAddress("to", "karina@netdumbster.com");

        var message = new MimeMessage();

        message.From.Add(from);
        message.To.Add(to);
        message.Subject = "test";

        var builder = new BodyBuilder();
        if (!isBodyHtml)
        {
            builder.TextBody = "this is the body";
        }
        else
        {
            builder.HtmlBody = "this is the html body";
        }

        if (attachment != null)
        {
            builder.Attachments.Add("image", new MemoryStream(attachment), ContentType.Parse("image/jpeg"));
        }

        message.Body = builder.ToMessageBody();

        if (smtpAuth)
        {
            client.Authenticate("userName", "Password");
        }

        client.Send(message);
    }

    protected void SendMail(bool smtpAuth, IEnumerable<string> attach)
    {
        using SmtpClient client = new();
        client.Connect("localhost", server.Configuration.Port, false);
        var from = new MailboxAddress("from", "carlos@netdumbster.com");
        var to = new MailboxAddress("to", "karina@netdumbster.com");

        var message = new MimeMessage();
        message.From.Add(from);
        message.To.Add(to);
        message.Subject = "test";

        var builder = new BodyBuilder
        {
            TextBody = "this is the body"
        };

        foreach (var fileName in attach)
            builder.Attachments.Add(fileName);

        if (smtpAuth)
        {
            NetworkCredential credentials = new("user", "pwd");
            client.Authenticate(credentials);
        }

        message.Body = builder.ToMessageBody();

        client.Send(message);
    }

    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                server.Stop();
            }

            disposedValue = true;
        }
    }

    static int GetRandomUnusedPort()
    {
        try
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
        catch
        {
            throw;
        }
    }

    // This code added to correctly implement the disposable pattern.
    void IDisposable.Dispose()
    {
        Dispose(true);
    }
}
