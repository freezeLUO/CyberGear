using CyberGear.Control.Params;
using CyberGear.Control.Protocols;
using FluentAssertions;
using Peak.Can.Basic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace CyberGear.Control.Tests
{
	public class Controller_Test
	{
		/// <summary>
		/// 计算仲裁域
		/// </summary>
		[Fact]
		public void GetArbitrationId_Ok()
		{
			var motor = new Controller(0, 127, PcanChannel.Usb01);
			var actual = motor.GetArbitrationId(CmdMode.SET_MECHANICAL_ZERO);
			actual.Should().Be(0x0600007f);
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		[Fact]
		public void WriteSingleParam_Greater_NOk()
		{
			var motor = new Controller(0, 127, PcanChannel.Usb01);
			Action action = () => motor.WriteParam(new SpdRefParam(40), 2000);
			action.Should().Throw<ArgumentOutOfRangeException>();
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		[Fact]
		public void WriteSingleParam_Less_NOk()
		{
			var motor = new Controller(0, 127, PcanChannel.Usb01);
			Action action = () => motor.WriteParam(new SpdRefParam(-40), 2000);
			action.Should().Throw<ArgumentOutOfRangeException>();
		}


		/// <summary>
		/// 超时
		/// </summary>
		[Fact]
		public void Timeout()
		{
			const int timeoutMilliseconds = 2000;
			var _mre = new ManualResetEvent(false);
			Action action = () =>
			{
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
				if (t.ThreadState == System.Threading.ThreadState.Running)
				{
					isReplyOK = true;
				}
				// 等待线程结束, 已经超时
				else
				{
					throw new TimeoutException("reply timeout");
				}
			};
			action.Should().Throw<TimeoutException>();
		}


		/// <summary>
		/// 没有超时
		/// </summary>
		[Fact]
		public void NotTimeout()
		{
			const int timeoutMilliseconds = 2000;
			var _mre = new ManualResetEvent(false);
			var t1 = new Thread(_ => 
			{
				Thread.Sleep(1000);
				_mre.Set();
				Debug.WriteLine("ReplyOK");
			})
			{ IsBackground = true };
			t1.Start();

			Action action = () =>
			{
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
			};
			action.Should().NotThrow<TimeoutException>();
		}
	}
}