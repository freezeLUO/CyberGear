using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ParamTable
{
	[Flags]
	public enum Access
	{
		/// <summary>
		/// 读
		/// </summary>
		Read,
		/// <summary>
		/// 写
		/// </summary>
		Write,
	}
}
