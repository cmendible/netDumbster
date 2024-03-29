// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp;

/// <summary>
/// Simple Smtp Server
/// </summary>
public class SimpleSmtpServer : IDisposable
{
    /// <summary>
    /// Logger
    /// </summary>
    readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /// <summary>
    /// Stores all of the email received since this instance started up.
    /// </summary>
    private readonly ConcurrentBag<SmtpMessage> smtpMessageStore = [];

    /// <summary>
    ///  CancellationTokenSource to stop server
    /// </summary>
    readonly CancellationTokenSource cancellation = new();

    /// <summary>
    /// TCP Listener
    /// </summary>
    private TcpListener tcpListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleSmtpServer"/> class.
    /// </summary>
    /// <param name="port">The port.</param>
    private SimpleSmtpServer(int port)
        : this(port, true)
    {
    }

    /// <summary>
    /// Prevents a default instance of the <see cref="SimpleSmtpServer"/> class from being created.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="useMessageStore">if set to <c>true</c> [use message store].</param>
    private SimpleSmtpServer(int port, bool useMessageStore)
        : this(Configuration.Configure().WithPort(port).EnableMessageStore(useMessageStore))
    {
    }

    private SimpleSmtpServer(Configuration configuration)
    {
        Configuration = configuration;
        ServerReady = new AutoResetEvent(false);
    }

    public event EventHandler<MessageReceivedArgs> MessageReceived;

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    /// <value>
    /// The configuration.
    /// </value>
    public Configuration Configuration
    {
        get;
        private set;
    }

    /// <summary>
    /// List of email received by this instance since start up.
    /// </summary>
    /// <value><see cref="T:System.Array">Array</see> holding received <see cref="T:netDumbster.smtp.SmtpMessage">SmtpMessage</see></value>
    public virtual SmtpMessage[] ReceivedEmail
    {
        get
        {
            lock (this)
            {
                return [.. smtpMessageStore];
            }
        }
    }

    /// <summary>
    /// Number of messages received by this instance since start up.
    /// </summary>
    /// <value>Number of messages</value>
    public virtual int ReceivedEmailCount
    {
        get
        {
            lock (this)
            {
                return smtpMessageStore.Count;
            }
        }
    }

    internal AutoResetEvent ServerReady
    {
        get;
        set;
    }

    /// <summary>
    /// Starts this instance.
    /// </summary>
    /// <returns></returns>
    public static SimpleSmtpServer Start()
    {
        return Start(Configuration.Configure().WithRandomPort());
    }

    /// <summary>
    /// Starts the specified use message store.
    /// </summary>
    /// <param name="useMessageStore">if set to <c>true</c> [use message store].</param>
    /// <returns></returns>
    public static SimpleSmtpServer Start(bool useMessageStore)
    {
        return Start(Configuration.Configure().WithRandomPort().EnableMessageStore(useMessageStore));
    }

    /// <summary>
    /// Starts server listening to the specified port.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <returns></returns>
    public static SimpleSmtpServer Start(int port)
    {
        return Start(port, true);
    }

    /// <summary>
    /// Starts the specified port.
    /// </summary>
    /// <param name="port">The port.</param>
    /// <param name="useMessageStore">if set to <c>true</c> [use message store].</param>
    /// <returns></returns>
    public static SimpleSmtpServer Start(int port, bool useMessageStore)
    {
        return Start(Configuration.Configure().WithPort(port).EnableMessageStore(useMessageStore));
    }

    internal static SimpleSmtpServer Start(Configuration configuration)
    {
        var server = new SimpleSmtpServer(configuration);
        server.StartListening();
        server.ServerReady.WaitOne();
        return server;
    }

    /// <summary>
    /// Clears the received email.
    /// </summary>
    public void ClearReceivedEmail()
    {
        lock (this)
        {
            while (!smtpMessageStore.IsEmpty)
            {
                smtpMessageStore.TryTake(out SmtpMessage itemToRemove);
            }
        }
    }

    /// <summary>
    /// Stop the server.  This notifies the listener to stop accepting new connections
    /// and that the loop should exit.
    /// </summary>
    public void Stop()
    {
        log.Debug("Trying to stop SmtpServer.");

        try
        {
            lock (this)
            {
                cancellation.Cancel();

                // Kick the server accept loop
                if (tcpListener != null)
                {
                    log.Debug("Stopping tcp listener.");
                    tcpListener.Stop();
                    log.Debug("Tcp listener stopped.");
                }

                tcpListener = null;
            }
        }
        catch (Exception ex)
        {
            log.Warn("Unexpected Exception stopping SmtpServer", ex);
        }
        finally
        {
            log.Debug("SmtpServer Stopped.");
            ServerReady.Close();
        }
    }

    /// <summary>
    /// Starts the Server
    /// </summary>
    internal void StartListening()
    {
        log.Info("Starting Smtp server");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Configuration.Port < 1000)
        {
            log.Warn($"POSIX system detected. Root access may be needed to open port: {Configuration.Port}.");
        }

        var endPoint = new IPEndPoint(Configuration.IPAddress, Configuration.Port);
        tcpListener = new TcpListener(endPoint);

        // Fix the problem with the scenario if the server is stopped, and then
        // restarted with the same port, it will not throw an error.
        if (Configuration.ReuseAddress)
        {
            tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
        }
        tcpListener.Start();

        log.DebugFormat("Started Tcp Listener at port {0}", Configuration.Port);

        ServerReady.Set();
        try
        {
            Task.Factory.StartNew(async () =>
               {
                   while (tcpListener.Server.IsBound)
                   {
                       var socket = await tcpListener.AcceptSocketAsync();
                       if (socket == null)
                       {
                           break;
                       }

                       SocketHandler(socket);
                   }
               },
               cancellation.Token);
        }
        catch (Exception ex)
        {
            log.Warn("Unexpected Exception starting the SmtpServer.", ex);
        }
    }

    /// <summary>
    /// Async Socket handler.
    /// </summary>
    /// <param name="result">The result.</param>
    private void SocketHandler(Socket socket)
    {
        if (cancellation.IsCancellationRequested)
        {
            return;
        }

        log.Debug("Entering Socket Handler.");

        try
        {
            using (socket)
            {
                log.Debug("Socket accepted and ready to be processed.");
                var processor = new SmtpProcessor(string.Empty, Configuration.UseMessageStore ? smtpMessageStore : null);
                processor.MessageReceived += (sender, args) =>
                {
                    if (MessageReceived != null)
                    {
                        MessageReceived(this, args);
                    }
                };
                processor.ProcessConnection(socket);
            }
        }
        catch (ObjectDisposedException ex)
        {
            log.Warn("Object Disposed Exception. THIS IS EXPECTED ONLY IF SERVER WAS STOPPED.", ex);
        }
        catch (SocketException ex)
        {
            log.Warn("Socket Exception", ex);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
