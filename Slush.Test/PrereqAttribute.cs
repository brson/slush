using System;
using System.Collections.Generic;
using System.Text;

namespace Slush
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple=true)]
    public class PrereqAttribute : Attribute
    {
        private string name;

        public PrereqAttribute(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

    }
}
