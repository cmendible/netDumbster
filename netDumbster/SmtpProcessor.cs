// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

namespace netDumbster.smtp;

/// <summary>
/// SMTPProcessor handles a single SMTP client connection.  This
/// class provides an implementation of the RFC821 specification.
/// </summary>
/// <remarks>
///     Created by: Eric Daugherty
///     Modified by: Carlos Mendible
/// </remarks>
public class SmtpProcessor
{
    /// <summary>DATA Comand</summary>
    public const int COMMAND_DATA = 6;

    // Command codes
    /// <summary>HELO Command</summary>
    public const int COMMAND_HELO = 0;

    /// <summary>MAIL FROM Command</summary>
    public const int COMMAND_MAIL = 4;

    /// <summary>NOOP Command</summary>
    public const int COMMAND_NOOP = 2;

    /// <summary>QUIT Command</summary>
    public const int COMMAND_QUIT = 3;

    /// <summary>RCPT TO Command</summary>
    public const int COMMAND_RCPT = 5;

    /// <summary>RSET Command</summary>
    public const int COMMAND_RSET = 1;

    private const string MESSAGE_DEFAULT_HELO_RESPONSE = "250 {0}";

    // Messages
    private const string MESSAGE_DEFAULT_WELCOME = "220 {0} Welcome to C# SMTP Server.";
    private const string MESSAGE_GOODBYE = "221 Goodbye.";
    private const string MESSAGE_INVALID_ADDRESS = "451 Address is invalid.";
    private const string MESSAGE_INVALID_ARGUMENT_COUNT = "501 Incorrect number of arguments.";
    private const string MESSAGE_INVALID_COMMAND_ORDER = "503 Command not allowed here.";
    private const string MESSAGE_OK = "250 OK";
    private const string MESSAGE_START_DATA = "354 Start mail input; end with <CRLF>.<CRLF>";
    private const string MESSAGE_UNKNOWN_COMMAND = "500 Command Unrecognized.";

    // Regular Expressions
    private static readonly Regex ADDRESS_REGEX = new("<.+@.+>", RegexOptions.IgnoreCase);

    /// <summary>
    /// Context holding refenrece to Socket
    /// </summary>
    SmtpContext context;

    /// <summary>Domain name for this server.</summary>
    private string domain = string.Empty;

    /// <summary>The response to the HELO command.</summary>
    private string heloResponse = string.Empty;

