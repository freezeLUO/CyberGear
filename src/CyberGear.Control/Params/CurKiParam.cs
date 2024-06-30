using System;
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

		public CurKiParam(float value = (float)0.0158)
		{
			Value = value;
		}

		public byte[] ToArray()
		{
			throw new NotImplementedException();
		}
	}
}
