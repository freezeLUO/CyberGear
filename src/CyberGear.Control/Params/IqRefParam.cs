using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 电流模式Iq指令
	/// </summary>
	public readonly struct IqRefParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 27;
		public float MinValue { get; init; } = -27;
		public ushort Index { get; init; } = 0X7006;
		public float Value { get; init; }

		public IqRefParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
