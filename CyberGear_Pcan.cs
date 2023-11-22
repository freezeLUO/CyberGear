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


namespace CyberGear_Pcan
{

    public class CANMotorController
    {
        private static Dictionary<string, Dictionary<string, object>> PARAM_TABLE = new Dictionary<string, Dictionary<string, object>>
        {
            { "motorOverTemp", new Dictionary<string, object> { { "feature_code", 0x200D }, { "type", "int16" } } },
            { "overTempTime", new Dictionary<string, object> { { "feature_code", 0x200E }, { "type", "int32" } } },
            { "limit_torque", new Dictionary<string, object> { { "feature_code", 0x2007 }, { "type", "float" } } },
            { "cur_kp", new Dictionary<string, object> { { "feature_code", 0x2012 }, { "type", "float" } } },
            { "cur_ki", new Dictionary<string, object> { { "feature_code", 0x2013 }, { "type", "float" } } },
            { "spd_kp", new Dictionary<string, object> { { "feature_code", 0x2014 }, { "type", "float" } } },
            { "spd_ki", new Dictionary<string, object> { { "feature_code", 0x2015 }, { "type", "float" } } },
            { "loc_kp", new Dictionary<string, object> { { "feature_code", 0x2016 }, { "type", "float" } } },
            { "spd_filt_gain", new Dictionary<string, object> { { "feature_code", 0x2017 }, { "type", "float" } } },
            { "limit_spd", new Dictionary<string, object> { { "feature_code", 0x2018 }, { "type", "float" } } },
            { "limit_cur", new Dictionary<string, object> { { "feature_code", 0x2019 }, { "type", "float" } } }
        };

        // 定义了一个名为PARAM_TABLE的字典，其中包含11个键值对。这些键值对
        // 表示不同的参数名称及其对应的特征码（feature code）和数据类型（type）。
        // "motorOverTemp"：表示电机过热，特征码为0x200D，数据类型为int16（16位有符号整数）。
        // "overTempTime"：表示过热时间，特征码为0x200E，数据类型为int32（32位有符号整数）。
        // "limit_torque"：表示限扭矩，特征码为0x2007，数据类型为float（浮点数）。
        // "cur_kp"：表示电流比例积分参数Kp，特征码为0x2012，数据类型为float（浮点数）。
        // "cur_ki"：表示电流比例积分参数Ki，特征码为0x2013，数据类型为float（浮点数）。
        // "spd_kp"：表示速度比例积分参数Kp，特征码为0x2014，数据类型为float（浮点数）。
        // "spd_ki"：表示速度比例积分参数Ki，特征码为0x2015，数据类型为float（浮点数）。
        // "loc_kp"：表示位置比例积分参数Kp，特征码为0x2016，数据类型为float（浮点数）。
        // "spd_filt_gain"：表示速度滤波增益，特征码为0x2017，数据类型为float（浮点数）。
        // "limit_spd"：表示速度限制，特征码为0x2018，数据类型为float（浮点数）。
        // "limit_cur"：表示电流限制，特征码为0x2019，数据类型为float（浮点数）。
        // 这个字典可以用于存储和检索与电机控制相关的参数。


