using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 电流的 Ki
	/// </summary>
	public readonly struct CurKiParam : IParam<float>
	{
		public ushort Index { get; init; } = 0X7011;
		public float Value { get; init; }

		public CurKiParam(float value)
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
