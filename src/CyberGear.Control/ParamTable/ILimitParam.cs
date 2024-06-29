using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	public interface ILimitParam<T> : IParam<T> where T : struct
	{
		public T MaxValue { get; set; }

		public T MinValue { get; set; }
	}
}
