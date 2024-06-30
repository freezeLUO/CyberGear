using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 运行模式
	/// </summary>
	public readonly struct RunModeParam : IParam<byte>
	{
		public ushort Index { get; init; } = 0X7005;
		public byte Value { get; init; }

		public RunModeParam(RunMode runMode)
		{
			Value = (byte)runMode;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
