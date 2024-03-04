using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public class Audit
    {
        private static Audit? instance;
        private StreamWriter? _writer;
        private object _lock = new object();

        public static Audit Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Audit();
                }

                return instance;
            }
        }

        public Audit()
        {
            try
            {
                _writer = new StreamWriter("audit.txt", true);
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
