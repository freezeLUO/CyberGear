using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
    public interface ILimitParam<T> : IParam<T> where T : struct, IComparable<T>
    {
        public T MaxValue { get; init; }

        public T MinValue { get; init; }
    }
}
