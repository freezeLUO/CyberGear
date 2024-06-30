using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 转矩限制
	/// </summary>
	public readonly struct LimitTorqueParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 12;
		public float MinValue { get; init; } = 0;
		public ushort Index { get; init; } = 0X700B;
		public float Value { get; init; }

		public LimitTorqueParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
