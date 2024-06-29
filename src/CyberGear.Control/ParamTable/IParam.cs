using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	public interface IParam<T>
	{
		/// <summary>
		/// 功能码
		/// </summary>
		public ushort FunctionCode { get; }

		public T Value { get; set; }

		public Access Access { get; }

		public byte[] ToArray();
	}
}
