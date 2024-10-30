using Peak.Can.Basic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using CyberGear.Control.Protocols;
using CyberGear.Control.ReceiveMessageType;

namespace CyberGear.Control
{
	/// <summary>
	/// 用于管理CyberGear控制系统中的CAN总线通信
	/// </summary>
	/// <remarks>
	/// 该类提供了一系列方法，包括发送和接收CAN消息，解析接收到的消息，
	/// 以及处理与主控制器和电机相关的逻辑。
	/// </remarks>
	public class CanBus : IDisposable
	{
		/// <summary>
		/// CAN 通道
		/// </summary>
		private readonly PcanChannel _channel;

		/// <summary>
		/// CAN 通道
		/// </summary>
		public PcanChannel PcanChanne => _channel;

		/// <summary>
		/// 插口类型
		/// </summary>
		public readonly SlotType SlotType;

		/// <summary>
		/// 插槽序号
		/// </summary>
		public readonly int SlotIndex;

		/// <summary>
		/// 波特率
		/// </summary>
		public readonly Bitrate Bitrate;

		/// <summary>
		/// 电机
		/// </summary>
		private readonly Motor[] _motors;
		/// <summary>
		/// 电机
		/// </summary>
		public Motor[] Motors => _motors;

		private readonly ManualResetEvent _mre = new ManualResetEvent(true);
		private readonly EventWaitHandle _receiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		private Thread? _receiveThread;
		/// <summary>
		/// 正在运行
		/// </summary>
		internal volatile bool _isRunning;
		private bool _disposed;
		private IMessageType _currentMessageData;
		public readonly uint MasterId;

		private readonly SemaphoreSlim _semaphoreForSend = new SemaphoreSlim(1, 1);

		/// <summary>
		/// 生成建造者
		/// </summary>
		/// <param name="slotType">插口类型</param>
		/// <param name="slotIndex">插槽序号</param>
		/// <returns></returns>
		public static CanBusBuilder CreateBuilder(SlotType slotType, int slotIndex)
			=> new CanBusBuilder(slotType, slotIndex);

		private CanBus() { }

		internal CanBus(CanBusBuilder builder)
		{
			if (!TryParseToPcanChannel(builder.SlotType, builder.SlotIndex, out var pcanChannel))
				throw new ArgumentOutOfRangeException($"{nameof(builder.SlotIndex)} must between 1 and 16");
			_channel = pcanChannel!.Value;
			SlotType = builder.SlotType;
			MasterId = builder.CanbusOption.MasterId;
			SlotIndex = builder.SlotIndex;
			Bitrate = builder.CanbusOption.Bitrate;
			_motors = builder.CanbusOption.MotorIds.Select(x => new Motor(x, this)).ToArray();

			var bitrateValue = Enum.Parse<Peak.Can.Basic.Bitrate>(Bitrate.ToString());
			PcanStatus result = Api.Initialize(_channel, bitrateValue);
			if (result != PcanStatus.OK)
			{
				Api.GetErrorText(result, out var errorText);
				Debug.WriteLine($"初始化通道 {_channel} 错误: {errorText}");
				return false;
			}

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
			_receiveThread = new Thread(ReceiveThread);
			_receiveThread.Start();
			_isRunning = true;
		}

