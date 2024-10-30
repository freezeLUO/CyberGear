using CyberGear.Control.Params;
using CyberGear.Control.ReceiveMessageType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control
{
	public class Motor
	{
		/// <summary>
		/// id
		/// </summary>
		private readonly uint _id;

		/// <summary>
		/// id
		/// </summary>
		public uint Id => _id;

		private readonly CanBus _canBus;

		public Motor(uint id, CanBus canbus)
        {
			_id = id;
			_canBus = canbus;
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="param">参数</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> WriteParamAsync<T>(IParam<T> param, int timeoutMilliseconds)
			where T : struct, IComparable<T>
		{
			if (!_canBus._isRunning)
				return new NullMessageType();
			var limitParam = param as ILimitParam<T>;
			if (limitParam is not null)
				ValidateParam(limitParam);
			//发送CAN消息
			return await _canBus.SendAsync(this._id, CmdMode.SINGLE_PARAM_WRITE, param.ToArray(), timeoutMilliseconds);
		}

		/// <summary>
		/// 设置运行模式
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="runMode"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetRunModeAsync(RunMode runMode, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new RunModeParam(runMode), timeoutMilliseconds);

		/// <summary>
		/// 设置转速模式转速指令
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetIqRefAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new IqRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置转矩限制
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetLimitTorqueAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitTorqueParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Kp
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetCurKpParamAsync(float value = (float)0.125, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurKpParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Ki
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetCurKiParamAsync(float value = (float)0.0158, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurKiParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流滤波系数
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetCurFiltGainParamAsync(float value = (float)0.1, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurFiltGainParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式角度指令
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetLocRefParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LocRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式速度设置
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetLimitSpdParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitSpdParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置速度位置模式电流设置
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetLimitCurParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitCurParam(value), timeoutMilliseconds);

		[Obsolete]
		/// <summary>
		/// 读取单个参数
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="index">参数的索引</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		/// <remarks>
		/// 具体的index请参见官方手册
		/// </remarks>
		public async Task<IMessageType> ReadSingleParamAsync(uint index, int timeoutMilliseconds = 2000)
		{
			byte[] data_index = BitConverter.GetBytes(index);
			byte[] date_parameter = { 0, 0, 0, 0 };
			//组合2个数组
			byte[] data1 = data_index.Concat(date_parameter).ToArray();
			return await _canBus.SendAsync(this._id, CmdMode.SINGLE_PARAM_READ, data1, timeoutMilliseconds);
		}

		/// <summary>
		/// 使能电机
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> EnableAsync(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				return new NullMessageType(); ;
			return await _canBus.SendAsync(this._id, CmdMode.MOTOR_ENABLE, Array.Empty<byte>(), timeoutMilliseconds);
		}

		/// <summary>
		/// 停止电机
		/// </summary>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> DisableAsnyc(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				return new NullMessageType();
			return await _canBus.SendAsync(this._id, CmdMode.MOTOR_STOP, new byte[8], timeoutMilliseconds);
		}

		/// <summary>
		/// 设置机械零点
		/// </summary>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<IMessageType> SetMechanicalZeroAsync(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				return new NullMessageType();
			return await _canBus.SendAsync(this._id, CmdMode.SET_MECHANICAL_ZERO, new byte[] { 1 }, timeoutMilliseconds);
		}

		/// <summary>
		/// 检验限制类型
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="limitParam"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		internal static void ValidateParam<T>(ILimitParam<T> limitParam) 
			where T : struct, IComparable<T>
		{
			// 校验
			if (limitParam.Value.CompareTo(limitParam.MinValue) < 0
				|| limitParam.Value.CompareTo(limitParam.MaxValue) > 0)
				throw new ArgumentOutOfRangeException($"Value should between {limitParam.MinValue} and {limitParam.MaxValue}");
		}
	}
}
