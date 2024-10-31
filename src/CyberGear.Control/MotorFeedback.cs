using Peak.Can.Basic;
using System.Buffers.Binary;

namespace CyberGear.Control
{
	/// <summary>
	/// 应答电机反馈帧
	/// </summary>
	public struct MotorFeedback
	{
		/// <summary>
		/// 主机 can id
		/// </summary>
		public uint MasterCanId { get; init; }

		/// <summary>
		/// 故障信息
		/// </summary>
		public bool ErrorMessage { get; init; }

		/// <summary>
		/// HALL 编码故障
		/// </summary>
        public bool HALLError { get; set; }

        /// <summary>
        /// 磁编码故障
        /// </summary>
        public byte MagneticError { get; init; }

		/// <summary>
		/// 过温
		/// </summary>
        public bool Overheating { get; init; }

		/// <summary>
		/// 过流
		/// </summary>
        public bool Overcurrent { get; init; }

		/// <summary>
		/// 欠压
		/// </summary>
        public bool Undervoltage { get; init; }

		/// <summary>
		/// 模式
		/// 0 - Reset
		/// 1 - Cali
		/// 2 - Motor
		/// </summary>
        public int Mode { get; init; }

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

		/// <summary>
		/// 当前温度
		/// </summary>
		public double Temp { get; init; }

		public static MotorFeedback Parse(PcanMessage pcanMessage)
		{
			var dataSpan = ((byte[])pcanMessage.Data).AsSpan();
			return new MotorFeedback
			{
				MasterCanId = (pcanMessage.ID >> 8) & 0xFF,
				ErrorMessage = ((pcanMessage.ID >> 16) & 0xFF) == 1,
				HALLError = (pcanMessage.ID >> 20 & 0xFF) == 1,
				Overheating = (pcanMessage.ID >> 18 & 0xFF) == 1,
				Overcurrent = (pcanMessage.ID >> 17 & 0xFF) == 1,
				Undervoltage = (pcanMessage.ID >> 16 & 0xFF) == 1,
				Mode = (int)((pcanMessage.ID >> 22) & 0xFF),
				Angle = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(dataSpan[0..2]), ushort.MaxValue, -4, 4),
				AngularVelocity = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(dataSpan[2..4]), ushort.MaxValue, -30, 30),
				Torque = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(dataSpan[4..6]), ushort.MaxValue, -12, 12),
				Temp = RangeConvetor(BinaryPrimitives.ReadUInt16BigEndian(dataSpan[6..8]), ushort.MaxValue, 0, 500),
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
