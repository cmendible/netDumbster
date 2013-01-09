using System;

namespace netDumbster.smtp.Abstractions
{
//  public class 
//  {
//    public static ILog GetLogger(Type type)
//    {
//      return EmptyLogger.Instance;
//    }
//  }

  public class LogManager
  {
    public static event Action<LogManager> LogManagerCreated = delegate { };
    public static Func<Type,ILog> GetLogger = type => new EmptyLogger();
    public LogManager()
    {
      LogManagerCreated(this);
    }
  }

}

