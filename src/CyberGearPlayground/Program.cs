using CyberGear.Control;
using CyberGear.Control.Params;
using NLog;

var _logger = LogManager.GetCurrentClassLogger();

var builder = CanBus.CreateBuilder(SlotType.Usb, 1);
builder.Configure(opt =>
{
	opt.AddMotors(new uint[] { 127, 2 });
});
CanBus can = builder.Build();

Console.WriteLine("enter to start");
Console.ReadKey();
//设置机械零点，读取一次最新反馈数据
var motorFeedback = await can.Motors[0].SetMechanicalZeroAsync();
_logger.Info("写入机械零点");
Console.ReadKey();

await can.Motors[0].SetRunModeAsync(RunMode.POSITION_MODE);
_logger.Info("写入位置模式");
Console.ReadKey();

await can.Motors[0].EnableAsync();
_logger.Info("写入启动");
Console.ReadKey();

await can.Motors[0].SetLimitSpdParamAsync(3.1F);
_logger.Info("写入速度");
Console.ReadKey();

await can.Motors[0].SetLocRefParamAsync(1.1F);
_logger.Info("写入转到位置 1.1 rad");
Console.ReadKey();

await can.Motors[0].SetLocRefParamAsync(2.0F);
_logger.Info("写入转到位置 2.0 rad");
Console.ReadKey();

await can.Motors[0].SetLocRefParamAsync(0.0F);
_logger.Info("写入转到位置 0 rad");
Console.ReadKey();

await can.Motors[0].DisableAsnyc();
_logger.Info("写入停止电机");
Thread.Sleep(50);

//结束
can.Stop();//停止接收数据的线程
can.Dispose();//释放资源
