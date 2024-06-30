using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 位置模式速度设置
	/// </summary>
	public readonly struct LimitSpdParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 30;
		public float MinValue { get; init; } = 0;
		public ushort Index { get; init; } = 0X7017;
		public float Value { get; init; }

		public LimitSpdParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
