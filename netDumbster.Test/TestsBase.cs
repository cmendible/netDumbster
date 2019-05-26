using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using netDumbster.smtp;
using netDumbster.smtp.Logging;
using System.Diagnostics;
using Xunit;

namespace netDumbster.Test
{
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

        // Test is run several several times since we're testing asynchronous behaviour
        [Theory]
        [Repeat(5)]
        public void Send_Email_When_Server_Not_Running()
        {
            this.server.Stop();
            Assert.Throws<SmtpException>(() => this.SendMail());
        }

        [Fact]
        public void Send_Email_With_AlternateViews()
        {
            using (var client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("FooBar", new ContentType("text/html")));
                client.Send(mailMessage);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];

            Assert.Equal(2, smtpMessage.MessageParts.Length);
            Assert.True(smtpMessage.MessageParts[0].HeaderData.Contains("text/plain"));
            Assert.Equal("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.True(smtpMessage.MessageParts[1].HeaderData.Contains("text/html"));
            Assert.Equal("FooBar", smtpMessage.MessageParts[1].BodyData);
        }

        [Fact]
        public void Send_Email_With_AlternateViews_And_Attachments()
        {
            using (var client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("FooBar", new ContentType("text/html")));
                mailMessage.Attachments.Add(Attachment.CreateAttachmentFromString("Attachment1", new ContentType("application/octet-stream")));
                mailMessage.Attachments.Add(Attachment.CreateAttachmentFromString("Attachment2", new ContentType("application/octet-stream")));
                mailMessage.Attachments.Add(Attachment.CreateAttachmentFromString("Attachment3", new ContentType("application/octet-stream")));
                client.Send(mailMessage);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];
            Assert.Equal(5, smtpMessage.MessageParts.Length);
            Assert.True(smtpMessage.MessageParts[0].HeaderData.Contains("text/plain"));
            Assert.Equal("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.True(smtpMessage.MessageParts[1].HeaderData.Contains("text/html"));
            Assert.Equal("FooBar", smtpMessage.MessageParts[1].BodyData);
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
        public void Send_Email_With_Many_Lines()
        {
            using (SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body\r\nline2\r\nline3");
                mailMessage.IsBodyHtml = false;
                client.Send(mailMessage);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the body\r\nline2\r\nline3", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Fact]
        public void Send_Email_With_Priority()
        {
            using (SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.IsBodyHtml = false;
                mailMessage.Priority = MailPriority.High;
                client.Send(mailMessage);
            }

            Assert.Equal(1, this.server.ReceivedEmailCount);
            Assert.Equal("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.Equal("1", this.server.ReceivedEmail[0].XPriority);
            Assert.Equal("urgent", this.server.ReceivedEmail[0].Priority);
            Assert.Equal("high", this.server.ReceivedEmail[0].Importance);
        }

        [Fact]
        public void Send_Email_With_RussianText()
        {
            string body = string.Empty;
            using (SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                body = "Съешь ещё этих мягких французских булок, да выпей чаю" +
                       "Съешь ещё этих мягких французских булок, да выпей чаю" +
                       "Съешь ещё этих мягких французских булок, да выпей чаю" +
                       "Съешь ещё этих мягких французских булок, да выпей чаю" +
                       "Съешь ещё этих мягких французских булок, да выпей чаю" +
                       "Съешь ещё этих мягких французских булок, да выпей чаю";
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", body);
                mailMessage.IsBodyHtml = false;
                client.Send(mailMessage);
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
            var port = 25;
            var host = "localhost";
            using (SimpleSmtpServer emailServer = this.StartServer(port))
            {
                SmtpClient client = new SmtpClient(host, port);
                client.Send("noone@nowhere.com", "nobody@nowhere.com", "This is an email", "body of email");
            }

            using (SimpleSmtpServer emailServer = this.StartServer(port))
            {
                using (SmtpClient client = new SmtpClient(host, port))
                    client.Send("noone@nowhere.com", "nobody@nowhere.com", "This is an email", "body of email");
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
            using (MailMessage mailMessage = MailMessageMimeParser.ParseMessage(new System.IO.StringReader(smtpMail.Data)))
            {
                foreach (var m in mailMessage.Attachments)
                {
                    Console.WriteLine(m.Name);
                    Assert.True(files[0].EndsWith(m.Name));
                }
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
            using (SmtpClient client = new SmtpClient("localhost", serverPort))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.Bcc.Add(new MailAddress("bcc@mendible.com"));
                mailMessage.CC.Add(new MailAddress("cc@mendible.com"));
                mailMessage.IsBodyHtml = isBodyHtml;

                if (isBodyHtml)
                {
                    mailMessage.Body = "this is the html body";
                }

                if (smtpAuth)
                {
                    NetworkCredential credentials = new NetworkCredential("user", "pwd");
                    client.Credentials = credentials;
                    client.EnableSsl = false;
                }

                if (attachment != null)
                {
                    mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), "image/jpeg"));
                }

                client.Send(mailMessage);
            }
        }

        protected void SendMail(bool smtpAuth, IEnumerable<string> attach)
        {
            SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port);
            var mailMessage = new MailMessage("cfm@mendible.com", "kbm@mendible.com", "test", "test test test");
            foreach (var fileName in attach)
                mailMessage.Attachments.Add(new Attachment(fileName));

            if (smtpAuth)
            {
                NetworkCredential credentials = new NetworkCredential("user", "pwd");
                client.Credentials = credentials;
                client.EnableSsl = false;
            }
            client.Send(mailMessage);

            client.Dispose();
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

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
    }
}