        private static Dictionary<string, Dictionary<string, object>> PARAMETERS = new Dictionary<string, Dictionary<string, object>>
        {
            { "run_mode", new Dictionary<string, object> { { "index", 0x7005 }, { "format", "u8" } } },
            { "iq_ref", new Dictionary<string, object> { { "index", 0x7006 }, { "format", "f" } } },
            { "spd_ref", new Dictionary<string, object> { { "index", 0x700A }, { "format", "f" } } },
            { "limit_torque", new Dictionary<string, object> { { "index", 0x700B }, { "format", "f" } } },
            { "cur_kp", new Dictionary<string, object> { { "index", 0x7010 }, { "format", "f" } } },
            { "cur_ki", new Dictionary<string, object> { { "index", 0x7011 }, { "format", "f" } } },
            { "cur_filt_gain", new Dictionary<string, object> { { "index", 0x7014 }, { "format", "f" } } },
            { "loc_ref", new Dictionary<string, object> { { "index", 0x7016 }, { "format", "f" } } },
            { "limit_spd", new Dictionary<string, object> { { "index", 0x7017 }, { "format", "f" } } },
            { "limit_cur", new Dictionary<string, object> { { "index", 0x7018 }, { "format", "f" } } }
        };
        //定义了一个名为PARAMETERS的字典，其中包含10个键值对。
        //这些键值对表示不同的参数名称及其对应的索引（index）和数据格式（format）。
        //"run_mode"：表示运行模式，索引为0x7005，数据格式为u8（8位无符号整数）。
        //"iq_ref"：表示Iq参考值，索引为0x7006，数据格式为f（32位浮点数）。
        //"spd_ref"：表示速度参考值，索引为0x700A，数据格式为f（32位浮点数）。
        //"limit_torque"：表示限扭矩，索引为0x700B，数据格式为f（32位浮点数）。
        //"cur_kp"：表示电流比例积分参数Kp，索引为0x7010，数据格式为f（32位浮点数）。
        //"cur_ki"：表示电流比例积分参数Ki，索引为0x7011，数据格式为f（32位浮点数）。
        //"cur_filt_gain"：表示电流滤波增益，索引为0x7014，数据格式为f（32位浮点数）。
        //"loc_ref"：表示位置参考值，索引为0x7016，数据格式为f（32位浮点数）。
        //"limit_spd"：表示速度限制，索引为0x7017，数据格式为f（32位浮点数）。
        //"limit_cur"：表示电流限制，索引为0x7018，数据格式为f（32位浮点数）。

        private int TWO_BYTES_BITS = 16;
        private uint MOTOR_ID;
        private uint MAIN_CAN_ID;
        private float P_MIN;
        private float P_MAX;
        private float V_MIN;
        private float V_MAX;
        private float T_MIN;
        private float T_MAX;
        private float KP_MIN;
        private float KP_MAX;
        private float KD_MIN;
        private float KD_MAX;
        private PcanChannel channel;


        public CANMotorController(uint motor_id = 127, uint main_can_id = 255, PcanChannel channel = PcanChannel.Usb01)
        {
            this.MOTOR_ID = motor_id;
            this.MAIN_CAN_ID = main_can_id;
            this.P_MIN = -12.5F;
            this.P_MAX = 12.5F;
            this.V_MIN = -30.0F;
            this.V_MAX = 30.0F;
            this.T_MIN = -12.0F;
            this.T_MAX = 12.0F;
            this.KP_MIN = 0.0F;
            this.KP_MAX = 500.0F;
            this.KD_MIN = 0.0F;
            this.KD_MAX = 5.0F;
            this.TWO_BYTES_BITS = 16;
            this.channel = channel;//PcanChannel.Usb01;
        }

        private class CmdModes
        {
            public const uint GET_DEVICE_ID = 0;
            public const uint MOTOR_CONTROL = 1;
            public const uint MOTOR_FEEDBACK = 2;
            public const uint MOTOR_ENABLE = 3;
            public const uint MOTOR_STOP = 4;
            public const uint SET_MECHANICAL_ZERO = 6;
            public const uint SET_MOTOR_CAN_ID = 7;
            public const uint PARAM_TABLE_WRITE = 8;
            public const uint SINGLE_PARAM_READ = 17;
            public const uint SINGLE_PARAM_WRITE = 18;
            public const uint FAULT_FEEDBACK = 21;
        }


        public enum RunModes
        {
            CONTROL_MODE = 0,    // 运控模式
            POSITION_MODE = 1,   // 位置模式
            SPEED_MODE = 2,      // 速度模式
            CURRENT_MODE = 3     // 电流模式
        }

        public uint FloatToUInt(float x, float x_min, float x_max, int bits)
        {
            // 将浮点数转换为无符号整数。

            float span = x_max - x_min;
            float offset = x_min;

            // Clamp x to the range [x_min, x_max]
            x = Math.Max(Math.Min(x, x_max), x_min);

            return (uint)((x - offset) * ((1 << bits) - 1) / span);
        }

