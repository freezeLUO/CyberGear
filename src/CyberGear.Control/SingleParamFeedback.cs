using Peak.Can.Basic;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control
{
	/// <summary>
	/// 单个参数读取应答帧
	/// </summary>
	public struct SingleParamFeedback
	{
		/// <summary>
		/// 目标电机 can id
		/// </summary>
		public uint MotorCanId { get; init; }

		/// <summary>
		/// 主机 can id
		/// </summary>
		public uint MasterCanId { get; init; }

		/// <summary>
		/// 序号
		/// </summary>
		public ushort Index { get; init; }

		/// <summary>
		/// 参考数据
		/// </summary>
		public byte[] Value { get; set; }

		public static SingleParamFeedback Parse(PcanMessage pcanMessage)
		{
			var dataSpan = ((byte[])pcanMessage.Data).AsSpan();
			return new SingleParamFeedback
			{
				MasterCanId = pcanMessage.ID & 0xFF,
				MotorCanId = (pcanMessage.ID >> 8) & 0xFF,
				Index = BinaryPrimitives.ReadUInt16BigEndian(dataSpan[0..2]),
				Value = dataSpan[4..8].ToArray()
			};
		}

		public override string ToString()
		{
			return
				$"{nameof(MotorCanId)}: {MotorCanId}, {nameof(MasterCanId)}: {MasterCanId}, {nameof(Index)}: {Index}, {nameof(Value)}: {Value}";
		}
	}
}
