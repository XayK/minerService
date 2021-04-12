using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace minerService
{
    static class RunsState
    {
        private static int runs;

        public static int Runs { get => runs; set => runs = value; }
    }
}
