using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HIDInterface;

namespace BuzzPluginDriver
{
    public class Buzzers
    {
        byte[][] buzzersBuffersIn = new byte[2][];
        byte[][] buzzersBuffersOut = new byte[2][];
        HIDDevice[] buzzers = new HIDDevice[2];
        object[] locks = new object[2] { new object(), new object()} ;
        
        private Buzzers()
        {

        }

        private static Buzzers SingletonBuzz;
        
        public static Buzzers getBuzzers()
        {
            if (SingletonBuzz == null) SingletonBuzz = new Buzzers();
            return SingletonBuzz;
        }

        public void USBinit()
        {
            HIDDevice.interfaceDetails[] devices = HIDDevice.getConnectedDevices();
            
            var buzzersFound = devices.Where((hiddev) => (((hiddev.PID.ToString("X4") == "1000") ||
                                                        (hiddev.PID.ToString("X4") == "0002")) &&
                                                        (hiddev.VID.ToString("X4") == "054C"))).ToList();
            if (buzzersFound.Count > 0)
            {
                buzzers[0] = new HIDDevice(buzzersFound[0].devicePath, true);
                buzzers[0].dataReceived += Device_dataReceived0;
                buzzersBuffersIn[0] = new byte[buzzers[0].productInfo.IN_reportByteLength];
                buzzersBuffersOut[0] = new byte[buzzers[0].productInfo.OUT_reportByteLength];
                if (buzzersFound.Count > 1)
                {
                    buzzers[1] = new HIDDevice(buzzersFound[1].devicePath, true);
                    buzzers[1].dataReceived += Device_dataReceived1;
                    buzzersBuffersIn[1] = new byte[buzzers[1].productInfo.IN_reportByteLength];
                    buzzersBuffersOut[1] = new byte[buzzers[1].productInfo.OUT_reportByteLength];
                }
            }
        }

        public void USBclose()
        {
            buzzers[0]?.close();
            buzzers[1]?.close();
        }

        public void USBshutdown()
        {
            
        }

        public int ReadBuzzer(ref byte[] data, int buzzerNumber)
        {
            if (buzzerNumber == 1 && buzzers[1] == null)
            {
                data[0] = 0x00;
                data[1] = 0x00;
                data[2] = 0x00;
                data[3] = 0x00;
                data[4] = 0x00;
                data[5] = 0x00;
                return 16;
            }
            data[0] = 0x00;
            data[1] = 0x00;
            lock(locks[buzzerNumber])
            {
                data[2] = buzzersBuffersIn[buzzerNumber][3];
                data[3] = buzzersBuffersIn[buzzerNumber][4];
                data[4] = buzzersBuffersIn[buzzerNumber][5];
            }
            data[5] = 0x00;
            return 16;
        }

        public void WriteBuzzer(ref byte[] data, int buzzerNumber)
        {
            if (buzzerNumber == 1 && buzzers[1] == null) return;
            buzzersBuffersOut[buzzerNumber][0] = 0x00;
            buzzersBuffersOut[buzzerNumber][1] = 0x00;
            buzzersBuffersOut[buzzerNumber][2] = data[1];
            buzzersBuffersOut[buzzerNumber][3] = data[2];
            buzzersBuffersOut[buzzerNumber][4] = data[3];
            buzzersBuffersOut[buzzerNumber][5] = data[4];
            buzzersBuffersOut[buzzerNumber][6] = 0x00;
            buzzersBuffersOut[buzzerNumber][7] = 0x00;
            buzzers[buzzerNumber].write(buzzersBuffersOut[buzzerNumber]);
        }

        private void Device_dataReceived0(byte[] message)
        {
            lock(locks[0])
            {
                buzzersBuffersIn[0][3] = message[3];
                buzzersBuffersIn[0][4] = message[4];
                buzzersBuffersIn[0][5] = message[5];
            }
        }

        private void Device_dataReceived1(byte[] message)
        {
            lock (locks[1])
            {
                buzzersBuffersIn[1][3] = message[3];
                buzzersBuffersIn[1][4] = message[4];
                buzzersBuffersIn[1][5] = message[5];
            }
        }
    }
}