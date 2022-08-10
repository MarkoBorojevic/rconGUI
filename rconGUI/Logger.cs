using System;
using System.Collections.Generic;
using System.Text;

namespace rconGUI
{
    public class Logger
    {
        Action<string> logEvent;

        public Logger(Action<string> logEvent)
        {
            this.logEvent = logEvent;
        }

        public void Log(string message)
        {
            logEvent?.Invoke(message);
        }
    }
}
