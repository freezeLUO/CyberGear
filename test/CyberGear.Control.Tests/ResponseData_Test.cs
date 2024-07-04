using CyberGear.Control.Protocols;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Tests
{
	public class ResponseData_Test
	{
		[Fact]
		public void Parse_OK()
		{
			var data = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
			var res = ResponseData.Parse(data);
			res.Angle.Should().Be(Calculate.UToF((data[0] << 8) + data[1], -4, 4));
			res.AngularVelocity.Should().Be(Calculate.UToF((data[2] << 8) + data[3], -30, 30));
		}
	}
}
