using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	public struct BootBuildDate : IParam<string>
	{
		public ushort FunctionCode => 0X1001;

		public string Value { get; set; }

		public Access Access => Access.Read;

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