        public float UIntToFloat(uint x, float x_min, float x_max, int bits)
        {
            // 将无符号整数转换为浮点数。

            int span = (1 << bits) - 1;
            float offset = x_max - x_min;

            // Clamp x to the range [0, span]
            x = (uint)Math.Max(Math.Min(x, span), 0);

            float a = offset * x / span + x_min;

            return a;
        }

        public uint LinearMapping(double value, double value_min, double value_max, int target_min = 0, int target_max = 65535)
        {
            // 对输入值进行线性映射。

            return (uint)((value - value_min) / (value_max - value_min) * (target_max - target_min) + target_min);
        }

        public List<byte> FormatData(float value, string format = "f", string type = "decode")
        {
            /*
            对数据进行编码或解码。

            参数:
            data: 输入的数据列表。
            format: 数据的格式。
            type: "encode" 或 "decode", 表示是进行编码还是解码。

            返回:
            编码或解码后的数据。
            */
            if (format == "f")
            {
                //object[] s_f = { 4, "f" };
                float floatValue = (float)value;
                byte[] bs = BitConverter.GetBytes(floatValue);
                List<byte> rdata = new List<byte>();

                for (int j = 0; j < bs.Length; j++)
                {
                    rdata.Add(bs[j]);
                }

                return rdata;
            }
            else if (format == "u8")
            {
                //object[] s_f = { 1, "B" };
                byte byteValue = (byte)value;
                byte[] bs = new byte[] { byteValue };
                bs = bs.Concat(Enumerable.Repeat((byte)0, 3)).ToArray();
                List<byte> rdata = new List<byte>();

                for (int j = 0; j < bs.Length; j++)
                {
                    rdata.Add(bs[j]);
                }

                return rdata;
            }
            else { return new List<byte>(); }

        }
        /*
        public List<byte> PackTo8Bytes(float targetAngle, float targetVelocity, float Kp, float Kd)
        {
            // 定义打包数据函数，将控制参数打包为8字节的数据。

            // 对输入变量进行线性映射
            float targetAngleMapped = LinearMapping(targetAngle, P_MIN, P_MAX);
            float targetVelocityMapped = LinearMapping(targetVelocity, V_MIN, V_MAX);
            float KpMapped = LinearMapping(Kp, KP_MIN, KP_MAX);
            float KdMapped = LinearMapping(Kd, KD_MIN, KD_MAX);

            // 使用BitConverter进行打包，类似struct.pack(python)
            List<byte> data1 = new List<byte>();
            data1.AddRange(BitConverter.GetBytes((ushort)targetAngleMapped));
            data1.AddRange(BitConverter.GetBytes((ushort)targetVelocityMapped));
            data1.AddRange(BitConverter.GetBytes((ushort)KpMapped));
            data1.AddRange(BitConverter.GetBytes((ushort)KdMapped));

            return data1;

        }
        */



        public Tuple<byte[], uint> SendReceiveCanMessage(uint cmdMode, uint date2, byte[] data1, uint timeout = 200)
        {
            // Calculate the arbitration ID
            uint arbitrationId = (cmdMode << 24) | (date2 << 8) | this.MOTOR_ID;

            // Create a TPCANMsg CAN message structure
            PcanMessage canMessage = new PcanMessage
            {
                ID = arbitrationId,
                MsgType = MessageType.Extended,
                DLC = Convert.ToByte(data1.Length),
                Data = data1
            };


            // Write the CAN message
            PcanStatus writeStatus = Api.Write(this.channel, canMessage);
            if (writeStatus != PcanStatus.OK)
            {
                Console.WriteLine("Failed to send the message.");
                return Tuple.Create<byte[], uint>(null, 0);
            }

            // Output details of the sent message
            Console.WriteLine($"Sent message with ID {arbitrationId:X}, data: {BitConverter.ToString(data1)}");

            System.Threading.Thread.Sleep(50);  // Give the driver some time to send the messages...

            PcanMessage receivedMsg;
            ulong timestamp;

            PcanStatus readStatus = Api.Read(this.channel, out receivedMsg, out timestamp);

            // Check if received a message
            if (readStatus == PcanStatus.OK)
            {
                DataBytes DB = new DataBytes();
                DB = receivedMsg.Data;
                byte[] bytes = DB;

                return Tuple.Create(bytes, receivedMsg.ID);
            }
            else
            {
                Debug.WriteLine("Failed to receive the message or message was not received within the timeout period.");
                return Tuple.Create<byte[], uint>(null, 0);
            }


        }

