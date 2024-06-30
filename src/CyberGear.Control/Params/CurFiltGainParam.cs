using System;
using System.Buffers.Binary;
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
			var ret = new byte[8];
			BinaryPrimitives.WriteUInt16BigEndian(ret.AsSpan(), Index);
			var floatArray = BitConverter.GetBytes(Value);
			Array.Copy(floatArray, 0, ret, 4, floatArray.Length);
			return ret;
		}
	}
}
