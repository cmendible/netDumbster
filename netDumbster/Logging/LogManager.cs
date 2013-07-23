namespace netDumbster.smtp.Logging
{
    using System;

    public class LogManager
    {
        public static Func<Type, ILog> GetLogger = type => new EmptyLogger();

        public LogManager()
        {
        }
    }
}