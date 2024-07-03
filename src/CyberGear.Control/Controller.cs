using Peak.Can.Basic;
using System.Diagnostics;
using CyberGear.Control.Params;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CyberGear.Control
{
	/// <summary>
	/// 控制器类，用于管理CyberGear控制系统中的CAN总线通信。
	/// </summary>
	/// <remarks>
	/// 该类提供了一系列方法，包括发送和接收CAN消息，解析接收到的消息，
	/// 以及处理与主控制器和电机相关的逻辑。
	/// </remarks>
	public class Controller : IDisposable
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

		private readonly ManualResetEvent _mre;

		/// <summary>
		/// 位置最小值
		/// </summary>
		private const double P_MIN = -4 * Math.PI;
		/// <summary>
		/// 位置最大值
		/// </summary>
		private const double P_MAX = 4 * Math.PI;
		/// <summary>
		/// 速度最小值
		/// </summary>
		private const double V_MIN = -30.0;
		/// <summary>
		/// 速度最大值
		/// </summary>
		private const double V_MAX = 30.0;
		/// <summary>
		/// 力矩最小值
		/// </summary>
		private const double T_MIN = -12.0;
		/// <summary>
		/// 力矩最大值
		/// </summary>
		private const double T_MAX = 12.0;


		private readonly EventWaitHandle _receiveEvent;
		private Thread? _receiveThread;
		private bool _isRunning;
		private bool _disposed;


		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="masterCANID"></param>
		/// <param name="motorCANID">电机 canid</param>
		/// <param name="channel">通道类型</param>
		public Controller(SlotType slotType, int slotIndex, uint masterCANID, uint motorCANID)
		{
			if (!TryParseToPcanChannel(slotType, slotIndex, out var pcanChannel))
				throw new ArgumentOutOfRangeException("slotIndex must between 1 and 16");
			_channel = pcanChannel.Value;
			_masterCANID = masterCANID;
			_motorCANID = motorCANID;
			_mre = new ManualResetEvent(true);
			_receiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset);

			// Windows操作系统
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// 在Windows操作系统上，直接设置接收事件
				if (Api.SetValue(_channel, PcanParameter.ReceiveEvent, (uint)_receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32()) != PcanStatus.OK)
				{
					Debug.WriteLine($"在通道 {_channel} 上配置接收事件时出错。");
					Api.Uninitialize(_channel);
					throw new InvalidOperationException($"channel {_channel} initialize failed");
				}
			}
			// 在非Windows操作系统上，获取接收事件句柄并进行设置
			else
			{
				if (Api.GetValue(_channel, PcanParameter.ReceiveEvent, out uint eventHandle) != PcanStatus.OK)
				{
					Debug.WriteLine($"在通道 {_channel} 上获取接收事件时出错。");
					Api.Uninitialize(_channel);
					throw new InvalidOperationException($"channel {_channel} initialize failed");
				}

				_receiveEvent.SafeWaitHandle.Close();
				_receiveEvent.SafeWaitHandle = new SafeWaitHandle(new IntPtr(eventHandle), false);
			}
		}

		/// <summary>
		/// 尝试转化成 PcanChannel
		/// </summary>
		/// <param name="slotType"></param>
		/// <param name="slotIndex"></param>
		/// <param name="pcanChannel"></param>
		/// <returns></returns>
		internal static bool TryParseToPcanChannel(SlotType slotType, int slotIndex, out PcanChannel? pcanChannel)
		{
			if (slotIndex < 1 || slotIndex > 16)
			{
				pcanChannel = null;
				return false;
			}
			else
			{
				var str = slotType.ToString() + slotIndex.ToString("D2");
				if (Enum.TryParse<PcanChannel>(str, true, out PcanChannel value))
				{
					pcanChannel = value;
					return true;
				}
				else
				{
					pcanChannel = null;
					return false;
				}
			}
		}

		/// <summary>
		/// 初始化
		/// </summary>
		public bool Init(Bitrate bitrate)
		{
			if (_isRunning)
				return false;
			var bitrateValue = Enum.Parse<Peak.Can.Basic.Bitrate>(bitrate.ToString());
			// 硬件以1000k bit/s初始化
			PcanStatus result = Api.Initialize(_channel, bitrateValue);
			if (result != PcanStatus.OK)
			{
				Api.GetErrorText(result, out var errorText);
				Console.WriteLine(errorText);
				return false;
			}
			_receiveThread = new Thread(ReceiveThread);
			_receiveThread.Start();
			_isRunning = true;
			return true;
		}

		/// <summary>
		/// 停止
		/// </summary>
		public void Stop()
			=> StopReceive();

		/// <summary>
		/// 停止接受数据
		/// </summary>
		private void StopReceive()
		{
			if (!_isRunning)
				return;
			// 停止接收线程并进行清理
			_isRunning = false;
			_receiveEvent.Set();
			_receiveThread?.Join();
			var result = Api.Uninitialize(_channel);
			if (result != PcanStatus.OK)
			{
				Api.GetErrorText(result, out var errorText);
				Debug.WriteLine($"通道 {_channel} 硬件关闭失败: {errorText}");
			}
		}

		/// <summary>
		/// 接收数据
		/// </summary>
		private void ReceiveThread()
		{
			while (_isRunning)
			{
				// 读取并处理接收缓冲区中的所有CAN消息
				while (Api.Read(_channel, out var canMessage, out var canTimestamp) == PcanStatus.OK)
				{
					Debug.WriteLine($"Timestamp: {canTimestamp}, 消息: ID=0x{canMessage.ID:X}, 数据={BitConverter.ToString(canMessage.Data)}");
					var result = Controller.ParseReceivedMsg(canMessage.Data, canMessage.ID);
					_mre.Set();
					Debug.WriteLine($"解析结果为：Motor CAN ID: {result.Item1}, Position: {result.Item2} rad, Velocity: {result.Item3} rad/s, Torque: {result.Item4} Nm");
				}
			}
		}

		/// <summary>
		/// 发送CAN消息。
		/// </summary>
		/// <param name="cmdMode">仲裁ID通信类型</param>
		/// <param name="data">CAN2.0数据区1</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		internal void CanSend(CmdMode cmdMode, byte[] data, int timeoutMilliseconds)
		{
			// 计算仲裁ID
			uint arbitrationId = GetArbitrationId(cmdMode, _masterCANID, _motorCANID);
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

			bool isReplyOK = false;
			var t = new Thread(_ =>
			{
				Thread.Sleep(timeoutMilliseconds);
				// 已经完成后, 不要干涉后续的 mre
				if (!isReplyOK)
					_mre.Set();
			})
			{ IsBackground = true };
			t.Start();

			_mre.Reset();
			_mre.WaitOne();

			// 等待线程正在运行, 没有超时
			if (t.ThreadState == (System.Threading.ThreadState.WaitSleepJoin | System.Threading.ThreadState.Background))
			{
				isReplyOK = true;
			}
			// 等待线程结束, 已经超时
			else
			{
				throw new TimeoutException("reply timeout");
			}

			// Output details of the sent message
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data)}");
		}

		/// <summary>
		/// 计算仲裁ID
		/// </summary>
		/// <param name="cmdMode"></param>
		/// <returns></returns>
		internal static uint GetArbitrationId(CmdMode cmdMode, uint masterCANID, uint motorCANID) =>
			(uint)cmdMode << 24 | masterCANID << 8 | motorCANID;

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
		/// 检验限制类型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="limitParam"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		internal static void ValidateParam<T>(ILimitParam<T> limitParam) where T : struct, IComparable<T>
		{
			// 校验
			if (limitParam.Value.CompareTo(limitParam.MinValue) < 0
				|| limitParam.Value.CompareTo(limitParam.MaxValue) > 0)
				throw new ArgumentOutOfRangeException($"Value should between {limitParam.MinValue} and {limitParam.MaxValue}");
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="param"></param>
		public void WriteParam<T>(IParam<T> param, int timeoutMilliseconds) where T : struct, IComparable<T>
		{
			if (!_isRunning)
				return;
			var limitParam = param as ILimitParam<T>;
			if (limitParam is not null)
				ValidateParam(limitParam);
			//发送CAN消息
			CanSend(CmdMode.SINGLE_PARAM_WRITE, param.ToArray(), timeoutMilliseconds);
		}

		/// <summary>
		/// 设置运行模式
		/// </summary>
		/// <param name="runMode"></param>
		public void SetRunMode(RunMode runMode, int timeoutMilliseconds = 2000)
			=> WriteParam(new RunModeParam(runMode), timeoutMilliseconds);

		/// <summary>
		/// 设置转速模式转速指令
		/// </summary>
		/// <param name="value"></param>
		public void SetIqRef(float value, int timeoutMilliseconds = 2000)
			=> WriteParam(new IqRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置转矩限制
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitTorque(float value, int timeoutMilliseconds = 2000)
			=> WriteParam(new LimitTorqueParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Kp
		/// </summary>
		/// <param name="value"></param>
		public void SetCurKpParam(float value = (float)0.125, int timeoutMilliseconds = 2000)
			=> WriteParam(new CurKpParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Ki
		/// </summary>
		/// <param name="value"></param>
		public void SetCurKiParam(float value = (float)0.0158, int timeoutMilliseconds = 2000)
			=> WriteParam(new CurKiParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流滤波系数
		/// </summary>
		/// <param name="value"></param>
		public void SetCurFiltGainParam(float value = (float)0.1, int timeoutMilliseconds = 2000)
			=> WriteParam(new CurFiltGainParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式角度指令
		/// </summary>
		/// <param name="value"></param>
		public void SetLocRefParam(float value, int timeoutMilliseconds = 2000)
			=> WriteParam(new LocRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式速度设置
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitSpdParam(float value, int timeoutMilliseconds = 2000)
			=> WriteParam(new LimitSpdParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置速度位置模式电流设置
		/// </summary>
		/// <param name="value"></param>
		public void SetLimitCurParam(float value, int timeoutMilliseconds = 2000)
			=> WriteParam(new LimitCurParam(value), timeoutMilliseconds);

		[Obsolete]
		/// <summary>
		/// 读取单个参数
		/// </summary>
		/// <param name="index">参数的索引。</param>
		/// <remarks>
		/// 具体的index请参见官方手册
		/// </remarks>
		public void ReadSingleParam(uint index, int timeoutMilliseconds = 2000)
		{
			byte[] data_index = BitConverter.GetBytes(index);
			byte[] date_parameter = { 0, 0, 0, 0 };
			//组合2个数组
			byte[] data1 = data_index.Concat(date_parameter).ToArray();
			CanSend(CmdMode.SINGLE_PARAM_READ, data1, timeoutMilliseconds);
		}

		/// <summary>
		/// 使能电机
		/// </summary>
		public void EnableMotor(int timeoutMilliseconds = 2000)
		{
			if (!_isRunning)
				return;
			CanSend(CmdMode.MOTOR_ENABLE, Array.Empty<byte>(), timeoutMilliseconds);
		}

		/// <summary>
		/// 停止电机
		/// </summary>
		public void DisableMotor(int timeoutMilliseconds = 2000)
		{
			if (!_isRunning)
				return;
			CanSend(CmdMode.MOTOR_STOP, new byte[8], timeoutMilliseconds);
		}

		/// <summary>
		/// 设置机械零点
		/// </summary>
		public void SetMechanicalZero(int timeoutMilliseconds = 2000)
		{
			if (!_isRunning)
				return;
			CanSend(CmdMode.SET_MECHANICAL_ZERO, new byte[] { 1 }, timeoutMilliseconds);
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

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					StopReceive();
				}

				_receiveEvent.Dispose();
				_mre.Dispose();
				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