        public Tuple<byte, float, float, float> ParseReceivedMsg(byte[] data, uint arbitration_id)
        {
            //解析接收到的CAN消息。

            //参数:
            //data: 接收到的数据。
            //arbitration_id: 接收到的消息的仲裁ID。

            //返回:
            //一个元组, 包含电机的CAN ID、位置(rad)、速度(rad / s)、力矩(Nm)。
            if (data != null)
            {
                Debug.WriteLine($"Received message with ID 0x{arbitration_id:X}");

                // 解析电机CAN ID
                byte motor_can_id = (byte)((arbitration_id >> 8) & 0xFF);

                //float pos = UIntTofloat(
                //    (ushort)((data[0] << 8) + data[1]), this.P_MIN, this.P_MAX, this.TWO_BYTES_BITS);

                float pos = UIntToFloat(
                    (uint)((data[0] << 8) + data[1]), this.P_MIN, this.P_MAX, this.TWO_BYTES_BITS);

                float vel = UIntToFloat(
                    (uint)((data[2] << 8) + data[3]), this.V_MIN, this.V_MAX, this.TWO_BYTES_BITS);
                float torque = UIntToFloat(
                    (uint)((data[4] << 8) + data[5]), this.T_MIN, this.T_MAX, TWO_BYTES_BITS);

                Debug.WriteLine($"Motor CAN ID: {motor_can_id}, pos: {pos:.2f} rad, vel: {vel:.2f} rad/s, torque: {torque:.2f} Nm");

                return new Tuple<byte, float, float, float>(motor_can_id, pos, vel, torque);
            }
            else
            {
                Debug.WriteLine("No message received within the timeout period.");
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }
        }

        public void ClearCanRx(int timeout = 10)
        {
            float timeoutSeconds = timeout / 1000.0F; // Convert to seconds
            while (true)
            {
                PcanMessage receivedMsg;
                ulong timestamp;

                PcanStatus readStatus = Api.Read(this.channel, out receivedMsg, out timestamp);

                if (readStatus != PcanStatus.OK)
                {
                    break;
                }

                Debug.WriteLine($"Cleared message with ID 0x{receivedMsg.ID:X}");
            }
        }

        public Tuple<byte, float, float, float> WriteSingleParam(uint index, float value, string format = "f")
        {
            //写入单个参数。  PARAMETERS\RunMods

            //参数:
            //index: 参数索引。
            //value: 要设置的值。
            //format: 数据格式。

            //返回:
            //解析后的接收消息。

            List<byte> encodedData = FormatData(value, format, "encode");
            byte[] data1 = BitConverter.GetBytes(index).Concat(encodedData).ToArray();

            ClearCanRx();

            Tuple<byte[], uint> receivedMsg = SendReceiveCanMessage(CmdModes.SINGLE_PARAM_WRITE, this.MAIN_CAN_ID, data1);

            byte[] received_msg_dat = receivedMsg.Item1;
            uint received_msg_arbitration_id = receivedMsg.Item2;

            if (receivedMsg != null)
            {
                return ParseReceivedMsg(received_msg_dat, received_msg_arbitration_id);
            }
            else
            {
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }

        }

        public Tuple<byte, float, float, float> Disable()
        {
            //停止运行电机。

            //返回:
            //解析后的接收消息。
            ClearCanRx();
            byte[] data1 = { 0, 0, 0, 0, 0, 0, 0, 0 };
            Tuple<byte[], uint> receivedMsg = SendReceiveCanMessage(CmdModes.MOTOR_STOP, this.MAIN_CAN_ID, data1);
            byte[] received_msg_dat = receivedMsg.Item1;
            uint received_msg_arbitration_id = receivedMsg.Item2;
            if (receivedMsg != null)
            {
                return ParseReceivedMsg(received_msg_dat, received_msg_arbitration_id);
            }
            else
            {
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }
        }

