using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRailTracker.Service
{
    public class DataService<T>
    {
        public T Data { get; set; } = default!;
    }

}
