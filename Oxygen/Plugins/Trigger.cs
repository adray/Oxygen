using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxygen
{
    internal class Trigger
    {
        private readonly string triggerName;
        private readonly List<string> conditions = new List<string>();

        public Trigger(string triggerName)
        {
            this.triggerName = triggerName;
        }

        public string TriggerName => triggerName;
        public IEnumerable<string> Conditions => conditions;

        public void AddConditions(IList<string> conditions)
        {
            this.conditions.AddRange(conditions);
        }
    }
}
