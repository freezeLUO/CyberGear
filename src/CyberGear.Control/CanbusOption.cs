using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control
{
	public class CanbusOption
	{
		/// <summary>
		/// 主控制器 id
		/// </summary>
		public uint MasterId { get; set; } = 0;

		/// <summary>
		/// 波特率
		/// </summary>
		public Bitrate Bitrate { get; set; } = Bitrate.Pcan1000;

		public readonly List<uint> MotorCanIds = new List<uint>();

		/// <summary>
		/// 新增电机
		/// </summary>
		/// <param name="motorId">电机编号</param>
		/// <exception cref="ArgumentException"></exception>
		public void AddMotor(uint motorId)
		{
			for (int i = 0; i < MotorCanIds.Count; i++)
			{
				if (MotorCanIds[i] == motorId)
					throw new ArgumentException("exist same motor can id");
			}
			MotorCanIds.Add(motorId);
		}

		/// <summary>
		/// 新增电机
		/// </summary>
		/// <param name="motorCanIds">电机编号</param>
		/// <exception cref="ArgumentException"></exception>
		public void AddMotors(uint[] motorCanIds)
		{
			for (int i = 0; i < motorCanIds.Length; i++)
			{
				for (int j = 0; i < MotorCanIds.Count; j++)
				{
					if (motorCanIds[i] == MotorCanIds[j])
						throw new ArgumentException("exist same motor can id");
				}
			}
			MotorCanIds.AddRange(motorCanIds);
		}
	}
}
