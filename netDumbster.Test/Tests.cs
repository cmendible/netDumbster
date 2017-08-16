// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.Test
{
    using netDumbster.smtp;

    public class Tests : TestsBase
    {
        protected override SimpleSmtpServer StartServer()
        {
            return SimpleSmtpServer.Start();
        }

        protected override SimpleSmtpServer StartServer(int port)
        {
            return SimpleSmtpServer.Start(port);
        }
    }
}