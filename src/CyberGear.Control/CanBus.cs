using Peak.Can.Basic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Peak.Can.Basic.BackwardCompatibility;

namespace CyberGear.Control
{
	/// <summary>
	/// 用于管理CyberGear控制系统中的CAN总线通信
	/// </summary>
	/// <remarks>
	/// 该类提供了一系列方法，包括发送和接收CAN消息，解析接收到的消息，
	/// 以及处理与主控制器和电机相关的逻辑。
	/// </remarks>
	public sealed class CanBus : IDisposable
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
		private readonly Motor[] _motors = null!;
		/// <summary>
		/// 电机
		/// </summary>
		public Motor[] Motors => _motors;

		private readonly EventWaitHandle _receiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		private Thread? _receiveThread;
		/// <summary>
		/// 正在运行
		/// </summary>
		internal volatile bool _isRunning;
		private bool _disposed;
		public readonly uint MasterCanId;

		private readonly SemaphoreSlim _semaphoreForSend = new SemaphoreSlim(1, 1);
		private TaskCompletionSource<PcanMessage>? _completionSource;

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
			MasterCanId = builder.CanbusOption.MasterId;
			SlotIndex = builder.SlotIndex;
			Bitrate = builder.CanbusOption.Bitrate;
			_motors = builder.CanbusOption.MotorCanIds.Select(x => new Motor(x, this)).ToArray();

			var bitrateValue = Enum.Parse<Peak.Can.Basic.Bitrate>(Bitrate.ToString());
			PcanStatus result = Api.Initialize(_channel, bitrateValue);
			if (result != PcanStatus.OK)
			{
				Api.GetErrorText(result, out var errorText);
				throw new CanBusException($"init can error: {errorText}");
			}

			// Windows操作系统
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				var pcanStatus = Api.SetValue(_channel, PcanParameter.ReceiveEvent, (uint)_receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32());
				// 在Windows操作系统上，直接设置接收事件
				if (pcanStatus != PcanStatus.OK)
				{
					Api.Uninitialize(_channel);
					throw new CanBusException($"init can receiver error: {pcanStatus}");
				}
			}
			// 在非Windows操作系统上，获取接收事件句柄并进行设置
			else
			{
				var pcanStatus = Api.GetValue(_channel, PcanParameter.ReceiveEvent, out uint eventHandle);
				if (pcanStatus != PcanStatus.OK)
				{
					Api.Uninitialize(_channel);
					throw new CanBusException($"init can receiver error: {pcanStatus}");
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
				if (Api.Read(_channel, out var canMessage, out var canTimestamp) == PcanStatus.OK)
				{
					Debug.WriteLine($"Timestamp: {canTimestamp}, 消息: ID=0x{canMessage.ID:X}, 数据={BitConverter.ToString(canMessage.Data)}");
					_completionSource?.SetResult(canMessage);
				}
			}
		}

		/// <summary>
		/// 发送信息
		/// </summary>
		/// <param name="pcanMessage">信息</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		/// <returns></returns>
		/// <exception cref="CanBusException"></exception>
		/// <exception cref="TimeoutException"></exception>
		internal async Task<PcanMessage> SendAsync(PcanMessage pcanMessage, int timeoutMilliseconds)
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeoutMilliseconds);
			try
			{
				await _semaphoreForSend.WaitAsync(cts.Token);

				_completionSource = new TaskCompletionSource<PcanMessage>();
				var pcanStatus = Api.Write(_channel, pcanMessage);
				if (pcanStatus != PcanStatus.OK)
					throw new CanBusException($"failed to send message: {pcanStatus}");
				return await _completionSource.Task;
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

		void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					StopReceive();
				}

				_receiveEvent.Dispose();
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
