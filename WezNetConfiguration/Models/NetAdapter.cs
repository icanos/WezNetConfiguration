using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WezNetConfiguration.Models
{
    public class NetAdapter
    {
        public NetAdapter(string displayName, string bindName)
        {
            DisplayName = displayName;
            BindName = bindName;
        }

        public string DisplayName { get; set; }

        public string BindName { get; set; }
    }
}
