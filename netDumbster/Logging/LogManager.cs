namespace netDumbster.smtp.Logging;

public class LogManager
{
    public static Func<Type, ILog> GetLogger = type => new EmptyLogger();

    public LogManager()
    {
    }
}
