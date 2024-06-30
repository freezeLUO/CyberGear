using System.Buffers.Binary;

namespace CyberGear.Control.Protocols;

/// <summary>
/// 发送协议
/// </summary>
public readonly struct SendProtocol
{
	/// <summary>
	/// 通讯类型
	/// </summary>
	/// <remarks>
	/// 5 bits
	/// </remarks>
	public CmdMode CmdMode { get; init; }

	/// <summary>
	/// 主控制器 CANID
	/// </summary>
	/// <remarks>
	/// 16 bits
	/// </remarks>
	public ushort MasterCanId { get; init; }

	/// <summary>
	/// 目标点击 CANID
	/// </summary>
	/// <remarks>
	/// 8 bits
	/// </remarks>
	public byte MotorCanId { get; init; }

	/// <summary>
	/// 数据区
	/// </summary>
	/// <remarks>
	/// 8 bytes
	/// </remarks>
	public byte[] Data { get; init; } = new byte[8];

	public SendProtocol(CmdMode cmdMode, ushort masterCanId, byte motorCanId)
	{
		CmdMode = cmdMode;
		MasterCanId = masterCanId;
		MotorCanId = motorCanId;
	}

	public byte[] ToArray()
	{
		var ret = new byte[12];
		ret[0] = MotorCanId;
		ret[3] = (byte)CmdMode;
		BinaryPrimitives.WriteUInt16BigEndian(ret.AsSpan()[1..], MasterCanId);
		if (BinaryPrimitives.ReadInt64BigEndian(Data) != 0)
			Array.Copy(Data, 0, ret, 4, Data.Length);
		return ret;
	}
}
