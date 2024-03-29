namespace netDumbster.smtp;

/// <summary>
/// 
/// </summary>
public class Configuration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Configuration"/> class.
    /// </summary>
    internal Configuration()
    {
        UseMessageStore = true;
        IPAddress = IPAddress.Any;
        ReuseAddress = true;
    }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    /// <value>The port.</value>
    public int Port
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets the ip address.
    /// </summary>
    /// <value>
    /// The ip address.
    /// </value>
    public IPAddress IPAddress
    {
        get;
        internal set;
    }

    /// <summary>
    /// Gets a value indicating whether [use message store].
    /// </summary>
    /// <value>
    ///   <c>true</c> if [use message store]; otherwise, <c>false</c>.
    /// </value>
    public bool UseMessageStore
    {
        get;
        internal set;
    }

    public bool ReuseAddress
    {
        get;
        internal set;
    }

    /// <summary>
    /// Configures this instance.
    /// </summary>
    /// <returns></returns>
    public static Configuration Configure()
    {
        return new Configuration();
    }

    /// <summary>
    /// Builds this instance.
    /// </summary>
    /// <returns></returns>
    public SimpleSmtpServer Build()
    {
        var config = this;
        if (Port == 0)
        {
            config = WithRandomPort();
        }
        return SimpleSmtpServer.Start(config);
    }

    public Configuration DoNotReuseAddress()
    {
        ReuseAddress = false;
        return this;
    }

    /// <summary>
    /// Configures a random port.
    /// </summary>
    /// <returns></returns>
    public Configuration WithRandomPort()
    {
        Port = GetRandomUnusedPort();
        return this;
    }

    /// <summary>
    /// Configures with specified port
    /// </summary>
    /// <param name="port">The port.</param>
    /// <returns></returns>
    public Configuration WithPort(int port)
    {
        Port = port;
        return this;
    }

    /// <summary>
    /// Configures with specified address
    /// </summary>
    /// <param name="address">The address.</param>
    /// <returns></returns>
    public Configuration WithAddress(IPAddress address)
    {
        IPAddress = address;
        return this;
    }

    /// <summary>
    /// Enables the message store.
    /// </summary>
    /// <param name="enable">if set to <c>true</c> [enable].</param>
    /// <returns></returns>
    public Configuration EnableMessageStore(bool enable)
    {
        UseMessageStore = enable;
        return this;
    }

    /// <summary>
    /// Gets the random unused port.
    /// </summary>
    /// <returns></returns>
    private static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Any, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

