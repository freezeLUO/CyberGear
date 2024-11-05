using CyberGear.Control.Params;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control.Tests
{
	public class Param_Test
	{
		private byte[] OldFloatValueToArray(uint index, float value)
		{
			byte[] data_index = BitConverter.GetBytes(index);
			byte[] date_parameter = BitConverter.GetBytes(value);
			//组合2个数组   
			byte[] data = data_index.Concat(date_parameter).ToArray();
			return data; 
		}

		private byte[] OldByteValueToArray(uint index, byte value)
		{
			// 创建一个只包含这个byte值的数组，并补充三个字节的0
			byte[] bs = new byte[] { value };
			bs = bs.Concat(Enumerable.Repeat((byte)0, 3)).ToArray();
			byte[] data_index = BitConverter.GetBytes(index);
			// 组合index数组和处理后的value数组
			byte[] data = data_index.Concat(bs).ToArray();
			return data;
		}

		[Fact]
		public void LimitSpdParam_ToArray_Ok()
		{
			var limitSpdParam = new LimitSpdParam(3.1F);
			var actual = limitSpdParam.ToArray();
			var expected = OldFloatValueToArray(0X7017, 3.1F);
			actual.Should().BeEquivalentTo(expected);
		}

		[Fact]
		public void RunModeParam_ToArray_Ok()
		{
			var runModeParam = new RunModeParam(RunMode.POSITION_MODE);
			var actual = runModeParam.ToArray();
			var expected = OldByteValueToArray(0X7005, 1);
			actual.Should().BeEquivalentTo(expected);
		}
	}
}
