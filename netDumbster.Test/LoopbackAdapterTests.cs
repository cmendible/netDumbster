// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.
namespace netDumbster.Test;

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

    protected override SimpleSmtpServer StartServer(bool useMessageStore)
    {
        return Configuration.Configure()
            .WithAddress(IPAddress.Loopback)
            .EnableMessageStore(useMessageStore)
            .Build();
    }

    protected override SimpleSmtpServer StartServer(int port, bool useMessageStore)
    {
        return Configuration.Configure()
            .WithAddress(IPAddress.Loopback)
            .WithPort(port)
            .EnableMessageStore(useMessageStore)
            .Build();
    }
}
