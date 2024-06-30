using CyberGear.Control;
using CyberGear.Control.Params;
using NLog;
using Peak.Can.Basic;

var _logger = LogManager.GetCurrentClassLogger();

_logger.Info("程序启动");

PcanChannel channel = PcanChannel.Usb01;

// 硬件以1000k bit/s初始化
PcanStatus result = Api.Initialize(channel, Bitrate.Pcan1000);
if (result != PcanStatus.OK)
{
	Api.GetErrorText(result, out var errorText);
	Console.WriteLine(errorText);
	return;
}

// 初始化成功
_logger.Info("通道 {channel} 表示的硬件已成功初始化", channel);
Console.ReadKey();

// 创建接收器实例
PcanReceiver receiver = new PcanReceiver(channel);
if (receiver.Start())
{
	_logger.Info("接收器已启动，如果需要结束接收器进程，请调用receiver.Stop()函数");
}
else
{
	_logger.Warn("接收器启动失败");
}
// 创建控制器实例
var motor = new Controller(0, 127, channel);

/////////////////////////////////////////////////////////////////////////////////////
//以下为测试代码
////////////////////////////////////////////////////////////////////////////////////

Console.ReadKey();
//设置机械零点
motor.SetMechanicalZero();
_logger.Info("写入设置0点");

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
motor.SetLocRefParam(1.1F);
_logger.Info("写入转到位置1");

Console.ReadKey();
motor.SetLocRefParam(2.0F);
_logger.Info("写入转到位置2");

Console.ReadKey();
motor.SetLocRefParam(0.0F);
_logger.Info("写入转到位置0");

///////////////////////////////////////////////////
//运控模式示例
///////////////////////////////////////////////////
//Console.ReadKey();
//index = 0x7005;
//Console.WriteLine("写入运控模式");
//Motor.WriteSingleParam(index, 0);
//Console.ReadKey();
//Console.WriteLine("写入转到位置x1");
//Motor.SendMotorControlCommand(0.1F, 4.0F, 1.0F, 2.0F, 0.1F);
//Console.ReadKey();
//Console.WriteLine("写入转到位置0");
//Motor.SendMotorControlCommand(0.1F, 0.0F, 1.0F, 2.0F, 0.1F);
//Console.WriteLine("按任意键停止电机");
//Console.ReadKey(); // 等待用户按下任意键


motor.DisableMotor();
_logger.Info("写入停止电机");

// 当不再需要硬件时，将完成与硬件的连接
result = Api.Uninitialize(channel);
if (result != PcanStatus.OK)
{
	// 发生错误
	Api.GetErrorText(result, out var errorText);
	_logger.Warn("通道 {channel} 表示的硬件关闭失败: {eror}", channel, errorText);
}
else
{
	_logger.Info("通道 {channel} 表示的硬件已成功关闭", channel);
}
