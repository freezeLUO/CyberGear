using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Peak.Can.Basic;
using Peak.Can.Basic.BackwardCompatibility;
using Peak;
using CyberGear_Pan;


namespace Myspace
{
    class Program
    {
        static void Main(string[] args)
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
                // 显示连接成功消息
                //
                Console.WriteLine($"通道{channel}表示的硬件已成功初始化");

                var motor = new CANMotorController(127, 0, channel);

                //设置机械零点
                Console.WriteLine("写入设置0点");
                Console.ReadKey();
                motor.Set0();

                //位置模式
                //发送电机模式参数写入命令（通信类型 18）
                //设置 `runmode` 参数为 1
                //- index(Byte0~1): `run_mode`，0x7005
                //- value(Byte4~7): 1(位置模式)
                uint index = 0x7005;
                int value = 1;
                Console.ReadKey();
                Console.WriteLine("写入位置模式");
                motor.WriteSingleParam(index, value,"u8");
                System.Threading.Thread.Sleep(50);
                Console.ReadKey();
                Console.WriteLine("写入启动");
                motor.Enable();

                //最大速度：发送电机模式参数写入命令（通信类型 18）
                //设置 `limit_spd` 参数为预设最大速度指令
                //- index(Byte0~1): `limit_spd`, 0x7017
                //- value(Byte4~7): `float` [0,30]rad / s
                int value1 = 3;
                Console.ReadKey();
                Console.WriteLine("写入速度");
                motor.WriteSingleParam(0x7017, value1);
                System.Threading.Thread.Sleep(50);

                //目标位置：发送电机模式参数写入命令（通信类型 18）
                //设置 `loc_ref` 参数为预设位置指令
                //- index(Byte0~1): `loc_ref`, 0x7016
                //- value(Byte4~7): `float` rad
                //int value2 = 1;
                Console.ReadKey();
                Console.WriteLine("写入转到位置1");
                motor.WriteSingleParam(0x7016, 1);
                System.Threading.Thread.Sleep(50);

                Console.ReadKey();
                Console.WriteLine("写入转到位置2");
                motor.WriteSingleParam(0x7016, 2);
                System.Threading.Thread.Sleep(50);

                Console.ReadKey();
                Console.WriteLine("写入转到位置0");
                motor.WriteSingleParam(0x7016, 0);
                System.Threading.Thread.Sleep(50);

                Console.WriteLine("按任意键停止电机");
                Console.ReadKey(); // 等待用户按下任意键

                Console.WriteLine("写入停止电机");
                motor.Disable();


                // 给驱动一些时间发送信息……
                //
                System.Threading.Thread.Sleep(50);

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
                    Console.WriteLine($"通道{channel}表示的硬件已成功关闭");
            }

        }
    }
}
