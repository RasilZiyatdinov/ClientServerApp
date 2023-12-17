using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    public class ServerSettings
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return Address + ":" + Port.ToString();
        }
    }
}
