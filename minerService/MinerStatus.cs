using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace minerService
{
    class MinerStatus
    {
        public string user { get; set; }
        public string pool { get; set; }

        public int GPUtemp { get; set; }

        public bool running { get; set; }
    }
}
