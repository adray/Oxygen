using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public class Logger
    {
        private static Logger? instance;
        private StreamWriter? _writer;
        private object _lock = new object();

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }

                return instance;
            }
        }

        public Logger()
        {
            try
            {
                _writer = new StreamWriter("log.txt");
            }
            catch (Exception)
            {
                // Nothing will be logged
            }
        }

        public void Log(string message, params object[] args)
        {
            if (_writer != null)
            {
                lock (_lock)
                {
                    _writer.Write($"{DateTime.Now}: ");
                    _writer.WriteLine(string.Format(message, args));
                    _writer.Flush();
                }
            }
        }
    }
}
