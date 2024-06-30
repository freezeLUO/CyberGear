using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 位置模式角度指令
	/// </summary>
	public readonly struct LocRefParam : IParam<float>
	{
		public ushort Index { get; init; } = 0X7016;
		public float Value { get; init; }

		public LocRefParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
