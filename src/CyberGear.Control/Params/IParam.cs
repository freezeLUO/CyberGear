using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
    public interface IParam<T> where T : struct, IComparable<T>
    {
        /// <summary>
        /// 参数
        /// </summary>
        public ushort Index { get; init; }

        public T Value { get; init; }

        public byte[] ToArray();
    }
}
