namespace netDumbster.Test;

public class MailKitTests : IDisposable
{
    private bool _disposed;
    private readonly SimpleSmtpServer _server;

    public MailKitTests()
    {
        LogManager.GetLogger = type => new ConsoleLogger(type);

        _server = Configuration.Configure()
            .WithRandomPort()
            .Build();
    }

    /// <summary>
    /// As reported: <see href="https://github.com/cmendible/netDumbster/issues/26"/>
    /// </summary>
    [Fact]
    public async Task Send_WithMailKit_WholeBodyMessageIsPreserved()
    {
        var expectedBody = $"this is the body{Environment.NewLine}line2{Environment.NewLine}line3";

        using var client = new SmtpClient
        {
            ServerCertificateValidationCallback = (_, __, ___, ____) => true
        };

        await client.ConnectAsync("localhost", _server.Configuration.Port, false).ConfigureAwait(true);
        client.AuthenticationMechanisms.Remove("XOAUTH2");

        var from = new MailboxAddress("from", "carlos@netdumbster.com");
        var to = new MailboxAddress("to", "karina@netdumbster.com");

        var message = new MimeMessage();

        message.From.Add(from);
        message.To.Add(to);
        message.Subject = "test";
        message.Body = new TextPart("plain") { Text = expectedBody };
        message.Headers.Add("empty-value", string.Empty);

        client.Send(message);

        Assert.Equal(1, _server.ReceivedEmailCount);
        Assert.Equal(expectedBody, _server.ReceivedEmail[0].MessageParts[0].BodyData);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _server.Stop();
        }

        _disposed = true;
    }
}

