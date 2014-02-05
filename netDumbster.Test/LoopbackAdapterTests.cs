// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.
namespace netDumbster.Test
{
    using System.Net;
    using netDumbster.smtp;
    using NUnit.Framework;

    [TestFixture]
    public class LoopbackAdapterTests : TestsBase
    {
        protected override SimpleSmtpServer StartServer()
        {
            return Configuration.Configure()
                .WithAddress(IPAddress.Loopback)
                .Build();
        }

        protected override SimpleSmtpServer StartServer(int port)
        {
            return Configuration.Configure()
                .WithAddress(IPAddress.Loopback)
                .WithPort(port)
                .Build();
        }

    }
}