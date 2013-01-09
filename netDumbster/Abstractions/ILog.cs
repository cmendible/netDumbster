using System;

namespace netDumbster.smtp.Abstractions
{
  public interface ILog
  {
    void Info(string message);
    void Debug(string message);
    void Warn(string message, Exception exception);
    void DebugFormat(string message, params object[] parameters);
  }
}