using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peak.Can.Basic;
using System.Diagnostics;
using System.Threading.Tasks;
namespace CyberGear_Control_.NET
{
    public class Test
    {
        public static void Main(string[] args)
        {
            PcanChannel channel = PcanChannel.Usb01;

            // 硬件以1000k bit/s初始化
            PcanStatus result = Api.Initialize(channel, Bitrate.Pcan1000);
            if (result != PcanStatus.OK)
            {
                // 发生错误
                //
                Api.GetErrorText(result, out var errorText);
                Console.WriteLine(errorText);
            }
            else
            {
                // 初始化成功
                Console.WriteLine($"通道{channel}表示的硬件已成功初始化");
                Console.ReadKey();
                // 创建接收器实例
                PcanReceiver receiver = new PcanReceiver(channel);
                if (receiver.Start())
                {
                    Console.WriteLine("接收器已启动，如果需要结束接收器进程，请调用receiver.Stop()函数");
                }
                else
                {
                    Console.WriteLine("接收器启动失败。");
                }

                var Motor = new Controller(0, 127, channel);

                /////////////////////////////////////////////////////////////////////////////////////
                //以下为测试代码
                ////////////////////////////////////////////////////////////////////////////////////
                //设置机械零点
                Console.WriteLine("写入设置0点");
                Console.ReadKey();
                Motor.SetMechanicalZero();

                //位置模式
                //发送电机模式参数写入命令（通信类型 18）
                //设置 `runmode` 参数为 1
                //- index(Byte0~1): `run_mode`，0x7005
                //- value(Byte4~7): 1(位置模式)
                uint index = 0x7005;
                byte value = 1;
                Console.ReadKey();
                Console.WriteLine("写入位置模式");
                Motor.WriteSingleParam(index, value);
                Console.ReadKey();
                Console.WriteLine("写入启动");
                Motor.EnableMotor();
                //设置最大速度：发送电机模式参数写入命令（通信类型 18）
                //设置 `limit_spd` 参数为预设最大速度指令
                //- index(Byte0~1): `limit_spd`, 0x7017
                //- value(Byte4~7): `float` [0,30]rad / s
                float value1 = 3.1F;
                Console.ReadKey();
                Console.WriteLine("写入速度");
                index = 0x7017;
                Motor.WriteSingleParam(index, value1);
                //设置目标位置：发送电机模式参数写入命令（通信类型 18）
                //设置 `loc_ref` 参数为预设位置指令
                //- index(Byte0~1): `loc_ref`, 0x7016
                //- value(Byte4~7): `float` rad
                //int value2 = 1;
                Console.ReadKey();
                Console.WriteLine("写入转到位置1");
                index = 0x7016;
                Motor.WriteSingleParam(index, 1.1F);

                Console.ReadKey();
                Console.WriteLine("写入转到位置2");
                Motor.WriteSingleParam(index, 2.0F);

                Console.ReadKey();
                Console.WriteLine("写入转到位置0");
                Motor.WriteSingleParam(index, 0.0F);
                ///////////////////////////////////////////////////
                //运控模式示例
                ///////////////////////////////////////////////////
                Console.ReadKey();
                index = 0x7005;
                Console.WriteLine("写入运控模式");
                Motor.WriteSingleParam(index, 0);
                Console.ReadKey();
                Console.WriteLine("写入转到位置x1");
                Motor.SendMotorControlCommand(0.1F, 4.0F, 1.0F, 2.0F, 0.1F);
                Console.ReadKey();
                Console.WriteLine("写入转到位置0");
                Motor.SendMotorControlCommand(0.1F, 0.0F, 1.0F, 2.0F, 0.1F);
                Console.WriteLine("按任意键停止电机");
                Console.ReadKey(); // 等待用户按下任意键

                Console.WriteLine("写入停止电机");
                Motor.DisableMotor();
                // 当不再需要硬件时，将完成与硬件的连接
                //
                result = Api.Uninitialize(channel);
                if (result != PcanStatus.OK)
                {
                    // 发生错误
                    //
                    Api.GetErrorText(result, out var errorText);
                    Console.WriteLine(errorText);
                }
                else
                {
                    Console.WriteLine($"通道{channel}表示的硬件已成功关闭");
                }

            }

        }

          // 接收线程，用于处理接收到的CAN消息

    }

    
}
