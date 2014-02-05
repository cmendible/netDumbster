// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.Test
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Mime;

    using netDumbster.smtp;
    using netDumbster.smtp.Logging;

    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        private SimpleSmtpServer server;
        private Random _Rnd = new Random();

        public Tests()
        {
            LogManager.GetLogger = type => new ConsoleLogger(type);
        }

        [Test]
        public void Send_100_Mails()
        {
            for (var i = 0; i < 100; i++)
            {
                this.SendMail();
                Assert.AreEqual(i +1, this.server.ReceivedEmailCount);
            }

            Assert.AreEqual(100, this.server.ReceivedEmailCount);
        }

        [Test]
        public void Send_100_Mail_With_SmtpAuth()
        {
            for (var i = 0; i < 100; i++)
            {
                this.SendMail(true);
                Assert.AreEqual(i + 1, this.server.ReceivedEmailCount);
            }

            Assert.AreEqual(100, this.server.ReceivedEmailCount);
        }

        [Test]
        public void Send_Email_And_Restart_Server_Using_The_Same_Port()
        {
            int port = 50003;
            SimpleSmtpServer fixedPortServer = SimpleSmtpServer.Start(port);

            this.SendMail(false, false, null, port);

            Assert.AreEqual(1, fixedPortServer.ReceivedEmailCount);
            Assert.AreEqual("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();

            fixedPortServer = SimpleSmtpServer.Start(port);

            this.SendMail(false, false, null, port);

            Assert.AreEqual(1, fixedPortServer.ReceivedEmailCount);
            Assert.AreEqual("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();
        }

        // Test is run several several times since we're testing asynchronous behaviour
        [Test]
        [Repeat(5)]
        public void Send_Email_When_Server_Not_Running()
        {
            this.server.Stop();
            Assert.Throws<SmtpException>(() => this.SendMail());
        }

        [Test]
        public void Send_Email_With_AlternateViews()
        {
            using (var client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("FooBar", new ContentType("text/html")));
                client.Send(mailMessage);
            }

            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];

            Assert.AreEqual(2, smtpMessage.MessageParts.Length);
            Assert.IsTrue(smtpMessage.MessageParts[0].HeaderData.Contains("text/plain"));
            Assert.AreEqual("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.IsTrue(smtpMessage.MessageParts[1].HeaderData.Contains("text/html"));
            Assert.AreEqual("FooBar", smtpMessage.MessageParts[1].BodyData);
        }

        [Test]
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

            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            var smtpMessage = this.server.ReceivedEmail[0];
            Assert.AreEqual(5, smtpMessage.MessageParts.Length);
            Assert.IsTrue(smtpMessage.MessageParts[0].HeaderData.Contains("text/plain"));
            Assert.AreEqual("this is the body", smtpMessage.MessageParts[0].BodyData);
            Assert.IsTrue(smtpMessage.MessageParts[1].HeaderData.Contains("text/html"));
            Assert.AreEqual("FooBar", smtpMessage.MessageParts[1].BodyData);
        }

        [Test]
        public void Send_Email_With_Attachment()
        {
            var data = new byte[] { 0x1 };

            this.SendMail(false, true, data);
            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.IsNotNull(this.server.ReceivedEmail[0].MessageParts[1]);
            Assert.IsNotNull(this.server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.IsNotEmpty(this.server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.AreEqual(System.Convert.ToBase64String(data) + "\r\n", this.server.ReceivedEmail[0].MessageParts[1].BodyData);
        }

        [Test]
        public void Send_Email_With_Many_Lines()
        {
            using (SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body\r\nline2\r\nline3");
                mailMessage.IsBodyHtml = false;
                client.Send(mailMessage);
            }

            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the body\r\nline2\r\nline3", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_Email_With_Priority()
        {
            using (SmtpClient client = new SmtpClient("localhost", this.server.Configuration.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.IsBodyHtml = false;
                mailMessage.Priority = MailPriority.High;
                client.Send(mailMessage);
            }

            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.AreEqual("1", this.server.ReceivedEmail[0].XPriority);
            Assert.AreEqual("urgent", this.server.ReceivedEmail[0].Priority);
            Assert.AreEqual("high", this.server.ReceivedEmail[0].Importance);
        }

        [Test]
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

            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("base64", this.server.ReceivedEmail[0].Headers["content-transfer-encoding"]);
            Assert.AreEqual(
                body,
                System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(this.server.ReceivedEmail[0].MessageParts[0].BodyData)));

            this.server.Stop();
        }

        [Test]
        public void Send_Fires_Message_Received_Event()
        {
            int port = 50004;
            SimpleSmtpServer fixedPortServer = SimpleSmtpServer.Start(port);
            fixedPortServer.MessageReceived += (sender, args) =>
            {
                Assert.IsNotNull(args.Message);
                Assert.AreEqual(1, fixedPortServer.ReceivedEmailCount);
                Assert.AreEqual("this is the body", args.Message.MessageParts[0].BodyData);
            };

            this.SendMail(false, false, null, port);

            fixedPortServer.Stop();
        }

        [Test]
        public void Send_Html_Email()
        {
            this.SendMail(false, true, null);
            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_One_Mail()
        {
            this.SendMail();
            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_One_Mail_Clear_Send_Another_Mail()
        {
            this.SendMail();
            Assert.AreEqual(1, this.server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
            this.server.ClearReceivedEmail();
            this.SendMail();
            Assert.AreEqual("this is the body", this.server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_One_Mail_With_SmtpAuth()
        {
            this.SendMail(true);
            Assert.AreEqual(1, this.server.ReceivedEmailCount);
        }

        [SetUp]
        public void SetUp()
        {
            this.server = SimpleSmtpServer.Start();
        }

        [Test]
        public void Start_Server_Random_Port()
        {
            SimpleSmtpServer randomPortServer = SimpleSmtpServer.Start();
            Assert.Greater(randomPortServer.Configuration.Port, 0);
            randomPortServer.Stop();
        }

        [TearDown]
        public void TearDown()
        {
            this.server.Stop();
        }

        private void SendMail()
        {
            this.SendMail(false);
        }

        private void SendMail(bool smtpAuth)
        {
            this.SendMail(smtpAuth, false, null);
        }

        private void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment)
        {
            this.SendMail(smtpAuth, isBodyHtml, attachment, this.server.Configuration.Port);
        }

        private void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment, int serverPort)
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
    }
}