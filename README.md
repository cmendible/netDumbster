![dotnetcore](https://github.com/cmendible/netDumbster/workflows/dotnetcore/badge.svg)

# netDumbster 
**netDumbster** is a .Net Fake SMTP Server clone of the popular **Dumbster**.

**netDumbster** is based on the API of **nDumbster** (http://ndumbster.sourceforge.net/default.html) and the nice C# Email Server (CSES) written by Eric Daugherty.

License: http://www.apache.org/licenses/LICENSE-2.0.html

# Usage

Create a netDumbster Server instance:
````csharp
using var server = SimpleSmtpServer.Start(port);
````

Check received email count:
````csharp
var count = server.ReceivedEmailCount
````

Get the body of the first email received:
````csharp
var smtpMessage = server.ReceivedEmail[0];
var body = smtpMessage.MessageParts[0].BodyData
```` 

Subscribe to the message received event:
````csharp
server.MessageReceived += (sender, args) =>
    {
        // Get message body.
        var body = args.Message.MessageParts[0].BodyData;
    };
````
