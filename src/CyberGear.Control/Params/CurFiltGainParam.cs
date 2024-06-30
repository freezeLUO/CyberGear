using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 电流滤波系数
	/// </summary>
	public readonly struct CurFiltGainParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 1;
		public float MinValue { get; init; } = 0;
		public ushort Index { get; init; } = 0X7014;
		public float Value { get; init; }

		public CurFiltGainParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
