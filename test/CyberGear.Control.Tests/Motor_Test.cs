using CyberGear.Control.Params;
using FluentAssertions;
using System.Diagnostics;

namespace CyberGear.Control.Tests
{
	public class Motor_Test
	{
		/// <summary>
		/// 写入参数
		/// </summary>
		[Fact]
		public void ValidateParam_Greater_NOk()
		{
			Action action = () => Motor.ValidateParam(new SpdRefParam(40));
			action.Should().Throw<ArgumentOutOfRangeException>();
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		[Fact]
		public void ValidateParam_Less_NOk()
		{
			Action action = () => Motor.ValidateParam(new SpdRefParam(-40));
			action.Should().Throw<ArgumentOutOfRangeException>();
		}

		/// <summary>
		/// 尝试转化成 PcanChannel
		/// </summary>
		[Fact]
		public void TryParseToPcanChannel_Ok()
		{
			var acutal = CanBus.TryParseToPcanChannel(SlotType.Usb, 1, out var pcanChannel);
			acutal.Should().BeTrue();
			pcanChannel.Should().Be(Peak.Can.Basic.PcanChannel.Usb01);
		}
	}
}