using CyberGear.Control.Params;
using Peak.Can.Basic;
using System.Buffers.Binary;


namespace CyberGear.Control
{
	public sealed class Motor
	{
		/// <summary>
		/// can id
		/// </summary>
		private readonly uint _canId;

		/// <summary>
		/// can id
		/// </summary>
		public uint CanId => _canId;

		private readonly CanBus _canBus;

		internal Motor(uint id, CanBus canbus)
		{
			_canId = id;
			_canBus = canbus;
		}

		/// <summary>
		/// 写入参数
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="param">参数</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> WriteParamAsync<T>(IParam<T> param, int timeoutMilliseconds)
			where T : struct, IComparable<T>
		{
			if (!_canBus._isRunning)
				throw new InvalidOperationException("canbus is not started");
			var limitParam = param as ILimitParam<T>;
			if (limitParam is not null)
				ValidateParam(limitParam);
			// 计算仲裁ID
			uint arbitrationId = (uint)CmdMode.SINGLE_PARAM_WRITE << 24 | _canBus.MasterCanId << 8 | this._canId;
			var data = param.ToArray();
			var canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data.Length),
				Data = data
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return MotorFeedback.Parse(reply);
		}

		/// <summary>
		/// 设置运行模式
		/// </summary>
		/// <param name="runMode">运行模式</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetRunModeAsync(RunMode runMode, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new RunModeParam(runMode), timeoutMilliseconds);

		/// <summary>
		/// 设置转速模式转速指令
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetIqRefAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new IqRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置转矩限制
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetLimitTorqueAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitTorqueParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Kp
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetCurKpParamAsync(float value = (float)0.125, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurKpParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流的 Ki
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetCurKiParamAsync(float value = (float)0.0158, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurKiParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置电流滤波系数
		/// </summary>
		/// <param name="masterId">主控制器id</param>
		/// <param name="motorId">电机id</param>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetCurFiltGainParamAsync(float value = (float)0.1, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new CurFiltGainParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式角度指令
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetLocRefParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LocRefParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置位置模式速度设置
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetLimitSpdParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitSpdParam(value), timeoutMilliseconds);

		/// <summary>
		/// 设置速度位置模式电流设置
		/// </summary>
		/// <param name="value"></param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetLimitCurParamAsync(float value, int timeoutMilliseconds = 2000)
			=> await WriteParamAsync(new LimitCurParam(value), timeoutMilliseconds);

		/// <summary>
		/// 读取单个参数
		/// </summary>
		/// <param name="index">参数的索引</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		/// <remarks>
		/// 具体的index请参见官方手册
		/// </remarks>
		public async Task<SingleParamFeedback> ReadSingleParamAsync(ushort index, int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				throw new InvalidOperationException("canbus is not started");
			// 计算仲裁ID
			uint arbitrationId = (uint)CmdMode.SINGLE_PARAM_READ << 24 | _canBus.MasterCanId << 8 | this._canId;
			byte[] data = new byte[8];
			BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(), index);
			var canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = 8,
				Data = data
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return SingleParamFeedback.Parse(reply);
		}

		/// <summary>
		/// 电机使能运行
		/// </summary>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> EnableAsync(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				throw new InvalidOperationException("canbus is not started");
			// 计算仲裁ID
			uint arbitrationId = (uint)CmdMode.MOTOR_ENABLE << 24 | _canBus.MasterCanId << 8 | this._canId;
			var canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = 0,
				Data = Array.Empty<byte>()
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return MotorFeedback.Parse(reply);
		}

		/// <summary>
		/// 停止电机
		/// </summary>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> DisableAsnyc(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				throw new InvalidOperationException("canbus is not started");
			// 计算仲裁ID
			uint arbitrationId = (uint)CmdMode.MOTOR_STOP << 24 | _canBus.MasterCanId << 8 | this._canId;
			var canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = 8,
				Data = new byte[8]
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return MotorFeedback.Parse(reply);
		}

		/// <summary>
		/// 设置机械零点
		/// </summary>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SetMechanicalZeroAsync(int timeoutMilliseconds = 2000)
		{
			if (!_canBus._isRunning)
				throw new InvalidOperationException("canbus is not started");
			// 计算仲裁ID
			uint arbitrationId = (uint)CmdMode.SET_MECHANICAL_ZERO << 24 | _canBus.MasterCanId << 8 | this._canId;
			var canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = 1,
				Data = new byte[] { 1 }
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return MotorFeedback.Parse(reply);
		}

		/// <summary>
		/// 运控模式
		/// </summary>
		/// <param name="torque">扭矩</param>
		/// <param name="target_angle">目标角度</param>
		/// <param name="target_velocity">目标速度</param>
		/// <param name="Kp">比例增益</param>
		/// <param name="Kd">微分增益</param>
		/// <param name="timeoutMilliseconds">超时时间</param>
		public async Task<MotorFeedback> SendMotorControlCommandAsync(
			float torque,
			float target_angle,
			float target_velocity,
			float Kp,
			float Kd,
			int timeoutMilliseconds = 2000)
		{
			//生成29位的仲裁ID的组成部分
			uint torque_mapped = Calculate.FToU(torque, -12.0, 12.0);
			// 计算仲裁ID，
			uint arbitrationId = (uint)CmdMode.MOTOR_CONTROL << 24 | torque_mapped << 8 | this._canId;

			// 生成数据区1
			//目标角度
			uint target_angle_mapped = Calculate.FToU(target_angle, -4 * Math.PI, 4 * Math.PI);
			//目标速度
			uint target_velocity_mapped = Calculate.FToU(target_velocity, -30.0F, 30.0F);
			uint Kp_mapped = Calculate.FToU(Kp, 0.0F, 500.0F);//比例增益
			uint Kd_mapped = Calculate.FToU(Kd, 0.0F, 5.0F);//微分增益

			//组合为一个8个字节的data
			byte[] data = new byte[8];
			Array.Copy(BitConverter.GetBytes(target_angle_mapped), 0, data, 0, 2);
			Array.Copy(BitConverter.GetBytes(target_velocity_mapped), 0, data, 2, 2);
			Array.Copy(BitConverter.GetBytes(Kp_mapped), 0, data, 4, 2);
			Array.Copy(BitConverter.GetBytes(Kd_mapped), 0, data, 6, 2);
			// 一条CAN消息结构
			PcanMessage canMessage = new PcanMessage
			{
				ID = arbitrationId,
				MsgType = MessageType.Extended,
				DLC = Convert.ToByte(data.Length),
				Data = data
			};
			var reply = await _canBus.SendAsync(canMessage, timeoutMilliseconds);
			return MotorFeedback.Parse(reply);
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
