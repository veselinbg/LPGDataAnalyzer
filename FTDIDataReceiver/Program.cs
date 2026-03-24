using System;
using System.IO;
using System.Text;
using System.Threading;
using FTD2XX_NET;

class Parser
{
    public static void Read(string filename)
    {
        string[] lines = File.ReadAllLines(filename);

        foreach (string line in lines)
        {
            if (line.StartsWith("TEMP:"))
            {
                double temp = double.Parse(line.Replace("TEMP:", ""));
                Console.WriteLine($"Temperature: {temp} °C");
            }
            else if (line.StartsWith("HUM:"))
            {
                double hum = double.Parse(line.Replace("HUM:", ""));
                Console.WriteLine($"Humidity: {hum} %");
            }
        }
    }
    public static void read2(string filename)
    {
        byte[] data = File.ReadAllBytes(filename);

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == 0xAA)
            {
                int length = data[i + 1];
                byte[] payload = new byte[length];

                Array.Copy(data, i + 2, payload, 0, length);

                Console.WriteLine("Packet: " + BitConverter.ToString(payload));

                i += length + 2;
            }
        }
    }
    }

class FTDI_Logger
{
    static void Main()
    {
        FTDI ftdi = new();
        uint deviceCount = 0;
        try
        {
            // Get number of devices
            ftdi.GetNumberOfDevices(ref deviceCount);

            if (deviceCount == 0)
                return;

            // Create device list
            FTDI.FT_DEVICE_INFO_NODE[] deviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];

            ftdi.GetDeviceList(deviceList);

            for (uint i = 0; i < deviceCount; i++)
            {
                Console.WriteLine($"Index: {i}");
                Console.WriteLine($"  Flags: {deviceList[i].Flags}");
                Console.WriteLine($"  Type: {deviceList[i].Type}");
                Console.WriteLine($"  ID: {deviceList[i].ID}");
                Console.WriteLine($"  LocId: {deviceList[i].LocId}");
                Console.WriteLine($"  Serial: {deviceList[i].SerialNumber}");
                Console.WriteLine($"  Description: {deviceList[i].Description}");
                Console.WriteLine();
            }
        }
        finally
        {
            ftdi.Close();
        }
       
        var logger = new FtdiLogger();

        logger.Start(0);

        Console.WriteLine("Press ENTER to stop...");
        Console.ReadLine();

        logger.Stop();
    }
}