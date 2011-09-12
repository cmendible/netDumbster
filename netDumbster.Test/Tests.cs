using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;

using Gallio.Framework;
using MbUnit.Framework;

using netDumbster.smtp;

namespace netDumbster.Test
{

	[TestFixture]
	public class Tests
	{
		private SimpleSmtpServer _Server;
		private Random _Rnd = new Random();

		public Tests()
        {
            log4net.Config.XmlConfigurator.Configure();
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
            SmtpClient client = new SmtpClient("localhost", _Server.Port);
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

		[FixtureTearDown]
		public void FixtureTearDown()
		{
			_Server.Stop();
		}

		[SetUp]
		public void SetUp()
		{
            _Server = SimpleSmtpServer.Start(_Rnd.Next(50000, 60000));
			_Server.ClearReceivedEmail();
		}

		[Test]
		public void Send_One_Mail()
		{
			SendMail();
			Assert.AreEqual(1, _Server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", _Server.ReceivedEmail[0].MessageParts[0].BodyData);
		}

		[Test]
		public void Send_One_Mail_With_SmtpAuth()
		{
			SendMail(true);
			Assert.AreEqual(1, _Server.ReceivedEmailCount);
		}

		[Test]
		public void Send_100_Mail_With_SmtpAuth()
		{
			for (var i = 0; i < 100; i++)
			{
				SendMail(true);
				Assert.AreEqual(i + 1, _Server.ReceivedEmailCount);
			}
			Assert.AreEqual(100, _Server.ReceivedEmailCount);
		}

		[Test]
		public void Send_100_Mails()
		{
			for (var i = 0; i < 100; i++)
			{
				SendMail();
				Assert.AreEqual(i +1, _Server.ReceivedEmailCount);
			}
			Assert.AreEqual(100, _Server.ReceivedEmailCount);
		}

        [Test]
        public void Send_Html_Email()
        {
            SendMail(false, true, null);
            Assert.AreEqual(1, _Server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body", _Server.ReceivedEmail[0].MessageParts[0].BodyData);
        }

        [Test]
        public void Send_Email_With_Priority()
        {
            SmtpClient client = new SmtpClient("localhost", _Server.Port);
            var mailMessage = new MailMessage("carlos@mendible.com", "karina@mendible.com", "test", "this is the body");
            mailMessage.IsBodyHtml = false;
            mailMessage.Priority = MailPriority.High;

            client.Send(mailMessage);
            Assert.AreEqual(1, _Server.ReceivedEmailCount);
            Assert.AreEqual("this is the body", _Server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.AreEqual("1", _Server.ReceivedEmail[0].XPriority);
            Assert.AreEqual("1", _Server.ReceivedEmail[0].Priority);
            Assert.AreEqual("high", _Server.ReceivedEmail[0].Importance);
        }

        [Test]
        public void Send_Email_With_Attachment()
        {
            var data = new byte[] { 0x1 };

            SendMail(false, true, data);
            Assert.AreEqual(1, _Server.ReceivedEmailCount);
            Assert.AreEqual("this is the html body\r\n", _Server.ReceivedEmail[0].MessageParts[0].BodyData);
            Assert.IsNotNull(_Server.ReceivedEmail[0].MessageParts[1]);
            Assert.IsNotNull(_Server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.IsNotEmpty(_Server.ReceivedEmail[0].MessageParts[1].BodyData);
            Assert.AreEqual(System.Convert.ToBase64String(data) + "\r\n", _Server.ReceivedEmail[0].MessageParts[1].BodyData);
        }

        [Test]
        [Repeat(5)] // Test is run several several times since we're testing asynchronous behaviour
        public void Send_Email_When_Server_Not_Running()
        {
            _Server.Stop();
            Assert.Throws<SmtpException>(() => SendMail());
        }

	}
}
