using System;
using System.Buffers.Binary;
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
		public float MaxValue { get; init; } = 23;
		public float MinValue { get; init; } = -23;
		public ushort Index { get; init; } = 0X7006;
		public float Value { get; init; }

		public IqRefParam(float value)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			var ret = new byte[8];
			BinaryPrimitives.WriteUInt16LittleEndian(ret.AsSpan(), Index);
			BinaryPrimitives.WriteSingleLittleEndian(ret.AsSpan()[4..], Value);
			return ret;
		}
	}
}
