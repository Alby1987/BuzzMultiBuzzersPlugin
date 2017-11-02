using System;
using System.Linq;
using HIDInterface;
using System.Collections;
using System.Threading;

namespace BuzzPluginDriver
{
    public class Buzzers
    {
        byte[][] buzzersBuffersLastIn = new byte[2][];
        byte[][] buzzersBuffersOut = new byte[2][];
        HIDDevice[] buzzers = new HIDDevice[2];
        object[] locks = { new object(), new object() };
        int[] irqread = { 0, 0 };
        bool[] read = { false, false, false };
        long lastIrq;
        //System.IO.StreamWriter file = new System.IO.StreamWriter(@"F:\logbuzz.txt");

        private volatile Queue[] readDataQueues = { Queue.Synchronized(new Queue()), Queue.Synchronized(new Queue()) };
        private enum IrqState
        {
            unplugged,
            check,
            on
        }
        private IrqState[] irqstat = { IrqState.unplugged, IrqState.unplugged };

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
            buzzersBuffersLastIn[0] = new byte[6];
            buzzersBuffersOut[0] = new byte[8];
            buzzersBuffersLastIn[1] = new byte[6];
            buzzersBuffersOut[1] = new byte[8];

            if (buzzersFound.Count > 0)
            {
                buzzers[0] = new HIDDevice(buzzersFound[0].devicePath, false);
                buzzersBuffersOut[0] = new byte[buzzers[0].productInfo.OUT_reportByteLength];
                irqstat[0] = IrqState.check;
                Thread readOne = new Thread(this.ReadThread0);
                readOne.Start();
                if (buzzersFound.Count > 1)
                {
                    buzzers[1] = new HIDDevice(buzzersFound[1].devicePath, false);
                    buzzersBuffersOut[1] = new byte[buzzers[1].productInfo.OUT_reportByteLength];
                    irqstat[1] = IrqState.check;
                    Thread readTwo = new Thread(this.ReadThread1);
                    readTwo.Start();
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
            lock (locks[buzzerNumber])
            {
                read[buzzerNumber] = true;
                data[0] = 0x7F;
                data[1] = 0x7F;
                data[5] = 0xFD;
                if (irqstat[buzzerNumber] == IrqState.check) irqstat[buzzerNumber] = IrqState.on;
                if ((buzzerNumber == 1 && buzzers[1] == null) ||
                   (buzzerNumber == 0 && buzzers[0] == null))
                {
                    data[2] = 0x00;
                    data[3] = 0x00;
                    data[4] = 0xF0;
                    //file.WriteLine(buzzerNumber + ";read failed;" + irqread[buzzerNumber] + ";" + toread[buzzerNumber] + ";0");
                    return 16;
                }
                if (readDataQueues[buzzerNumber].Count < 1)
                {
                    data[2] = buzzersBuffersLastIn[buzzerNumber][3];
                    data[3] = buzzersBuffersLastIn[buzzerNumber][4];
                    data[4] = ((byte)(buzzersBuffersLastIn[buzzerNumber][5]|0xF0));
                    //file.WriteLine(buzzerNumber + ";last read;" + irqread[buzzerNumber] + ";" + toread[buzzerNumber] + ";0");
                    return 16;
                }
                var actualmessage = (byte[])readDataQueues[buzzerNumber].Dequeue();
                buzzersBuffersLastIn[buzzerNumber] = actualmessage;
                data[2] = actualmessage[3];
                data[3] = actualmessage[4];
                data[4] = (byte)(actualmessage[5]|0xF0);
                //file.WriteLine(buzzerNumber + ";new read;" + irqread[buzzerNumber] + ";" + toread[buzzerNumber] + ";2");
                return 16;
            }
        }

        public void WriteBuzzer(ref byte[] data, int buzzerNumber)
        {
            if (buzzerNumber == 0 && buzzers[0] == null) return;
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

        public Int32 getIrq(int buzzerNumber)
        {
            lock (locks[buzzerNumber])
            {
                if (irqstat[buzzerNumber] == IrqState.check) return 1;
                if (irqstat[buzzerNumber] == IrqState.on)
                {
                    if (read[2] == true)
                    {
                        if (read[0] == true && read[1] == true)
                        {
                            read[2] = false;
                            lastIrq = DateTime.Now.Ticks;
                            //file.WriteLine(buzzerNumber + ";request irq;" + irqread[buzzerNumber] + ";2;" + toread[buzzerNumber]);
                            return 1;
                        }
                        return 1;
                    }
                    if (read[2] == false && irqread[buzzerNumber] > 0)
                    {
                        read[0] = false;
                        read[1] = false;
                        if (DateTime.Now.Ticks - lastIrq < 1000000) return 0;
                        irqread[buzzerNumber] -= 1;
                        read[2] = true;
                        //file.WriteLine(buzzerNumber + ";request irq;" + irqread[buzzerNumber] + ";0;" + toread[buzzerNumber]);
                        return 1;
                    }
                }
            }
            return 0;
        }

        public void ReadThread0()
        {
            while(true)
            {
                readDataQueues[0].Enqueue(buzzers[0].read());
                lock (locks[0])
                {
                    irqread[0] += 1;
                    //file.WriteLine("0;Add read q;" + irqread[0] + ";" + toread[0]);
                }
            }
        }

        public void ReadThread1()
        {
            while (true)
            {
                readDataQueues[1].Enqueue(buzzers[1].read());
                lock (locks[1])
                {
                    irqread[1] += 1;
                    //file.WriteLine("1;Add read q;" + irqread[1] + ";" + toread[1]);
                }
            }
        }
    }
}