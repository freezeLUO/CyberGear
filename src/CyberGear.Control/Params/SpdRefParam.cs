using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 转速模式转速指令
	/// </summary>
	public readonly struct SpdRefParam : ILimitParam<float>
	{
		public float MaxValue { get; init; } = 30;
		public float MinValue { get; init; } = -30;
		public ushort Index { get; init; } = 0X700A;
		public float Value { get; init; }

		public SpdRefParam(float value)
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
