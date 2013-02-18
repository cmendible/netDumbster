namespace netDumbster.Test
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Mail;

    using netDumbster.smtp;
    using netDumbster.smtp.Logging;

    using NUnit.Framework;

    [TestFixture]
    public class Tests
    {
        #region Fields

        private SimpleSmtpServer server;
        private Random _Rnd = new Random();

        #endregion Fields

        #region Constructors

        public Tests()
        {
            LogManager.GetLogger = type => new ConsoleLogger(type);
        }

        #endregion Constructors

        #region Methods

        [TearDown]
        public void FixtureTearDown()
        {
            server.Stop();
        }

        [Test]
        public void Send_100_Mails()
        {
            for (var i = 0; i < 100; i++)
            {
                SendMail();
                Assert.AreEqual(i +1, server.ReceivedEmailCount);
            }
            Assert.AreEqual(100, server.ReceivedEmailCount);
        }

        [Test]
        public void Send_100_Mail_With_SmtpAuth()
        {
            for (var i = 0; i < 100; i++)
            {
                SendMail(true);
                Assert.AreEqual(i + 1, server.ReceivedEmailCount);
            }
            Assert.AreEqual(100, server.ReceivedEmailCount);
        }

        [Test]
        public void Send_Email_And_Restart_Server_Using_The_Same_Port()
        {
            int port = 50003;
            SimpleSmtpServer fixedPortServer = SimpleSmtpServer.Start(port);

            SendMail(false, false, null, port);

            Assert.AreEqual(1, fixedPortServer.ReceivedEmailCount);
            Assert.AreEqual("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();

            fixedPortServer = SimpleSmtpServer.Start(port);

            SendMail(false, false, null, port);

            Assert.AreEqual(1, fixedPortServer.ReceivedEmailCount);
            Assert.AreEqual("this is the body", fixedPortServer.ReceivedEmail[0].MessageParts[0].BodyData);

            fixedPortServer.Stop();
        }

        // Test is run several several times since we're testing asynchronous behaviour
        [Test]
        [Repeat(5)]
        public void Send_Email_When_Server_Not_Running()
        {
            server.Stop();
            Assert.Throws<SmtpException>(() => SendMail());
        }

        [Test]
        public void Send_Email_With_Attachment()
        {
            var data = new byte[] { 0x1 };

            SendMail(false, true, data);
            Assert.AreEqual(1, server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body\r\n", server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.IsNotNull(server.ReceivedEmail[0].MessageParts[1]);
            Assert.IsNotNull(server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.IsNotEmpty(server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.AreEqual(System.Convert.ToBase64String(data) + "\r\n", server.ReceivedEmail[0].MessageParts[1].BodyData);
        }

        [Test]
        public void Send_Email_With_Priority()
        {
            using (SmtpClient client = new SmtpClient("localhost", server.Port))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.IsBodyHtml = false;
                mailMessage.Priority = MailPriority.High;
                client.Send(mailMessage);
            }

            Assert.AreEqual(1, server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.AreEqual("1", server.ReceivedEmail[0].XPriority);
            Assert.AreEqual("1", server.ReceivedEmail[0].Priority);
            Assert.AreEqual("high", server.ReceivedEmail[0].Importance);
        }

        [Test]
        public void Send_Email_With_RussianText()
        {
            string body = string.Empty;
            using (SmtpClient client = new SmtpClient("localhost", server.Port))
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

            Assert.AreEqual(1, server.ReceivedEmailCount);
            Assert.AreEqual("base64", server.ReceivedEmail[0].Headers["content-transfer-encoding"]);
            Assert.AreEqual(body,
                System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(server.ReceivedEmail[0].MessageParts[0].BodyData)));

            server.Stop();
        }

        [Test]
        public void Send_Html_Email()
        {
            SendMail(false, true, null);
            Assert.AreEqual(1, server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body", server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_One_Mail()
        {
            SendMail();
            Assert.AreEqual(1, server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_One_Mail_With_SmtpAuth()
        {
            SendMail(true);
            Assert.AreEqual(1, server.ReceivedEmailCount);
        }

        [SetUp]
        public void SetUp()
        {
            server = SimpleSmtpServer.Start(_Rnd.Next(50000, 60000));
        }

        private void SendMail()
        {
            SendMail(false);
        }

        private void SendMail(bool smtpAuth)
        {
            SendMail(smtpAuth, false, null);
        }

        private void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment)
        {
            SendMail(smtpAuth, isBodyHtml, attachment, server.Port);
        }

        private void SendMail(bool smtpAuth, bool isBodyHtml, byte[] attachment, int serverPort)
        {
            using (SmtpClient client = new SmtpClient("localhost", serverPort))
            {
                var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
                mailMessage.IsBodyHtml = isBodyHtml;

                if (isBodyHtml)
                    mailMessage.Body = "this is the html body";

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

        #endregion Methods
    }
}