		/// <summary>
		/// 尝试转化成 <typeparamref name="PcanChannel"/>
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
				if (Enum.TryParse(str, true, out PcanChannel value))
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
					// 解析通讯类型, bit24-28
					byte com_type = (byte)(canMessage.ID >> 24 & 0xFF);
					// 解析电机CAN ID, bit8-15
					byte motor_can_id = (byte)(canMessage.ID >> 8 & 0xFF);
					// 解析主控制器CAN ID, bit0-7
					byte master_can_id = (byte)(canMessage.ID & 0xFF);
					//如果是电机反馈帧（2）
					if (com_type == 2)
					{
						// 解析位置、速度、力矩
						var rd = ResponseData.Parse(canMessage.Data);
						_currentMessageData = new MessageType<ResponseData>(canTimestamp, motor_can_id, master_can_id, com_type, rd);
						// 添加到队列
						//AddToFeedbackQueue(canTimestamp, canMessage.Data, rd);
						Debug.WriteLine($"Motor CAN ID: {motor_can_id}, Main CAN ID: {master_can_id}, pos: {rd.Angle:.2f} rad, vel: {rd.AngularVelocity:.2f} rad/s, Torque: {rd.Torque:.2f} N·m");
					}
					//如果是单个参数读取应答帧（17）
					else if (com_type == 17)
					{
						//byte[] data = canMessage.Data;                       
						var rd = SingleResponseData.Parse(canMessage.Data);
						_currentMessageData = new MessageType<SingleResponseData>(canTimestamp, motor_can_id, master_can_id, com_type, rd);
						Debug.WriteLine($"参数索引: {rd.Index}, 参数值: {BitConverter.ToString(rd.value_bytes)}");
					}
					//如果是故障反馈
					else if (com_type == 21)
					{
						byte[] data = canMessage.Data;
						//_currentMessageData = new MessageType<byte[]>(canTimestamp, motor_can_id, master_can_id, com_type, canMessage.Data);
						//Debug.WriteLine($"Motor CAN ID: {motor_can_id}, Main CAN ID: {master_can_id}, 故障反馈: {BitConverter.ToString(data)}");
						string byteDataString = BitConverter.ToString(data);
						throw new InvalidOperationException($"Motor {motor_can_id} Failure!!!The fault code is{byteDataString},Please refer to the manual");
					}
					else
					{
						Debug.WriteLine("未知通讯类型");
					}
					_mre.Set();
				}
			}
		}

		/// <summary>
		/// 发送CAN消息
		/// </summary>
		/// <param name="motorId">电机id</param>
		/// <param name="cmdMode">仲裁ID通信类型</param>
		/// <param name="data">CAN2.0数据区1</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		internal async Task<IMessageType> SendAsync(uint motorId, CmdMode cmdMode, byte[] data, int timeoutMilliseconds)
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeoutMilliseconds);
			try
			{
				await _semaphoreForSend.WaitAsync(cts.Token);

				// 计算仲裁ID
				uint arbitrationId = CalculateArbitrationId(cmdMode, MasterId, motorId);
				// 一条CAN消息结构
				var canMessage = new PcanMessage
				{
					ID = arbitrationId,
					MsgType = MessageType.Extended,
					DLC = Convert.ToByte(data.Length),
					Data = data
				};
				// Write the CAN message
				var writeStatus = Api.Write(_channel, canMessage);
				if (writeStatus != PcanStatus.OK)
				{
					Debug.WriteLine("Failed to send the message.");
				}

				// Output details of the sent message
				Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data)}");
				// 返回接收到的数据
				return _currentMessageData;
			}
			catch (OperationCanceledException)
			{
				throw new TimeoutException("reply timeout");
			}
			finally
			{
				_semaphoreForSend.Release();
				cts.Dispose();
			}
		}

		/// <summary>
		/// 运控模式
		/// </summary>
		/// <param name="motorId">电机id</param>
		/// <param name="torque">扭矩</param>
		/// <param name="target_angle">目标角度</param>
		/// <param name="target_velocity">目标速度</param>
		/// <param name="Kp">比例增益</param>
		/// <param name="Kd">微分增益</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public IMessageType SendMotorControlCommand(
			uint motorId,
			float torque,
			float target_angle,
			float target_velocity,
			float Kp,
			float Kd,
			int timeoutMilliseconds = 2000)
		{
			//生成29位的仲裁ID的组成部分
			//uint cmd_mode = CmdModes.MOTOR_CONTROL;
			uint torque_mapped = Calculate.FToU(torque, -12.0, 12.0);
			//data2为力矩值
			uint data2 = torque_mapped;
			// 计算仲裁ID，
			uint arbitrationId = (uint)CmdMode.MOTOR_CONTROL << 24 | data2 << 8 | motorId;

			// 生成数据区1
			//目标角度
			uint target_angle_mapped = Calculate.FToU(target_angle, -4 * Math.PI, 4 * Math.PI);
			//目标速度
			uint target_velocity_mapped = Calculate.FToU(target_velocity, -30.0F, 30.0F);
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
			//Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");
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
			Debug.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");
			// 返回接收到的数据
			return _currentMessageData;
		}

		/// <summary>
		/// 计算仲裁ID
		/// </summary>
		/// <param name="cmdMode"></param>
		/// <param name="masterCanId"></param>
		/// <param name="motorCanId"></param>
		/// <returns></returns>
		internal static uint CalculateArbitrationId(CmdMode cmdMode, uint masterCanId, uint motorCanId) =>
			(uint)cmdMode << 24 | masterCanId << 8 | motorCanId;

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
