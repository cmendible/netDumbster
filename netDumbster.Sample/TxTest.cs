using System;
using System.Net;
using System.Net.Mail;
using NUnit.Framework;
using netDumbster.smtp;

namespace netDumbster.Sample
{

    [TestFixture]
    public class TxNetDumbsterSamplesTest
    {
        private static SimpleSmtpServer _Server;
        private Random _Rnd = new Random();

//        public TxNetDumbsterSamplesTest()
//        {
//            log4net.Config.XmlConfigurator.Configure();
//        }

        private void SendMail()
        {
            SendMail(false);
        }

        private void SendMail(bool smtpAuth)
        {
            SmtpClient client = new SmtpClient("localhost", _Server.Port);
            var mailMessage = new MailMessage("cfm@mendible.com", "kbm@mendible.com", "test", "test tes test");

            if (smtpAuth)
            {
                NetworkCredential credentials = new NetworkCredential("user", "pwd");
                client.Credentials = credentials;
                client.EnableSsl = false;
            }

            client.Send(mailMessage);
        }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            _Server = SimpleSmtpServer.Start(_Rnd.Next(50000, 60000));
        }

        [TestFixtureTearDown]
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
                Assert.AreEqual(i + 1, _Server.ReceivedEmailCount);
            }
            Assert.AreEqual(100, _Server.ReceivedEmailCount);
        }
    }
}
