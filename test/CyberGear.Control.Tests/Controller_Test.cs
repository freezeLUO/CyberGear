using CyberGear.Control.Params;
using CyberGear.Control.Protocols;
using FluentAssertions;
using Peak.Can.Basic;

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
		public void WriteSingleParam_Ok()
		{
			var motor = new Controller(0, 127, PcanChannel.Usb01);
			motor.WriteParam(new SpdRefParam(40));
		}
	}
}