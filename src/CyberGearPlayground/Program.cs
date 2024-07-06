using CyberGear.Control;
using CyberGear.Control.Params;
using CyberGear.Control.Protocols;
using CyberGear.Control.ReceiveMessageType;
using NLog;
using System.Diagnostics;
using static CyberGear.Control.Controller;

var _logger = LogManager.GetCurrentClassLogger();

_logger.Info("------程序启动------");

// 创建控制器实例, 参数依次为: 通信类型Pcan-Usb, 通道1, 上位机CANID, 电机CANID, 接收消息超时时间
var motor = new Controller(SlotType.Usb, 1, 0, 127);

if (!motor.Init(Bitrate.Pcan1000))
{
	_logger.Info("初始化失败");
	return;
}
_logger.Info("初始化成功");

#region 以下为测试代码
Console.ReadKey();
//设置机械零点，读取一次最新反馈数据
IMessageType Messagedata = motor.SetMechanicalZero();
_logger.Info("写入设置0点，并获取一次最新消息");
switch (Messagedata)
{
    case MessageType<ResponseData> responseData:
        // 处理 ResponseData
        _logger.Info($"Timestamp: {responseData.CanTimestamp},Motor CAN ID: {responseData.MotorCanId}, Main CAN ID: {responseData.MasterCanId}, pos: {responseData.Data.Angle:.2f} rad, vel: {responseData.Data.AngularVelocity:.2f} rad/s, Torque: {responseData.Data.Torque:.2f} N·m");
        break;
    case MessageType<SingleResponseData> singleResponseData:
        // 处理 SingleResponseData
        _logger.Info($"Timestamp: {singleResponseData.CanTimestamp},Motor CAN ID: {singleResponseData.MotorCanId}, Main CAN ID: {singleResponseData.MasterCanId}, Index: {singleResponseData.Data.Index}, Data: {singleResponseData.Data.value_bytes.ToString}");
        break;
    default:
        // 未知类型
        break;
}


Console.ReadKey();
//位置模式
//发送电机模式参数写入命令（通信类型 18）
//设置 `runmode` 参数为 1
//- index(Byte0~1): `run_mode`，0x7005
//- value(Byte4~7): 1(位置模式)
motor.SetRunMode(RunMode.POSITION_MODE);
_logger.Info("写入位置模式");


Console.ReadKey();
// 启动
motor.EnableMotor();
_logger.Info("写入启动");


Console.ReadKey();
//设置最大速度：发送电机模式参数写入命令（通信类型 18）
//设置 `limit_spd` 参数为预设最大速度指令
//- index(Byte0~1): `limit_spd`, 0x7017
//- value(Byte4~7): `float` [0,30]rad / s
motor.SetLimitSpdParam(3.1F);
_logger.Info("写入速度");


Console.ReadKey();
//设置目标位置：发送电机模式参数写入命令（通信类型 18）
//设置 `loc_ref` 参数为预设位置指令
//- index(Byte0~1): `loc_ref`, 0x7016
//- value(Byte4~7): `float` rad
//int value2 = 1;
IMessageType Messagedata1 = motor.SetLocRefParam(1.1F);
_logger.Info("写入转到位置 1.1 rad");
switch (Messagedata1)
{
    case MessageType<ResponseData> responseData:
        // 处理 ResponseData
        _logger.Info($"Timestamp: {responseData.CanTimestamp},Motor CAN ID: {responseData.MotorCanId}, Main CAN ID: {responseData.MasterCanId}, pos: {responseData.Data.Angle:.2f} rad, vel: {responseData.Data.AngularVelocity:.2f} rad/s, Torque: {responseData.Data.Torque:.2f} N·m");
        break;
    case MessageType<SingleResponseData> singleResponseData:
        // 处理 SingleResponseData
        _logger.Info($"Timestamp: {singleResponseData.CanTimestamp},Motor CAN ID: {singleResponseData.MotorCanId}, Main CAN ID: {singleResponseData.MasterCanId}, Index: {singleResponseData.Data.Index}, Data: {singleResponseData.Data.value_bytes.ToString}");
        break;
    default:
        // 未知类型
        break;
}

Console.ReadKey();
motor.SetLocRefParam(2.0F);
_logger.Info("写入转到位置 2.0 rad");


Console.ReadKey();
motor.SetLocRefParam(0.0F);
_logger.Info("写入转到位置 0 rad");

///////////////////////////////////////////////////
//运控模式示例，请根据实际情况选择合适参数调用
///////////////////////////////////////////////////
//Console.ReadKey();
//index = 0x7005;
//Console.WriteLine("写入运控模式");
//Motor.WriteSingleParam(index, 0);
//Console.ReadKey();
//Console.WriteLine("写入转到位置x1");

	//运控模式下发送电机控制指令。
	//参数:
	//torque: 扭矩。
	//target_angle: 目标角度。
	//target_velocity: 目标速度。
	//Kp: 比例增益。
	//Kd: 微分增益。
//Motor.SendMotorControlCommand(-0.1F, 4.0F, 1.0F, 2.0F, 0.1F);

//Console.ReadKey();
//Console.WriteLine("写入转到位置0");
//Motor.SendMotorControlCommand(-0.1F, 0.0F, 1.0F, 2.0F, 0.1F);
//Console.WriteLine("按任意键停止电机");
//Console.ReadKey(); // 等待用户按下任意键


//失能电机并获取反馈数据队列
Console.ReadKey();
motor.DisableMotor();
_logger.Info("写入停止电机");
Thread.Sleep(50);

//结束
motor.Stop();//停止接收数据的线程
motor.Dispose();//释放资源
#endregion
