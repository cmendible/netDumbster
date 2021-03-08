namespace netDumbster.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using netDumbster.smtp;
    using netDumbster.smtp.Logging;
    using Xunit;
    using System.Threading.Tasks;
    using System.Net.Sockets;
    using MailKit.Net.Smtp;
    using MimeKit;

    public class TestsBase : IDisposable
    {
        protected SimpleSmtpServer server;

        private Random _Rnd = new Random();

        public TestsBase()
        {
            LogManager.GetLogger = type => new ConsoleLogger(type);
            this.server = this.StartServer();
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
        public void Send_10_Mails()
        {
            for (var i = 0; i < 10; i++)
            {
                this.SendMail();
                Assert.Equal(i + 1, this.server.ReceivedEmailCount);
            }

            Assert.Equal(10, this.server.ReceivedEmailCount);
        }

        [Fact]
        public void Send_10_Mail_With_SmtpAuth()
        {
            for (var i = 0; i < 10; i++)
            {
                this.SendMail(true);
                Assert.Equal(i + 1, this.server.ReceivedEmailCount);
            }

            Assert.Equal(10, this.server.ReceivedEmailCount);
        }

        [Fact]
        public void Send_Email_And_Restart_Server_Using_The_Same_Port()
        {
            int port = 5003;
            SimpleSmtpServer fixedPortServer = this.StartServer(port);

            this.SendMail(false, false, null, port);

            Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
            Assert.Equal("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();

            fixedPortServer = this.StartServer(port);

            this.SendMail(false, false, null, port);

            Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
            Assert.Equal("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();
        }

        [Fact]
        public void Send_Email_When_Server_Not_Running()
        {
            this.server.Stop();
            var ex = Record.Exception(() => this.SendMail());
            Assert.IsAssignableFrom<SocketException>(ex);
        }

        [Fact]
        public void Send_Email_With_AlternateViews()
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(to);
                message.Subject = "test";

                var builder = new BodyBuilder();
                builder.TextBody = "this is the body";
                builder.HtmlBody = "FooBar";

                message.Body = builder.ToMessageBody();

                client.Send(message);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];

            Assert.Equal(2, smtpMessage.MessageParts.Length);
            Assert.Contains("text/plain", smtpMessage.MessageParts[0].HeaderData);
            Assert.Contains("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.Contains("text/html", smtpMessage.MessageParts[1].HeaderData);
            Assert.Contains("FooBar", smtpMessage.MessageParts[1].BodyData);
        }

        [Fact]
        public void Send_Email_With_AlternateViews_And_Attachments()
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(to);
                message.Subject = "test";

                var builder = new BodyBuilder();
                builder.TextBody = "this is the body";
                builder.HtmlBody = "FooBar";

                builder.Attachments.Add("Attachment1", System.Text.Encoding.UTF8.GetBytes("Attachment1"), ContentType.Parse("application/octet-stream"));
                builder.Attachments.Add("Attachment2", System.Text.Encoding.UTF8.GetBytes("Attachment2"), ContentType.Parse("application/octet-stream"));
                builder.Attachments.Add("Attachment3", System.Text.Encoding.UTF8.GetBytes("Attachment3"), ContentType.Parse("application/octet-stream"));

                message.Body = builder.ToMessageBody();

                client.Send(message);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];
            Assert.Equal(5, smtpMessage.MessageParts.Length);
            Assert.Contains("text/plain", smtpMessage.MessageParts[0].HeaderData);
            Assert.Contains("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.Contains("text/html", smtpMessage.MessageParts[1].HeaderData);
            Assert.Contains("FooBar", smtpMessage.MessageParts[1].BodyData);
        }

        [Fact]
        public void Send_Email_With_Attachment()
        {
            var data = new byte[] { 0x1 };

            this.SendMail(false, true, data);
            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the html body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.NotNull(this.server.ReceivedEmail[0].MessageParts[1]);
            Assert.NotNull(this.server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.NotEmpty(this.server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.Equal(data, UTF8Encoding.UTF8.GetBytes(this.server.ReceivedEmail[0].MessageParts[1].BodyData));
        }

        [Fact]
        public void Send_Multiline_Email()
        {
            var expectedBody = $"this is the body{Environment.NewLine}line2{Environment.NewLine}line3";

            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(to);
                message.Subject = "test";

                var builder = new BodyBuilder();
                builder.TextBody = expectedBody;

                message.Body = builder.ToMessageBody();
                client.Send(message);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal(expectedBody, this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Fact]
        public void Send_Email_With_Priority()
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

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

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.Equal("1 (Highest)", this.server.ReceivedEmail[0].XPriority);
            Assert.Equal("urgent", this.server.ReceivedEmail[0].Priority);
            Assert.Equal("high", this.server.ReceivedEmail[0].Importance);
        }

        [Fact]
        public void Send_Email_With_RussianText()
        {
            string body = string.Empty;
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

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

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal(body, this.server.ReceivedEmail[0].MessageParts[0].BodyData);

            this.server.Stop();
        }

        [Fact]
        public void Send_Email_With_UTF8_Chars()
        {
            string body = string.Empty;
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

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

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal(body, this.server.ReceivedEmail[0].MessageParts[0].BodyData);

            this.server.Stop();
        }

        [Fact]
        public void Send_Fires_Message_Received_Event()
        {
            int port = 50004;
            SimpleSmtpServer fixedPortServer = this.StartServer(port);
            fixedPortServer.MessageReceived += (sender, args) =>
            {
                Assert.NotNull(args.Message);
                Assert.Equal(1, fixedPortServer.ReceivedEmailCount);
                Assert.Equal("this is the body", args.Message.MessageParts[0].BodyData);
            };

            this.SendMail(false, false, null, port);

            fixedPortServer.Stop();
        }

        [Fact]
        public void Send_Html_Email()
        {
            this.SendMail(false, true, null);
            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the html body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Fact]
        public void Send_One_Mail()
        {
            this.SendMail();
            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Fact]
        public void If_Client_Is_Not_Disposed_Server_Everything_Keeps_Working()
        {
            var port = GetRandomUnusedPort();
            var host = "localhost";
            using (SimpleSmtpServer emailServer = this.StartServer(port))
            {
                var client = new SmtpClient();
                client.Connect(host, port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

                var message = new MimeMessage();

                message.From.Add(from);
                message.To.Add(to);
                message.Subject = "This is an email";
                var builder = new BodyBuilder();
                builder.TextBody = "body of email";
                message.Body = builder.ToMessageBody();

                client.Send(message);
            }

            using (SimpleSmtpServer emailServer = this.StartServer(port))
            {
                using (SmtpClient client = new SmtpClient())
                {
                    client.Connect(host, port, false);
                    var from = new MailboxAddress("carlos@netdumbster.com");
                    var to = new MailboxAddress("karina@netdumbster.com");

                    var message = new MimeMessage();

                    message.From.Add(from);
                    message.To.Add(to);
                    message.Subject = "This is an email";
                    var builder = new BodyBuilder();
                    builder.TextBody = "body of email";
                    message.Body = builder.ToMessageBody();

                    client.Send(message);
                }
            }
        }

        [Fact]
        public void Send_One_Mail_Clear_Send_Another_Mail()
        {
            this.SendMail();
            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            this.server.ClearReceivedEmail();
            this.SendMail();
            Assert.Equal("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Fact]
        public void Send_One_Mail_With_SmtpAuth()
        {
            this.SendMail(true);
            Assert.Equal(1, this.server.ReceivedEmailCount);
        }

        [Fact]
        public void Start_Server_Random_Port()
        {
            SimpleSmtpServer randomPortServer = this.StartServer();
            Assert.True(randomPortServer.Configuration.Port > 0);
            randomPortServer.Stop();
        }

        [Fact]
        public void Send_Attachments_Mails_SpecialChars()
        {
            var files = Directory.GetFiles("Content");
            SendMail(false, files);
            Assert.Equal(files.Length, this.server.ReceivedEmailCount);
            var smtpMail = this.server.ReceivedEmail[0];
            using (var mailMessage = MailMessageMimeParser.ParseMessage(new System.IO.StringReader(smtpMail.Data)))
            {
                foreach (var m in mailMessage.Attachments)
                {
                    Console.WriteLine(m.Name);
                    Assert.Contains(m.Name, files[0]);
                }
            }
        }

        [Fact(Timeout = 10000)]
        public async Task Reusing_Smtp_Client_Should_Not_Fail()
        {
            var config = Configuration.Configure();
            using var server = SimpleSmtpServer.Start(config.WithRandomPort().Port);
            SmtpClient client = new SmtpClient();
            client.Connect("localhost", server.Configuration.Port, false);

            var from = new MailboxAddress("carlos@netdumbster.com");
            var to = new MailboxAddress("karina@netdumbster.com");

            var message = new MimeMessage();

            message.From.Add(from);
            message.To.Add(to);
            message.Subject = "This is an email";
            var builder = new BodyBuilder();
            builder.TextBody = "body of email";
            message.Body = builder.ToMessageBody();

            for (int messageNo = 0; messageNo < 2; messageNo++)
            {
                await client.SendAsync(message);

                Assert.Equal(messageNo + 1, server.ReceivedEmailCount);
            }
        }

        protected void SendMail()
        {
            this.SendMail(false);
        }

        protected void SendMail(bool smtpAuth)
        {
            this.SendMail(smtpAuth, false, null);
        }

        protected void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment)
        {
            this.SendMail(smtpAuth, isBodyHtml, attachment, this.server.Configuration.Port);
        }

        protected void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment, int serverPort)
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", serverPort, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

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
        }

        protected void SendMail(bool smtpAuth, IEnumerable<string> attach)
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Connect("localhost", this.server.Configuration.Port, false);
                var from = new MailboxAddress("carlos@netdumbster.com");
                var to = new MailboxAddress("karina@netdumbster.com");

                var message = new MimeMessage();
                message.From.Add(from);
                message.To.Add(to);
                message.Subject = "test";

                var builder = new BodyBuilder();
                builder.TextBody = "this is the body";

                foreach (var fileName in attach)
                    builder.Attachments.Add(fileName);

                if (smtpAuth)
                {
                    NetworkCredential credentials = new NetworkCredential("user", "pwd");
                    client.Authenticate(credentials);
                }

                message.Body = builder.ToMessageBody();

                client.Send(message);
            }
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.server.Stop();
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
}