    private readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType!);

    /// <summary>
    /// List of received messages (emails).
    /// </summary>
    readonly ConcurrentBag<SmtpMessage> smtpMessageStore;

    /// <summary>The message to display to the client when they first connect.</summary>
    private string welcomeMessage = string.Empty;

    /// <summary>
    /// Initializes the SMTPProcessor with the appropriate
    /// interface implementations.  This allows the relay and
    /// delivery behaviour of the SMTPProcessor to be defined
    /// by the specific server.
    /// </summary>
    /// <param name="domain">
    /// The domain name this server handles mail for.  This does not have to
    /// be a valid domain name, but it will be included in the Welcome Message
    /// and HELO response.
    /// </param>
    public SmtpProcessor(string domain, ConcurrentBag<SmtpMessage> smtpMessageStore)
    {
        this.smtpMessageStore = smtpMessageStore;
        Initialize(domain);
    }

    public event EventHandler<MessageReceivedArgs> MessageReceived;

    /// <summary>
    /// The response to the HELO command.  This response should
    /// include the local server's domain name. Please refer to RFC 821
    /// for more details.
    /// </summary>
    public virtual string HeloResponse
    {
        get
        {
            return heloResponse;
        }

        set
        {
            heloResponse = string.Format(value, domain);
        }
    }

    /// <summary>
    /// Returns the welcome message to display to new client connections.
    /// This method can be overridden to allow for user defined welcome messages.
    /// Please refer to RFC 821 for the rules on acceptable welcome messages.
    /// </summary>
    public virtual string WelcomeMessage
    {
        get
        {
            return welcomeMessage;
        }

        set
        {
            welcomeMessage = string.Format(value, domain);
        }
    }

    /// <summary>
    /// ProcessConnection handles a connected TCP Client
    /// and performs all necessary interaction with this
    /// client to comply with RFC821.  This method is thread
    /// safe.
    /// </summary>
    public void ProcessConnection(Socket socket)
    {
        context = new SmtpContext(socket);
        log.Debug("Sending welcome message.");
        SendWelcomeMessage(context);
        log.Debug("Welcome message sent.");
        log.Debug("Processing Commands.");
        ProcessCommands(context);
        log.Debug("Done processing Commands.");
    }

    /// <summary>
    /// Stops the processor.
    /// </summary>
    public void Stop()
    {
        if (context == null)
        {
            return;
        }

        log.Debug("trying to stop processor.");
        log.Debug("Shutting down Socket.");
        context.Socket.Shutdown(SocketShutdown.Both);
        log.Debug("Socket Shutdown.");
        log.Debug("Closing Socket.");
        context.Socket.Close();
        log.Debug("Socket Closed.");
    }

    private void Data(SmtpContext context)
    {
        context.WriteLine(MESSAGE_START_DATA);

        var rawSmtpMessage = context.Message;

        var clientEndPoint = (IPEndPoint)context.Socket.RemoteEndPoint;
        var header = new StringBuilder();
        header.Append(string.Format("Received: from ({0} [{1}])", context.ClientDomain, clientEndPoint.Address));
        header.Append("\r\n");
        header.Append("     " + DateTime.Now);
        header.Append("\r\n");

        rawSmtpMessage.Data.Append(header.ToString());

        header.Length = 0;

        var line = context.ReadLine();
        while (line is not null && !line.Equals("."))
        {
            rawSmtpMessage.Data.Append(line);
            rawSmtpMessage.Data.Append("\r\n");
            line = context.ReadLine();
        }

        // Spool the message
        if (smtpMessageStore is not null)
        {
            lock (smtpMessageStore)
            {
                var smtpMessage = new SmtpMessage(rawSmtpMessage);

                smtpMessageStore.Add(smtpMessage);

                if (MessageReceived is not null)
                {
                    MessageReceived(this, new MessageReceivedArgs(smtpMessage));
                }
            }
        }

        context.WriteLine(MESSAGE_OK);

        // Reset the connection.
        context.Reset();
    }

    /// <summary>
    /// Handles the HELO command.
    /// </summary>
    private void Helo(SmtpContext context, string[] inputs)
    {
        if (context.LastCommand == -1)
        {
            if (inputs.Length == 2)
            {
                context.ClientDomain = inputs[1];
                context.LastCommand = COMMAND_HELO;
                context.WriteLine(HeloResponse);
            }
            else
            {
                context.WriteLine(MESSAGE_INVALID_ARGUMENT_COUNT);
            }
        }
        else
        {
            context.WriteLine(MESSAGE_INVALID_COMMAND_ORDER);
        }
    }

    /// <summary>
    /// Provides common initialization logic for the constructors.
    /// </summary>
    private void Initialize(string domain)
    {
        this.domain = domain;

        // Initialize default messages
        welcomeMessage = string.Format(MESSAGE_DEFAULT_WELCOME, domain);
        heloResponse = string.Format(MESSAGE_DEFAULT_HELO_RESPONSE, domain);
    }

    /// <summary>
    /// Handle the MAIL FROM:&lt;address&gt; command.
    /// </summary>
    private void Mail(SmtpContext context, string argument)
    {
        var addressValid = false;
        if (context.LastCommand == COMMAND_HELO)
        {
            var address = ParseAddress(argument);
            if (!string.IsNullOrEmpty(address))
            {
                try
                {
                    context.LastCommand = COMMAND_MAIL;
                    addressValid = true;
                    context.WriteLine(MESSAGE_OK);
                }
                catch
                {
                    // This is fine, just fall through.
                }
            }

            // If the address is invalid, inform the client.
            if (!addressValid)
            {
                context.WriteLine(MESSAGE_INVALID_ADDRESS);
            }
        }
        else
        {
            context.WriteLine(MESSAGE_INVALID_COMMAND_ORDER);
        }
    }

    /// <summary>
    /// Parses a valid email address out of the input string and return it.
    /// Null is returned if no address is found.
    /// </summary>
    private string ParseAddress(string input)
    {
        var match = ADDRESS_REGEX.Match(input);
        if (match.Success)
        {
            var matchText = match.Value;

            // Trim off the :< chars
            matchText = matchText.Remove(0, 1);

            // trim off the . char.
            matchText = matchText.Remove(matchText.Length - 1, 1);

            return matchText;
        }

        return string.Empty;
    }

    /// <summary>
    /// Handles the command input from the client.  This
    /// message returns when the client issues the quit command.
    /// </summary>
    private void ProcessCommands(SmtpContext context)
    {
        var isRunning = true;

        // Loop until the client quits.
        while (isRunning)
        {
            try
            {
                var inputLine = context.ReadLine();
                if (inputLine == null)
                {
                    isRunning = false;
                    context.WriteLine(MESSAGE_GOODBYE);
                    context.Close();
                    continue;
                }

                string[] inputs = inputLine.Split(" ".ToCharArray());

                switch (inputs[0].ToLower())
                {
                    case "helo":
                        Helo(context, inputs);
                        break;
                    case "ehlo":
                        context.WriteLine("250-{inputs[1]}");
                        context.WriteLine("250 AUTH PLAIN");
                        context.LastCommand = COMMAND_HELO;
                        break;
                    case "rset":
                        Rset(context);
                        break;
                    case "noop":
                        context.WriteLine(MESSAGE_OK);
                        break;
                    case "quit":
                        isRunning = false;
                        context.WriteLine(MESSAGE_GOODBYE);
                        context.Close();
                        break;
                    case "mail":
                        if (inputs[1].ToLower().StartsWith("from"))
                        {
                            Mail(context, inputLine.Substring(inputLine.IndexOf(" ")));
                            break;
                        }

                        context.WriteLine(MESSAGE_UNKNOWN_COMMAND);
                        break;
                    case "rcpt":
                        if (inputs[1].ToLower().StartsWith("to"))
                        {
                            Rcpt(context, inputLine.Substring(inputLine.IndexOf(" ")));
                            break;
                        }

                        context.WriteLine(MESSAGE_UNKNOWN_COMMAND);
                        break;
                    case "data":
                        Data(context);
                        break;
                    case "auth":
                        context.WriteLine("235 Authentication successful.");
                        break;
                    default:
                        context.WriteLine(MESSAGE_UNKNOWN_COMMAND);
                        break;
                }
            }
            catch (SocketException sx)
            {
                if (sx.ErrorCode == 10060)
                {
                    context.WriteLine(MESSAGE_GOODBYE);
                }
                else
                {
                    log.Error("Processing exception", sx);
                }


                isRunning = false;
                context.Socket.Dispose();
            }
            catch (Exception)
            {
                isRunning = false;
                context.Socket.Dispose();
            }
        }
    }

    /// <summary>
    /// Handle the RCPT TO:&lt;address&gt; command.
    /// </summary>
    private void Rcpt(SmtpContext context, string argument)
    {
        if (context.LastCommand == COMMAND_MAIL || context.LastCommand == COMMAND_RCPT)
        {
            var address = ParseAddress(argument);
            if (!string.IsNullOrEmpty(address))
            {
                try
                {
                    var emailAddress = new EmailAddress(address);
                    context.Message.AddRecipient(emailAddress);
                    context.LastCommand = COMMAND_RCPT;
                    context.WriteLine(MESSAGE_OK);
                }
                catch
                {
                    context.WriteLine(MESSAGE_INVALID_ADDRESS);
                }
            }
            else
            {
                context.WriteLine(MESSAGE_INVALID_ADDRESS);
            }
        }
        else
        {
            context.WriteLine(MESSAGE_INVALID_COMMAND_ORDER);
        }
    }

    /// <summary>
    /// Reset the connection state.
    /// </summary>
    private void Rset(SmtpContext context)
    {
        if (context.LastCommand != -1)
        {
            // Dump the message and reset the context.
            context.Reset();
            context.WriteLine(MESSAGE_OK);
        }
        else
        {
            context.WriteLine(MESSAGE_INVALID_COMMAND_ORDER);
        }
    }

    /// <summary>
    /// Sends the welcome greeting to the client.
    /// </summary>
    private void SendWelcomeMessage(SmtpContext context)
    {
        context.WriteLine(WelcomeMessage);
    }
}
