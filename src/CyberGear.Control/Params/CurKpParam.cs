using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Params
{
	/// <summary>
	/// 电流的 Kp
	/// </summary>
	public readonly struct CurKpParam : IParam<float>
	{
		public ushort Index { get; init; } = 0X7010;
		public float Value { get; init; }

		public CurKpParam(float value = (float)0.125)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
