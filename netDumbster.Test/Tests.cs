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
		private static SimpleSmtpServer _Server;
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

		[FixtureSetUp]
		public void FixtureSetUp()
		{
			_Server = SimpleSmtpServer.Start(_Rnd.Next(50000, 60000));
		}

		[FixtureTearDown]
		public void FixtureTearDown()
		{
			_Server.Stop();
		}

		[SetUp]
		public void SetUp()
		{
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
        public void Send_Email_With_Attachment()
        {
            //SendMail(false, true, new byte[] { 0x1 });
            //Assert.AreEqual(1, _Server.ReceivedEmailCount);
            //Assert.AreEqual("this is the html body", _Server.ReceivedEmail[0].MessageParts[0].BodyData);
        }
	}
}
