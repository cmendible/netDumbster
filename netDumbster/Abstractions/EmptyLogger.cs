using System;

namespace netDumbster.smtp.Abstractions
{
  public class EmptyLogger : ILog
  {
    public void Info(string message)
    {
    }

    public void Debug(string message)
    {
      
    }

    public void Warn(string message, Exception exception)
    {
      
    }

    public void DebugFormat(string message, params object[] parameters)
    {
      
    }
  }
}