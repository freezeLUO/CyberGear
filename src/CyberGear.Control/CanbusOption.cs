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

		public readonly List<uint> MotorIds = new List<uint>();

		public void AddMotor(uint motorId)
		{
			for (int i = 0; i < MotorIds.Count; i++)
			{
				if (MotorIds[i] == motorId)
					throw new ArgumentException("exist same motor id");
			}
			MotorIds.Add(motorId);
		}

		public void AddMotors(uint[] motorIds)
		{
			for (int i = 0; i < motorIds.Length; i++)
			{
				for (int j = 0; i < MotorIds.Count; j++)
				{
					if (motorIds[i] == MotorIds[j])
						throw new ArgumentException("exist same motor id");
				}
			}
			MotorIds.AddRange(motorIds);
		}
	}
}
