namespace netDumbster.smtp.Logging
{
    using System;

    public class ConsoleLogger : ILog
    {
        private const string layout = "{0} - {1} - {2} - {3}";

        private string type;

        public ConsoleLogger(Type type)
        {
            this.type = type.FullName;
        }

        public void Debug(object message)
        {
            this.WriteToConsole("DEBUG", message);
        }

        public void Debug(object message, Exception exception)
        {
            this.WriteToConsole("DEBUG", message, exception);
        }

        public void DebugFormat(string format, params object[] args)
        {
            this.WriteToConsole("DEBUG", format, args);
        }

        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.WriteToConsole("DEBUG", provider, format, args);
        }

        public void Error(object message)
        {
            this.WriteToConsole("ERROR", message);
        }

        public void Error(object message, Exception exception)
        {
            this.WriteToConsole("ERROR", message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            this.WriteToConsole("ERROR", format, args);
        }

        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.WriteToConsole("ERROR", provider, format, args);
        }

        public void Fatal(object message)
        {
            this.WriteToConsole("FATAL", message);
        }

        public void Fatal(object message, Exception exception)
        {
            this.WriteToConsole("FATAL", message, exception);
        }

        public void FatalFormat(string format, params object[] args)
        {
            this.WriteToConsole("FATAL", format, args);
        }

        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.WriteToConsole("FATAL", provider, format, args);
        }

        public void Info(object message)
        {
            this.WriteToConsole("INFO", message);
        }

        public void Info(object message, Exception exception)
        {
            this.WriteToConsole("INFO", message, exception);
        }

        public void InfoFormat(string format, params object[] args)
        {
            this.WriteToConsole("INFO", format, args);
        }

        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.WriteToConsole("INFO", provider, format, args);
        }

        public void Warn(object message)
        {
            this.WriteToConsole("WARNING", message);
        }

        public void Warn(object message, Exception exception)
        {
            this.WriteToConsole("WARNING", message, exception);
        }

        public void WarnFormat(string format, params object[] args)
        {
            this.WriteToConsole("WARNING", format, args);
        }

        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            this.WriteToConsole("WARNING", provider, format, args);
        }

        private static string CurrentDateTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff");
        }

        private void WriteToConsole(string level, object message)
        {
            Console.WriteLine(string.Format(layout, CurrentDateTime(), level, this.type, message));
        }

        private void WriteToConsole(string level, object message, Exception exception)
        {
            Console.WriteLine(string.Format(layout, CurrentDateTime(), level, this.type, string.Format("{0} Exception: {1}", message, exception.ToString())));
        }

        private void WriteToConsole(string level, string format, params object[] args)
        {
            Console.WriteLine(string.Format(layout, CurrentDateTime(), level, this.type, string.Format(format, args)));
        }

        private void WriteToConsole(string level, IFormatProvider provider, string format, params object[] args)
        {
            Console.WriteLine(string.Format(layout, CurrentDateTime(), level, this.type, string.Format(provider, format, args)));
        }
    }
}