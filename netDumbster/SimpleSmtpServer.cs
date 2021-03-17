// Copyright (c) 2010, Hexasystems Corporation
// All rights reserved.

namespace netDumbster.smtp
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using netDumbster.smtp.Logging;

    /// <summary>
    /// Simple Smtp Server
    /// </summary>
    public class SimpleSmtpServer : IDisposable
    {
        /// <summary>
        /// Logger
        /// </summary>
        ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Stores all of the email received since this instance started up.
        /// </summary>
        private ConcurrentBag<SmtpMessage> smtpMessageStore = new ConcurrentBag<SmtpMessage>();

        /// <summary>
        ///  CancellationTokenSource to stop server
        /// </summary>
        CancellationTokenSource cancellation = new CancellationTokenSource();

        /// <summary>
        /// TCP Listener
        /// </summary>
        private TcpListener? tcpListener;

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
            this.Configuration = configuration;
            this.ServerReady = new AutoResetEvent(false);
        }

        public event EventHandler<MessageReceivedArgs>? MessageReceived;

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
                    return this.smtpMessageStore.ToArray();
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
                    return this.smtpMessageStore.Count;
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
            return SimpleSmtpServer.Start(Configuration.Configure().WithRandomPort());
        }

        /// <summary>
        /// Starts the specified use message store.
        /// </summary>
        /// <param name="useMessageStore">if set to <c>true</c> [use message store].</param>
        /// <returns></returns>
        public static SimpleSmtpServer Start(bool useMessageStore)
        {
            return SimpleSmtpServer.Start(Configuration.Configure().WithRandomPort().EnableMessageStore(useMessageStore));
        }

        /// <summary>
        /// Starts server listening to the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns></returns>
        public static SimpleSmtpServer Start(int port)
        {
            return SimpleSmtpServer.Start(port, true);
        }

        /// <summary>
        /// Starts the specified port.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <param name="useMessageStore">if set to <c>true</c> [use message store].</param>
        /// <returns></returns>
        public static SimpleSmtpServer Start(int port, bool useMessageStore)
        {
            return SimpleSmtpServer.Start(Configuration.Configure().WithPort(port).EnableMessageStore(useMessageStore));
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
                SmtpMessage itemToRemove;
                while (!this.smtpMessageStore.IsEmpty)
                {
                    this.smtpMessageStore.TryTake(out itemToRemove);
                }
            }
        }

        /// <summary>
        /// Stop the server.  This notifies the listener to stop accepting new connections
        /// and that the loop should exit.
        /// </summary>
        public void Stop()
        {
            this.log.Debug("Trying to stop SmtpServer.");

            try
            {
                lock (this)
                {
                    this.cancellation.Cancel();

                    // Kick the server accept loop
                    if (this.tcpListener != null)
                    {
                        // _processor.Stop();
                        this.log.Debug("Stopping tcp listener.");
                        this.tcpListener.Stop();
                        this.log.Debug("Tcp listener stopped.");
                    }

                    this.tcpListener = null;
                }
            }
            catch (Exception ex)
            {
                this.log.Warn("Unexpected Exception stopping SmtpServer", ex);
            }
            finally
            {
                this.log.Debug("SmtpServer Stopped.");
                this.ServerReady.Close();
            }
        }

        /// <summary>
        /// Starts the Server
        /// </summary>
        internal void StartListening()
        {
            this.log.Info("Starting Smtp server");

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && this.Configuration.Port < 1000)
            {
                log.Warn($"POSIX system detected. Root access may be needed to open port: {this.Configuration.Port}.");
            }

            var endPoint = new IPEndPoint(this.Configuration.IPAddress, this.Configuration.Port);
            this.tcpListener = new TcpListener(endPoint);

            // Fix the problem with the scenario if the server is stopped, and then
            // restarted with the same port, it will not throw an error.
            if (this.Configuration.ReuseAddress)
            {
                this.tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            }
            this.tcpListener.Start();

            this.log.DebugFormat("Started Tcp Listener at port {0}", this.Configuration.Port);

            this.ServerReady.Set();
            try
            {
                Task.Factory.StartNew(async () =>
                   {
                       while (this.tcpListener.Server.IsBound)
                       {
                           var socket = await this.tcpListener.AcceptSocketAsync();
                           if (socket == null)
                           {
                               break;
                           }

                           this.SocketHandler(socket);
                       }
                   },
                   cancellation.Token);
            }
            catch (Exception ex)
            {
                this.log.Warn("Unexpected Exception starting the SmtpServer.", ex);
            }
        }

        /// <summary>
        /// Async Socket handler.
        /// </summary>
        /// <param name="result">The result.</param>
        private void SocketHandler(Socket socket)
        {
            if (this.cancellation.IsCancellationRequested)
            {
                return;
            }

            this.log.Debug("Entering Socket Handler.");

            try
            {
                using (socket)
                {
                    this.log.Debug("Socket accepted and ready to be processed.");
                    var processor = new SmtpProcessor(string.Empty, this.Configuration.UseMessageStore ? this.smtpMessageStore : null);
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
                this.log.Warn("Object Disposed Exception. THIS IS EXPECTED ONLY IF SERVER WAS STOPPED.", ex);
            }
            catch (SocketException ex)
            {
                this.log.Warn("Socket Exception", ex);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}