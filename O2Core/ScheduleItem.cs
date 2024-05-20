using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    public class ScheduleItem
    {
        private readonly bool running;
        private readonly string name;
        private readonly string date;
        private readonly string startedBy;
        private readonly bool startedManually;

        public ScheduleItem(bool running, string name, string date, string startedBy, bool startedManually)
        {
            this.running = running;
            this.name = name;
            this.date = date;
            this.startedBy = startedBy;
            this.startedManually = startedManually;
        }

        public bool Running => running;
        public string Name => name;
        public string Date => date;
        public string StartedBy => startedBy;
        public bool StartedManually => startedManually;
    }
}
