using Peak.Can.Basic.BackwardCompatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.ReceiveMessageType
{
	public interface IMessageType
	{
		ulong CanTimestamp { get; }
		byte MotorCanId { get; }
		byte MasterCanId { get; }
		byte ComType { get; }
	}

	/// <summary>
	/// 空信息
	/// </summary>
	class NullMessageType : IMessageType
	{
		public ulong CanTimestamp => 0;
		public byte MotorCanId => 0;
		public byte MasterCanId => 0;
		public byte ComType => 0;
	}

	/// <summary>
	/// 信息
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class MessageType<T> : IMessageType
	{
		public ulong CanTimestamp { get; set; }
		public byte MotorCanId { get; set; }
		public byte MasterCanId { get; set; }
		public byte ComType { get; set; }
		public T Data { get; set; }

		public MessageType(ulong canTimestamp, byte motorCanId, byte masterCanId, byte comType, T data)
		{
			CanTimestamp = canTimestamp;
			MotorCanId = motorCanId;
			MasterCanId = masterCanId;
			ComType = comType;
			Data = data;
		}
	}
}
