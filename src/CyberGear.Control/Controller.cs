using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peak.Can.Basic;
using System.Diagnostics;
using System.Threading.Tasks;

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
		uint MasterCANID = 0;//主控制器CANID
		uint MotorCANID = 0;//电机CANID
		private PcanChannel channel;//CAN通道

		private static double P_MIN = -4 * Math.PI;//位置最小值
		private static double P_MAX = 4 * Math.PI;//位置最大值
		private static double V_MIN = -30.0;//速度最小值
		private static double V_MAX = 30.0;//速度最大值
		private static double T_MIN = -12.0;//力矩最小值
		private static double T_MAX = 12.0;//力矩最大值
		private static double KP_MIN = 0.0;//比例系数最小值
		private static double KP_MAX = 500.0;//比例系数最大值
		private static double KD_MIN = 0.0;//微分系数最小值
		private static double KD_MAX = 5.0;//微分系数最大值
										   //构造函数
		public Controller(uint masterCANID, uint motorCANID, PcanChannel channel)
		{
			// 初始化控制器
			MasterCANID = masterCANID;
			MotorCANID = motorCANID;
			this.channel = channel;
		}

		//仲裁ID的通讯类型枚举
		private class CmdModes
		{
			public const uint GET_DEVICE_ID = 0;//获取设备ID
			public const uint MOTOR_CONTROL = 1;//电机运控模式
			public const uint MOTOR_FEEDBACK = 2;//电机反馈
			public const uint MOTOR_ENABLE = 3;//电机使能
			public const uint MOTOR_STOP = 4;//电机停止
			public const uint SET_MECHANICAL_ZERO = 6;//设置机械零点
			public const uint SET_MOTOR_CAN_ID = 7;//设置电机CAN ID
			public const uint PARAM_TABLE_WRITE = 8;//参数表写入
			public const uint SINGLE_PARAM_READ = 17;//单个参数读取
			public const uint SINGLE_PARAM_WRITE = 18;//单个参数写入
			public const uint FAULT_FEEDBACK = 21;//故障反馈
		}
		// 枚举运行模式
		public enum RunModes
		{
			CONTROL_MODE = 0,    // 运控模式
			POSITION_MODE = 1,   // 位置模式
			SPEED_MODE = 2,      // 速度模式
			CURRENT_MODE = 3     // 电流模式
		}

		// 参数列表
		public static class ParameterList
		{
			public const uint RunMode = 0x7005; // 运控模式：0-位置模式；1-速度模式；2-电流模式
			public const uint IqRef = 0x7006; // 电流模式Iq指令（float类型，单位A）
			public const uint SpdRef = 0x700A; // 转速模式转速指令（float类型，单位rad/s）
			public const uint ImitTorque = 0x700B; // 转矩限制（float类型，单位Nm）
			public const uint CurKp = 0x7010; // 电流的Kp（float类型，默认值0.125）
			public const uint CurKi = 0x7011; // 电流的Ki（float类型，默认值0.0158）
			public const uint CurFiltGain = 0x7014; // 电流滤波系数filt_gain（float类型，默认值0.1）
			public const uint LocRef = 0x7016; // 位置模式角度指令（float类型，单位rad）
			public const uint LimitSpd = 0x7017; // 位置模式速度设置（float类型，单位rad/s）
			public const uint LimitCur = 0x7018; // 速度位置模式电流设置（float类型，单位A）
		}

		public Tuple<byte[], uint> SendReceiveCanMessage(uint cmdMode, byte[] data1)//data2 is MAIN_CAN_ID
		{
			// 计算仲裁ID
			uint arbitrationId = cmdMode << 24 | MasterCANID << 8 | MotorCANID;

			// 一条CAN消息结构
			PcanMessage canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data1.Length),
				Data = data1
			};

			// Write the CAN message
			PcanStatus writeStatus = Api.Write(channel, canMessage);
			if (writeStatus != PcanStatus.OK)
			{
				Debug.WriteLine("Failed to send the message.");
				return Tuple.Create<byte[], uint>(new byte[0], 0);
			}

			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");
			Thread.Sleep(50);  // Give the driver some time to send the messages...
			PcanMessage receivedMsg;
			ulong timestamp;
			PcanStatus readStatus = Api.Read(channel, out receivedMsg, out timestamp);
			// Check if received a message
			if (readStatus == PcanStatus.OK)
			{
				byte[]? DB = receivedMsg.Data;
				byte[] bytes = DB;

				return Tuple.Create(bytes, receivedMsg.ID);
			}
			else
			{
				Debug.WriteLine("Failed to receive the message or message was not received within the timeout period.");
				return Tuple.Create<byte[], uint>(new byte[0], 0);
			}

		}

		/// <summary>
		/// 发送CAN消息。
		/// </summary>
		/// <param name="cmdMode">仲裁ID通信类型</param>
		/// <param name="data1">CAN2.0数据区1</param>
		public void SendCanMessage(uint cmdMode, byte[] data1)
		{
			// 计算仲裁ID
			uint arbitrationId = cmdMode << 24 | MasterCANID << 8 | MotorCANID;
			// 一条CAN消息结构
			PcanMessage canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data1.Length),
				Data = data1
			};
			// Write the CAN message
			PcanStatus writeStatus = Api.Write(channel, canMessage);
			if (writeStatus != PcanStatus.OK)
			{
				Debug.WriteLine("Failed to send the message.");
			}
			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");

		}

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
			byte[] data1 = data_index.Concat(date_parameter).ToArray();

			//发送CAN消息
			SendCanMessage(CmdModes.SINGLE_PARAM_WRITE, data1);
		}
		public void WriteSingleParam(uint index, byte byteValue)
		{
			// 创建一个只包含这个byte值的数组，并补充三个字节的0
			byte[] bs = new byte[] { byteValue };
			bs = bs.Concat(Enumerable.Repeat((byte)0, 3)).ToArray();
			byte[] data_index = BitConverter.GetBytes(index);
			// 组合index数组和处理后的value数组
			byte[] data1 = data_index.Concat(bs).ToArray();

			// 发送CAN消息
			SendCanMessage(CmdModes.SINGLE_PARAM_WRITE, data1);
		}

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
			SendCanMessage(CmdModes.SINGLE_PARAM_READ, data1);
		}

		//使能电机
		public void EnableMotor()
		{
			byte[] data1 = { };
			SendCanMessage(CmdModes.MOTOR_ENABLE, data1);
		}
		//停止电机
		public void DisableMotor()
		{
			byte[] data1 = { 0, 0, 0, 0, 0, 0, 0, 0 };//置零
			SendCanMessage(CmdModes.MOTOR_STOP, data1);
		}
		//设置机械零点
		public void SetMechanicalZero()
		{
			byte[] data1 = { 1 };//Byte[0]=1
			SendCanMessage(CmdModes.SET_MECHANICAL_ZERO, data1);
		}

		//运控模式
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
			uint arbitrationId = CmdModes.MOTOR_CONTROL << 24 | data2 << 8 | MotorCANID;

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
			PcanStatus writeStatus = Api.Write(channel, canMessage);
			if (writeStatus != PcanStatus.OK)
			{
				Debug.WriteLine("Failed to send the message.");
			}
			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");

		}

	}
}
