using Peak.Can.Basic;
using System.Diagnostics;
using CyberGear.Control.Params;

namespace CyberGear.Control
{
	/// <summary>
	/// 控制器类，用于管理CyberGear控制系统中的CAN总线通信。
	/// </summary>
	/// <remarks>
	/// 该类提供了一系列方法，包括发送和接收CAN消息，解析接收到的消息，
	/// 以及处理与主控制器和电机相关的逻辑。
	/// </remarks>
	public class Controller
	{
		/// <summary>
		/// 主控制器CANID
		/// </summary>
		private readonly uint _masterCANID = 0;
		/// <summary>
		/// 电机CANID
		/// </summary>
		private readonly uint _motorCANID = 0;
		/// <summary>
		/// CAN 通道
		/// </summary>
		private readonly PcanChannel _channel;
		/// <summary>
		/// CAN 通道
		/// </summary>
		public PcanChannel PcanChanne => _channel;

		/// <summary>
		/// 位置最小值
		/// </summary>
		private static double P_MIN = -4 * Math.PI;
		/// <summary>
		/// 位置最大值
		/// </summary>
		private static double P_MAX = 4 * Math.PI;
		/// <summary>
		/// 速度最小值
		/// </summary>
		private static double V_MIN = -30.0;
		/// <summary>
		/// 速度最大值
		/// </summary>
		private static double V_MAX = 30.0;
		/// <summary>
		/// 力矩最小值
		/// </summary>
		private static double T_MIN = -12.0;
		/// <summary>
		/// 力矩最大值
		/// </summary>
		private static double T_MAX = 12.0;
		/// <summary>
		/// 比例系数最小值
		/// </summary>
		private static double KP_MIN = 0.0;
		/// <summary>
		/// 比例系数最大值
		/// </summary>
		private static double KP_MAX = 500.0;
		/// <summary>
		/// 微分系数最小值
		/// </summary>
		private static double KD_MIN = 0.0;
		/// <summary>
		/// 微分系数最大值
		/// </summary>
		private static double KD_MAX = 5.0;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="masterCANID"></param>
		/// <param name="motorCANID">电机 canid</param>
		/// <param name="channel">通道类型</param>
		public Controller(uint masterCANID, uint motorCANID, PcanChannel channel)
		{
			_masterCANID = masterCANID;
			_motorCANID = motorCANID;
			_channel = channel;
		}

