using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class test
    {
        private static void ReadExample()
        {
            PcanChannel channel = PcanChannel.Usb01;

            // The hardware represented by the given handle is initialized with 500 kBit/s bit rate (BTR0/BTR1 0x001C)
            //
            PcanStatus result = Api.Initialize(channel, Bitrate.Pcan500);
            if (result != PcanStatus.OK)
            {
                // An error occurred
                //
                Api.GetErrorText(result, out var errorText);
                Console.WriteLine(errorText);
            }
            else
            {
                // A success message on connection is shown.
                //
                Console.WriteLine($"The hardware represented by the handle {channel} was successfully initialized.");

                // Wait some time to get messages stored in the reception queue of the Channel
                //
                Console.WriteLine("The reception queue will be read out after 1 second...");
                System.Threading.Thread.Sleep(1000);

                // Messages are read and processed until the reception queue is empty
                //
                PcanMessage msg;
                do
                {
                    result = Api.Read(channel, out msg);
                    if (result == PcanStatus.OK)
                    {
                        // Process the received message
                        // 
                        ProcessMessage(msg);
                    }
                    else
                    {
                        if ((result & PcanStatus.ReceiveQueueEmpty) != PcanStatus.ReceiveQueueEmpty)
                        {
                            // An unexpected error occurred
                            //
                            Api.GetErrorText(result, out var errorText);
                            Console.WriteLine("Reading process canceled due to unexpected error: " + errorText);
                            break;
                        }
                    }

                } while ((result & PcanStatus.ReceiveQueueEmpty) != PcanStatus.ReceiveQueueEmpty);

                // The connection to the hardware is finalized when it is no longer needed
                //
                result = Api.Uninitialize(channel);
                if (result != PcanStatus.OK)
                {
                    // An error occurred
                    //
                    Api.GetErrorText(result, out var errorText);
                    Console.WriteLine(errorText);
                }
                else
                    Console.WriteLine($"The hardware represented by the handle {channel} was successfully finalized.");
            }
        }

        // Formats a CAN frame as string and writes it to the console output
        //
        private static void ProcessMessage(PcanMessage msg)
        {
            string msgText = $"Type: {msg.MsgType} | ";
            if ((msg.MsgType & MessageType.Extended) == MessageType.Extended)
                msgText += $"ID: {msg.ID:X8} | ";
            else
                msgText += $"ID: {msg.ID:X4} | ";
            msgText += $"Length: {msg.Length} | ";
            msgText += $"Data: ";
            for (int i = 0; i < msg.Length; i++)
                msgText += $"{msg.Data[i]} ";

            Console.WriteLine(msgText);
        }
    }
}
