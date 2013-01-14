namespace netDumbster.smtp.Logging
{
    using System;

    public class LogManager
    {
        #region Fields

        public static Func<Type, ILog> GetLogger = type => new EmptyLogger();

        #endregion Fields

        #region Constructors

        public LogManager()
        {
        }

        #endregion Constructors
    }
}