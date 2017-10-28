using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidUtilityNuget;

namespace BuzzPluginDriver
{
    public class Buzzers
    {
        byte[][] buzzersBuffersIn = new byte[2][];
        byte[][] buzzersBuffersOut = new byte[2][];
        HidUtility[] buzzers = new HidUtility[2];
        private object[] lockIn = new[] { new object(), new object() };
        private object[] lockOut = new[] { new object(), new object() };
        

        public void USBinit()
        {
            buzzers[0] = new HidUtility();
            var devs = buzzers[0].DeviceList;
            var buzzersFound = devs.Where((hiddev) => (((hiddev.Pid.ToString("X4") == "1000") ||
                                                        (hiddev.Pid.ToString("X4") == "0002")) &&
                                                        (hiddev.Vid.ToString("X4") == "054C"))).ToList();
            if (buzzersFound.Count > 0)
            {
                buzzersBuffersIn[0] = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                buzzersBuffersOut[0] = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                buzzersBuffersIn[1] = new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                buzzersBuffersOut[1] = new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                buzzers[0].SelectDevice(buzzersFound[0]);
                buzzers[0].RaiseSendPacketEvent += SendPacketHandler0;
                buzzers[0].RaisePacketSentEvent += PacketSentHandler0;
                buzzers[0].RaiseReceivePacketEvent += ReceivePacketHandler0;
                buzzers[0].RaisePacketReceivedEvent += PacketReceivedHandler0;
                if (buzzersFound.Count > 1)
                {
                    buzzers[1] = new HidUtility();
                    buzzers[1].SelectDevice(buzzersFound[1]);
                    buzzers[1].RaiseSendPacketEvent += SendPacketHandler1;
                    buzzers[1].RaisePacketReceivedEvent += PacketReceivedHandler1;
                }
            }
        }

        public void USBclose()
        {
            buzzers[0]?.CloseDevice();
            buzzers[1]?.CloseDevice();
        }

        public void USBshutdown()
        {
            
        }

        public int ReadBuzzer(ref byte[] data, int buzzerNumber)
        {
            lock (lockIn[buzzerNumber])
            {
                data[0] = 0x00;
                data[1] = 0x00;
                data[2] = buzzersBuffersIn[buzzerNumber][3];
                data[3] = buzzersBuffersIn[buzzerNumber][4];
                data[4] = buzzersBuffersIn[buzzerNumber][5];
                data[5] = 0x00;
                return 16;
            }
        }

        public void WriteBuzzer(ref byte[] data, int buzzerNumber)
        {
            lock(lockOut[buzzerNumber])
            {
                buzzersBuffersOut[buzzerNumber][0] = 0x00;
                buzzersBuffersOut[buzzerNumber][1] = 0x00;
                buzzersBuffersOut[buzzerNumber][2] = data[1];
                buzzersBuffersOut[buzzerNumber][3] = data[2];
                buzzersBuffersOut[buzzerNumber][4] = data[3];
                buzzersBuffersOut[buzzerNumber][5] = data[4];
                buzzersBuffersOut[buzzerNumber][6] = 0x00;
                buzzersBuffersOut[buzzerNumber][7] = 0x00;
            }
        }

        public void SendPacketHandler0(object sender, UsbBuffer OutBuffer)
        {
            lock(lockOut[0])
            {
                OutBuffer.clear();
                OutBuffer.buffer[0] = buzzersBuffersOut[0][0];
                OutBuffer.buffer[1] = buzzersBuffersOut[0][1];
                OutBuffer.buffer[2] = buzzersBuffersOut[0][2];
                OutBuffer.buffer[3] = buzzersBuffersOut[0][3];
                OutBuffer.buffer[4] = buzzersBuffersOut[0][4];
                OutBuffer.buffer[5] = buzzersBuffersOut[0][5];
                OutBuffer.buffer[6] = buzzersBuffersOut[0][6];
                OutBuffer.buffer[7] = buzzersBuffersOut[0][7];
                OutBuffer.RequestTransfer = true;
            }
        }

        public void SendPacketHandler1(object sender, UsbBuffer OutBuffer)
        {
            lock (lockOut[1])
            {
                OutBuffer.clear();
                OutBuffer.buffer[0] = buzzersBuffersOut[1][0];
                OutBuffer.buffer[1] = buzzersBuffersOut[1][1];
                OutBuffer.buffer[2] = buzzersBuffersOut[1][2];
                OutBuffer.buffer[3] = buzzersBuffersOut[1][3];
                OutBuffer.buffer[4] = buzzersBuffersOut[1][4];
                OutBuffer.buffer[5] = buzzersBuffersOut[1][5];
                OutBuffer.buffer[6] = buzzersBuffersOut[1][6];
                OutBuffer.buffer[7] = buzzersBuffersOut[1][7];
                OutBuffer.RequestTransfer = true;
            }
        }

        public void PacketReceivedHandler0(object sender, UsbBuffer InBuffer)
        {
            lock (lockIn[0])
            {
                buzzersBuffersIn[0][0] = InBuffer.buffer[0];
                buzzersBuffersIn[0][1] = InBuffer.buffer[1];
                buzzersBuffersIn[0][2] = InBuffer.buffer[2];
                buzzersBuffersIn[0][3] = InBuffer.buffer[3];
                buzzersBuffersIn[0][4] = InBuffer.buffer[4];
                buzzersBuffersIn[0][5] = InBuffer.buffer[5];
            }
        }

        public void PacketReceivedHandler1(object sender, UsbBuffer InBuffer)
        {
            lock (lockIn[1])
            {
                buzzersBuffersIn[1][0] = InBuffer.buffer[0];
                buzzersBuffersIn[1][1] = InBuffer.buffer[1];
                buzzersBuffersIn[1][2] = InBuffer.buffer[2];
                buzzersBuffersIn[1][3] = InBuffer.buffer[3];
                buzzersBuffersIn[1][4] = InBuffer.buffer[4];
                buzzersBuffersIn[1][5] = InBuffer.buffer[5];
            }
        }

        public void PacketSentHandler0(object sender, UsbBuffer OutBuffer)
        {
        }

        public void PacketSentHandler1(object sender, UsbBuffer OutBuffer)
        {
        }

        public void ReceivePacketHandler0(object sender, UsbBuffer InBuffer)
        {
            InBuffer.RequestTransfer = true;
        }

        public void ReceivePacketHandler1(object sender, UsbBuffer InBuffer)
        {
            InBuffer.RequestTransfer = true;
        }
    }
}