		/// <summary>
		/// 发送CAN消息。
		/// </summary>
		/// <param name="cmdMode">仲裁ID通信类型</param>
		/// <param name="data">CAN2.0数据区1</param>
		internal void CanSend(CmdMode cmdMode, byte[] data)
		{
			// 计算仲裁ID
			uint arbitrationId = GetArbitrationId(cmdMode);
			// 一条CAN消息结构
			PcanMessage canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data.Length),
				Data = data
			};
			// Write the CAN message
			PcanStatus writeStatus = Api.Write(_channel, canMessage);
			if (writeStatus != PcanStatus.OK)
			{
				Debug.WriteLine("Failed to send the message.");
			}
			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data)}");
		}

		/// <summary>
		/// 计算仲裁ID
		/// </summary>
		/// <param name="cmdMode"></param>
		/// <returns></returns>
		public uint GetArbitrationId(CmdMode cmdMode) =>
			(uint)cmdMode << 24 | _masterCANID << 8 | _motorCANID;

		// // 异步发送和接收CAN消息
		// public async Task<Tuple<byte[], uint>> SendReceiveCanMessageAsync(uint cmdMode, uint data2, byte[] data1, uint timeout = 200)
		// {
		//     // 计算仲裁ID
		//     uint arbitrationId = (cmdMode << 24) | (data2 << 8) | MotorCANID;

		//     // 构造CAN消息
		//     PcanMessage canMessage = new PcanMessage
		//     {
		//         ID = arbitrationId,
		//         MsgType = MessageType.Extended,
		//         DLC = Convert.ToByte(data1.Length),
		//         Data = data1
		//     };

		//     // 异步发送CAN消息
		//     PcanStatus writeStatus = await Task.Run(() => Api.Write(this.channel, canMessage));
		//     if (writeStatus != PcanStatus.OK)
		//     {
		//         Console.WriteLine("Failed to send the message.");
		//         return Tuple.Create<byte[], uint>(new byte[0], 0);
		//     }

		//     Console.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");

		//     // 异步等待，替代Thread.Sleep
		//     await Task.Delay(50); // 给驱动一些时间来发送消息

		//     // 异步读取CAN消息
		//     var readResult = await Task.Run(() =>
		//     {
		//         PcanMessage receivedMsg;
		//         ulong timestamp;
		//         PcanStatus readStatus = Api.Read(this.channel, out receivedMsg, out timestamp);
		//         return new { readStatus, receivedMsg };
		//     });

		//     if (readResult.readStatus == PcanStatus.OK)
		//     {
		//         byte[]? DB = readResult.receivedMsg.Data;
		//         byte[] bytes = DB;
		//         return Tuple.Create(bytes, readResult.receivedMsg.ID);
		//     }
		//     else
		//     {
		//         Debug.WriteLine("Failed to receive the message or message was not received within the timeout period.");
		//         return Tuple.Create<byte[], uint>(new byte[0], 0);
		//     }
		// }

		/// <summary>
		/// 解析接收到的CAN消息。
		/// </summary>
		/// <param name="data">接收到的数据。</param>
		/// <param name="arbitration_id">接收到的消息的仲裁ID。</param>
		/// <returns>返回一个元组，包含电机的CAN ID、位置（以弧度为单位）、速度（以弧度每秒为单位）和扭矩（以牛米为单位）。</returns>
		public static Tuple<byte, double, double, double> ParseReceivedMsg(byte[] data, uint arbitration_id)
		{
			if (data.Length > 0)
			{
				Debug.WriteLine($"Received message with ID 0x{arbitration_id:X}");

				// 解析电机CAN ID
				byte motor_can_id = (byte)(arbitration_id >> 8 & 0xFF);
				// 解析位置、速度和力矩
				double pos = Calculate.UToF((data[0] << 8) + data[1], P_MIN, P_MAX);
				double vel = Calculate.UToF((data[2] << 8) + data[3], V_MIN, V_MAX);
				double torque = Calculate.UToF((data[4] << 8) + data[5], T_MIN, T_MAX);

				Debug.WriteLine($"Motor CAN ID: {motor_can_id}, pos: {pos:.2f} rad, vel: {vel:.2f} rad/s, torque: {torque:.2f} Nm");

				return new Tuple<byte, double, double, double>(motor_can_id, pos, vel, torque);
			}
			else
			{
				Debug.WriteLine("No message received within the timeout period.");
				return new Tuple<byte, double, double, double>(0, 0, 0, 0);
			}
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="param"></param>
		public void WriteParam<T>(IParam<T> param) where T : struct, IComparable<T>
		{
			var limitParam = param as ILimitParam<T>;
			if (limitParam is not null)
			{
				// 校验
				if (limitParam.Value.CompareTo(limitParam.MinValue) < 0)
					return;
				if (limitParam.Value.CompareTo(limitParam.MaxValue) > 0)
					return;
			}
			//发送CAN消息
			CanSend(CmdMode.SINGLE_PARAM_WRITE, param.ToArray());
		}

		/// <summary>
		/// 设置运行模式
		/// </summary>
		/// <param name="runMode"></param>
		public void SetRunMode(RunMode runMode)
			=> WriteParam(new RunModeParam(runMode));

		/// <summary>
		/// 设置转速模式转速指令
		/// </summary>
		/// <param name="value"></param>
		public void SetIqRef(float value)
			=> WriteParam(new IqRefParam(value));

		/// <summary>
		/// 设置转矩限制
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitTorque(float value)
			=> WriteParam(new LimitTorqueParam(value));

		/// <summary>
		/// 设置电流的 Kp
		/// </summary>
		/// <param name="value"></param>
		public void SetCurKpParam(float value = (float)0.125)
			=> WriteParam(new CurKpParam(value));

		/// <summary>
		/// 设置电流的 Ki
		/// </summary>
		/// <param name="value"></param>
		public void SetCurKiParam(float value = (float)0.0158)
			=> WriteParam(new CurKiParam(value));

		/// <summary>
		/// 设置电流滤波系数
		/// </summary>
		/// <param name="value"></param>
		public void SetCurFiltGainParam(float value = (float)0.1)
			=> WriteParam(new CurFiltGainParam(value));

		/// <summary>
		/// 设置位置模式角度指令
		/// </summary>
		/// <param name="value"></param>
		public void SetLocRefParam(float value)
			=> WriteParam(new LocRefParam(value));

		/// <summary>
		/// 设置位置模式速度设置
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitSpdParam(float value)
			=> WriteParam(new LimitSpdParam(value));

		/// <summary>
		/// 设置速度位置模式电流设置
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitCurParam(float value)
			=> WriteParam(new LimitCurParam(value));

		/// <summary>
		/// 向指定索引处写入单个浮点参数值。
		/// </summary>
		/// <param name="index">参数的索引。</param>
		/// <param name="value">要设置的浮点数值。</param>
		/// <remarks>
		/// 具体的index请参见官方手册
		/// </remarks>
		public void WriteSingleParam(uint index, float value)
		{
			byte[] data_index = BitConverter.GetBytes(index);
			byte[] date_parameter = BitConverter.GetBytes(value);
			//组合2个数组   
			byte[] data = data_index.Concat(date_parameter).ToArray();

			//发送CAN消息
			CanSend(CmdMode.SINGLE_PARAM_WRITE, data);
		}

		[Obsolete]
		/// <summary>
		/// 向指定索引处写入单个字节参数值。
		/// </summary>
		/// <param name="index"></param>
		/// <param name="byteValue"></param>
		public void WriteSingleParam(uint index, byte byteValue)
		{
			// 创建一个只包含这个byte值的数组，并补充三个字节的0
			byte[] bs = new byte[] { byteValue };
			bs = bs.Concat(Enumerable.Repeat((byte)0, 3)).ToArray();
			byte[] data_index = BitConverter.GetBytes(index);
			// 组合index数组和处理后的value数组
			byte[] data1 = data_index.Concat(bs).ToArray();

			// 发送CAN消息
			CanSend(CmdMode.SINGLE_PARAM_WRITE, data1);
		}

		[Obsolete]
		/// <summary>
		/// 读取单个参数
		/// </summary>
		/// <param name="index">参数的索引。</param>
		/// <remarks>
		/// 具体的index请参见官方手册
		/// </remarks>
		public void ReadSingleParam(uint index)
		{
			byte[] data_index = BitConverter.GetBytes(index);
			byte[] date_parameter = { 0, 0, 0, 0 };
			//组合2个数组
			byte[] data1 = data_index.Concat(date_parameter).ToArray();
			CanSend(CmdMode.SINGLE_PARAM_READ, data1);
		}

		/// <summary>
		/// 使能电机
		/// </summary>
		public void EnableMotor()
		{
			CanSend(CmdMode.MOTOR_ENABLE, Array.Empty<byte>());
		}

		/// <summary>
		/// 停止电机
		/// </summary>
		public void DisableMotor()
		{
			CanSend(CmdMode.MOTOR_STOP, new byte[8]);
		}

		/// <summary>
		/// 设置机械零点
		/// </summary>
		public void SetMechanicalZero()
		{
			CanSend(CmdMode.SET_MECHANICAL_ZERO, new byte[] { 1 });
		}

		/// <summary>
		/// 运控模式
		/// </summary>
		/// <param name="torque"></param>
		/// <param name="target_angle"></param>
		/// <param name="target_velocity"></param>
		/// <param name="Kp"></param>
		/// <param name="Kd"></param>
		public void SendMotorControlCommand(float torque, float target_angle, float target_velocity, float Kp, float Kd)
		{
			//运控模式下发送电机控制指令。
			//参数:
			//torque: 扭矩。
			//target_angle: 目标角度。
			//target_velocity: 目标速度。
			//Kp: 比例增益。
			//Kd: 导数增益。

			//生成29位的仲裁ID的组成部分
			//uint cmd_mode = CmdModes.MOTOR_CONTROL;
			uint torque_mapped = Calculate.FToU(torque, -12.0, 12.0);
			uint data2 = torque_mapped;//data2为力矩值
									   // 计算仲裁ID，
			uint arbitrationId = (uint)CmdMode.MOTOR_CONTROL << 24 | data2 << 8 | _motorCANID;

			// 生成数据区1
			uint target_angle_mapped = Calculate.FToU(target_angle, -4 * Math.PI, 4 * Math.PI);//目标角度
			uint target_velocity_mapped = Calculate.FToU(target_velocity, -30.0F, 30.0F);//目标速度
			uint Kp_mapped = Calculate.FToU(Kp, 0.0F, 500.0F);//比例增益
			uint Kd_mapped = Calculate.FToU(Kd, 0.0F, 5.0F);//微分增益

			//组合为一个8个字节的data1
			byte[] data1 = new byte[8];
			Array.Copy(BitConverter.GetBytes(target_angle_mapped), 0, data1, 0, 2);
			Array.Copy(BitConverter.GetBytes(target_velocity_mapped), 0, data1, 2, 2);
			Array.Copy(BitConverter.GetBytes(Kp_mapped), 0, data1, 4, 2);
			Array.Copy(BitConverter.GetBytes(Kd_mapped), 0, data1, 6, 2);
			// 一条CAN消息结构
			PcanMessage canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data1.Length),
				Data = data1
			};
			// Write the CAN message
			PcanStatus writeStatus = Api.Write(_channel, canMessage);
			if (writeStatus != PcanStatus.OK)
			{
				Debug.WriteLine("Failed to send the message.");
			}
			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");
		}
	}
}
