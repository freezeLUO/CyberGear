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
		/// 当前角度
		/// </summary>
		public double Angle { get; init; }

		/// <summary>
		/// 当前角速度
		/// </summary>
		public double AngularVelocity { get; init; }

        /// <summary>
        /// 当前力矩
        /// </summary>
        public double Torque { get; init; }

        ///// <summary>
        ///// 当前温度
        ///// </summary>
        //public double Temp { get; init; }

		public static ResponseData Parse(byte[] array)
		{
			return new ResponseData
			{
				Angle = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[0..2]), ushort.MaxValue, -4, 4),
				AngularVelocity = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[2..4]), ushort.MaxValue, -30, 30),
                Torque = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[4..6]), ushort.MaxValue, -12, 12),
                //Temp = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[6..8]), ushort.MaxValue, 0, 500),
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

    public readonly struct SingleResponseData
    {
        /// <summary>
        /// 参数索引
        /// </summary>
        public ushort Index { get; init; }

        /// <summary>
        /// 数据数组
        /// </summary>
        public byte[] value_bytes { get; init; }


        public static SingleResponseData Parse(byte[] array)
        {
			return new SingleResponseData
			{
				Index = BinaryPrimitives.ReadUInt16BigEndian(array.AsSpan()[0..2]),
                value_bytes = array[4..8],
			};
        }

    }

}
