using System;
using System.Threading;
using Microsoft.SPOT;
using System.IO;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GHI.Pins;

using Dynamixel.AX12;

namespace Télépeage
{
    public class Servo
    {
        string COMPort;
        SerialPort serial;
        AX12 AX12;
        /*Constructeur d'initialisation du servo
         * */
        public Servo()
        {
            OutputPort direction = new OutputPort((Cpu.Pin)EMX.IO26, false);
            COMPort = GT.Socket.GetSocket(11, true, null, null).SerialPortName;
            serial = new SerialPort(COMPort, 1000000, Parity.None, 8, StopBits.One);
            serial.ReadTimeout = 500;
            serial.WriteTimeout = 500;

            serial.Open();

            if (serial.IsOpen)
                serial.Flush();

            AX12 = new AX12(1, serial, direction);
            AX12.setMode(AX12Mode.joint);
            //TEST
            //AX12.move(1023);
            //AX12.move(0);
        }

        public void openFence()
        {
            AX12.move(0);
            Debug.Print("Barriere ouverte");
        }

        public void closeFence()
        {
            AX12.move(1023);
            Debug.Print("Barriere fermé");
        }
    }
}
