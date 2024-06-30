using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 速度位置模式电流设置
	/// </summary>
	public readonly struct LimitCurParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 27;
		public float MinValue { get; init; } = 0;
		public ushort Index { get; init; } = 0X7018;
		public float Value { get; init; }

		public LimitCurParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
