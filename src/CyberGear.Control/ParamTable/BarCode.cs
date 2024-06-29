using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	public struct BarCode : IParam<string>
	{
		public readonly ushort FunctionCode => 0X0001;
		public string? Value { get; set; }
		public readonly Access Access => Access.Read | Access.Write;

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
