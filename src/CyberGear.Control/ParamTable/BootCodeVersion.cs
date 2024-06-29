using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	public struct BootCodeVersion : IParam<string>
	{
		public readonly ushort FunctionCode => 0X1000;
		public string? Value { get; set; }
		public readonly Access Access => Access.Read;

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
