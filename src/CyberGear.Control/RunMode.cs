using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyberGear.Control
{
	/// <summary>
	/// 运行模式
	/// </summary>
	public enum RunMode
	{
		/// <summary>
		/// 运控模式
		/// </summary>
		CONTROL_MODE = 0,
		/// <summary>
		/// 位置模式
		/// </summary>
		POSITION_MODE = 1,
		/// <summary>
		/// 速度模式
		/// </summary>
		SPEED_MODE = 2,
		/// <summary>
		/// 电流模式
		/// </summary>
		CURRENT_MODE = 3
	}
}
