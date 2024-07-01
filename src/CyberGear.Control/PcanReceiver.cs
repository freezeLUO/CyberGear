using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peak.Can.Basic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CyberGear.Control
{
	public class PcanReceiver
	{
		private readonly ManualResetEvent _mre;
		private readonly EventWaitHandle _receiveEvent;
		private Thread? receiveThread;
		private bool isRunning;
		private readonly PcanChannel _channel;

		public PcanReceiver(PcanChannel channel, ManualResetEvent mre)
		{
			_channel = channel;
			_receiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
			_mre = mre;
		}

		public bool Start()
		{
			// Windows操作系统
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// 在Windows操作系统上，直接设置接收事件
				if (Api.SetValue(_channel, PcanParameter.ReceiveEvent, (uint)_receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32()) != PcanStatus.OK)
				{
					Debug.WriteLine($"在通道 {_channel} 上配置接收事件时出错。");
					Api.Uninitialize(_channel);
					return false;
				}
			}
			// 在非Windows操作系统上，获取接收事件句柄并进行设置
			else
			{
				uint eventHandle;
				if (Api.GetValue(_channel, PcanParameter.ReceiveEvent, out eventHandle) != PcanStatus.OK)
				{
					Debug.WriteLine($"在通道 {_channel} 上获取接收事件时出错。");
					Api.Uninitialize(_channel);
					return false;
				}

				_receiveEvent.SafeWaitHandle.Close();
				_receiveEvent.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(new IntPtr(eventHandle), false);
			}

			// 启动接收线程
			isRunning = true;
			receiveThread = new Thread(ReceiveThread);
			receiveThread.Start();

			Debug.WriteLine($"已为通道 {_channel} 配置接收事件。");
			return true;
		}

		public void Stop()
		{
			// 停止接收线程并进行清理
			isRunning = false;
			if (receiveThread != null && receiveThread.IsAlive)
			{
				receiveThread.Join();
			}
			Api.Uninitialize(_channel);
			Debug.WriteLine($"通道 {_channel} 已关闭。");
		}

		private void ReceiveThread()
		{
			while (isRunning)
			{
				// 读取并处理接收缓冲区中的所有CAN消息
				while (Api.Read(_channel, out var canMessage, out var canTimestamp) == PcanStatus.OK)
				{
					Debug.WriteLine($"{canTimestamp}: 消息: ID=0x{canMessage.ID:X} 数据={BitConverter.ToString(canMessage.Data)}");
					var result = Controller.ParseReceivedMsg(canMessage.Data, canMessage.ID);
					_mre.Set();
					Debug.WriteLine($"解析结果为：Motor CAN ID: {result.Item1}, Position: {result.Item2} rad, Velocity: {result.Item3} rad/s, Torque: {result.Item4} Nm");
				}
			}
		}
	}
}
