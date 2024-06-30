using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control
{
	/// <summary>
	/// 仲裁ID的通讯类型
	/// </summary>
	public enum CmdMode : byte
	{
		/// <summary>
		/// 获取设备ID
		/// </summary>
		GET_DEVICE_ID = 0,
		/// <summary>
		/// 电机运控模式
		/// </summary>
		MOTOR_CONTROL = 1,
		/// <summary>
		/// 电机反馈
		/// </summary>
		MOTOR_FEEDBACK = 2,
		/// <summary>
		/// 电机使能
		/// </summary>
		MOTOR_ENABLE = 3,
		/// <summary>
		/// 电机停止
		/// </summary>
		MOTOR_STOP = 4,
		/// <summary>
		/// 设置机械零点
		/// </summary>
		SET_MECHANICAL_ZERO = 6,
		/// <summary>
		/// 设置电机CAN ID
		/// </summary>
		SET_MOTOR_CAN_ID = 7,
		/// <summary>
		/// 参数表写入
		/// </summary>
		PARAM_TABLE_WRITE = 8,
		/// <summary>
		/// 单个参数读取
		/// </summary>
		SINGLE_PARAM_READ = 17,
		/// <summary>
		/// 单个参数写入
		/// </summary>
		SINGLE_PARAM_WRITE = 18,
		/// <summary>
		/// 故障反馈
		/// </summary>
		FAULT_FEEDBACK = 21
	}
}
