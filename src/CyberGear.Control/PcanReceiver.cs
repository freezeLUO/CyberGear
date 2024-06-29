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
	public class PcanReceiver
	{
		private EventWaitHandle receiveEvent;
		private Thread? receiveThread;
		private bool isRunning;
		private PcanChannel channel;

		public PcanReceiver(PcanChannel channel)
		{
			this.channel = channel;
			receiveEvent = new EventWaitHandle(false, EventResetMode.AutoReset);
		}

		public bool Start()
		{

			// 根据操作系统类型配置接收事件
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
			{
				// 在Windows操作系统上，直接设置接收事件
				if (Api.SetValue(channel, PcanParameter.ReceiveEvent, (uint)receiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32()) != PcanStatus.OK)
				{
					Console.WriteLine($"在通道 {channel} 上配置接收事件时出错。");
					Api.Uninitialize(channel);
					return false;
				}
			}
			else
			{
				// 在非Windows操作系统上，获取接收事件句柄并进行设置
				uint eventHandle;
				if (Api.GetValue(channel, PcanParameter.ReceiveEvent, out eventHandle) != PcanStatus.OK)
				{
					Console.WriteLine($"在通道 {channel} 上获取接收事件时出错。");
					Api.Uninitialize(channel);
					return false;
				}

				receiveEvent.SafeWaitHandle.Close();
				receiveEvent.SafeWaitHandle = new Microsoft.Win32.SafeHandles.SafeWaitHandle(new IntPtr(eventHandle), false);
			}

			// 启动接收线程
			isRunning = true;
			receiveThread = new Thread(ReceiveThread);
			receiveThread.Start();

			Console.WriteLine($"已为通道 {channel} 配置接收事件。");
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
			Api.Uninitialize(channel);
			Console.WriteLine($"通道 {channel} 已关闭。");
		}

		private void ReceiveThread()
		{
			while (isRunning)
			{
				// 等待事件信号
				if (receiveEvent.WaitOne(50))
				{
					PcanMessage canMessage;
					ulong canTimestamp;

					// 读取并处理接收缓冲区中的所有CAN消息
					while (Api.Read(channel, out canMessage, out canTimestamp) == PcanStatus.OK)
					{
						// 处理接收到的CAN消息
						Console.WriteLine($"接收到的消息: ID=0x{canMessage.ID:X} 数据={BitConverter.ToString(canMessage.Data)}");
						Console.WriteLine($"接收到的消息的时间戳: {canTimestamp}");
						var result = Controller.ParseReceivedMsg(canMessage.Data, canMessage.ID);
						Console.WriteLine($"解析结果为：Motor CAN ID: {result.Item1}, Position: {result.Item2} rad, Velocity: {result.Item3} rad/s, Torque: {result.Item4} Nm");
					}

					// 重置事件
					receiveEvent.Reset();
				}
			}
		}
	}
}
