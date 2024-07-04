using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Protocols
{
	/// <summary>
	/// 应答电机反馈帧
	/// </summary>
	public readonly struct ResponseData
	{
		/// <summary>
		/// 目标角度
		/// </summary>
		public double Angle { get; init; }

		/// <summary>
		/// 目标角速度
		/// </summary>
		public double AngularVelocity { get; init; }

		public double Kp { get; init; }

		public double Kd { get; init; }

		public static ResponseData Parse(byte[] array)
		{
			return new ResponseData
			{
				Angle = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[0..2]), ushort.MaxValue, -4, 4),
				AngularVelocity = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[2..4]), ushort.MaxValue, -30, 30),
				Kp = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[4..6]), ushort.MaxValue, 0, 500),
				Kd = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[6..8]), ushort.MaxValue, 0, 500),
			};
		}

		/// <summary>
		/// 比例转换
		/// </summary>
		/// <param name="value">值</param>
		/// <param name="sourceRange">源范围</param>
		/// <param name="min">最小值</param>
		/// <param name="max">最大值</param>
		/// <returns></returns>
		internal static double RangeConvetor(int value, int sourceRange, int min, int max)
		{
			double targetRange = max - min;
			// 计算比例因子
			double scaleFactor = targetRange / sourceRange;
			// 应用映射
			double mappedValue = value * scaleFactor + min;
			// 四舍五入并转换为整数
			return mappedValue;
		}
	}
}
