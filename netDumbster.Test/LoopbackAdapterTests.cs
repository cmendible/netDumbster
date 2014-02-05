// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.Test
{
    using System.Net.Mail;
    using System.Net.Mime;

    using netDumbster.smtp;
    using NUnit.Framework;

    [TestFixture]
    public class LoopbackAdapterTests : TestsBase
    {
        protected override SimpleSmtpServer StartServer()
        {
            return SimpleSmtpServer.StartOnLoopbackOnly();
        }

        protected override SimpleSmtpServer StartServer(int port)
        {
            return SimpleSmtpServer.StartOnLoopbackOnly(port);
        }

    }
}