        public Tuple<byte, float, float, float> Enable()
        {
            //使能运行电机。

            //返回:
            //解析后的接收消息。
            ClearCanRx();
            byte[] data1 = { };
            Tuple<byte[], uint> receivedMsg = SendReceiveCanMessage(CmdModes.MOTOR_ENABLE, this.MAIN_CAN_ID, data1);
            byte[] received_msg_dat = receivedMsg.Item1;
            uint received_msg_arbitration_id = receivedMsg.Item2;
            if (receivedMsg != null)
            {
                return ParseReceivedMsg(received_msg_dat, received_msg_arbitration_id);
            }
            else
            {
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }
        }

        public Tuple<byte, float, float, float> Set0()
        {
            //设置电机的机械零点。

            //返回:
            //解析后的接收消息。
            ClearCanRx();
            byte[] data1 = { 1 };
            Tuple<byte[], uint> receivedMsg = SendReceiveCanMessage(CmdModes.SET_MECHANICAL_ZERO, this.MAIN_CAN_ID, data1);
            byte[] received_msg_dat = receivedMsg.Item1;
            uint received_msg_arbitration_id = receivedMsg.Item2;
            if (receivedMsg != null)
            {
                return ParseReceivedMsg(received_msg_dat, received_msg_arbitration_id);
            }
            else
            {
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }
        }

        public void SetMotorPositionControl(float limit_spd, float loc_ref)
        {
            //位置控制

            WriteSingleParam(0x7017, limit_spd);
            WriteSingleParam(0x7016, loc_ref);
        }

        public Tuple<byte, float, float, float> SendMotorControlCommand(float torque, float target_angle, float target_velocity, float Kp, float Kd)
        {
            //运控模式下发送电机控制指令。

            //参数:
            //torque: 扭矩。
            //target_angle: 目标角度。
            //target_velocity: 目标速度。
            //Kp: 比例增益。
            //Kd: 导数增益。

            //生成29位的仲裁ID的组成部分
            uint cmd_mode = CmdModes.MOTOR_CONTROL;
            uint torque_mapped = LinearMapping(torque, -12.0F, 12.0F);
            uint data2 = torque_mapped;

            //将实际值映射到消息值
            //float min_abgle = -4 * Math.PI;
            uint target_angle_mapped = LinearMapping(target_angle, -4 * Math.PI, 4 * Math.PI);
            uint target_velocity_mapped = LinearMapping(target_velocity, -30.0F, 30.0F);
            uint Kp_mapped = LinearMapping(Kp, 0.0F, 500.0F);
            uint Kd_mapped = LinearMapping(Kd, 0.0F, 5.0F);

            //创建8字节的数据区
            byte[] data1_bytes = new byte[8];
            Array.Copy(BitConverter.GetBytes(target_angle_mapped), 0, data1_bytes, 0, 2);
            Array.Copy(BitConverter.GetBytes(target_velocity_mapped), 0, data1_bytes, 2, 2);
            Array.Copy(BitConverter.GetBytes(Kp_mapped), 0, data1_bytes, 4, 2);
            Array.Copy(BitConverter.GetBytes(Kd_mapped), 0, data1_bytes, 6, 2);
            List<byte> data1 = new List<byte>();

            //for (int j = 0; j < data1_bytes.Length; j++)
            //{
            //    data1.Add(data1_bytes[j]);
            //}

            //send canmessage
            Tuple<byte[], uint> receivedMsg = SendReceiveCanMessage(cmd_mode, data2, data1_bytes);

            byte[] received_msg_dat = receivedMsg.Item1;
            uint received_msg_arbitration_id = receivedMsg.Item2;

            if (receivedMsg != null)
            {
                return ParseReceivedMsg(received_msg_dat, received_msg_arbitration_id);
            }
            else
            {
                return new Tuple<byte, float, float, float>(0, 0, 0, 0);
            }

        }
        
    }
}

