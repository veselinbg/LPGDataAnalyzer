using System;
using System.IO;
using System.Text;
using System.Threading;
using FTD2XX_NET;

public class FtdiLogger
{
    private FTDI ftdi = new FTDI();
    private Thread workerThread;
    private bool isRunning = false;

    public void Start(int deviceIndex = 0)
    {
        if (isRunning)
            return;

        isRunning = true;

        workerThread = new Thread(() => Run(deviceIndex));
        workerThread.IsBackground = true;
        workerThread.Start();
    }

    public void Stop()
    {
        isRunning = false;

        if (workerThread != null && workerThread.IsAlive)
        {
            workerThread.Join(); // wait for clean exit
        }

        try
        {
            ftdi.Close();
        }
        catch { }

        Console.WriteLine("Logger stopped.");
    }

    private void Run(int deviceIndex)
    {
        var status = ftdi.OpenByIndex((uint)deviceIndex);

        if (status != FTDI.FT_STATUS.FT_OK)
        {
            Console.WriteLine("Failed to open FTDI device");
            isRunning = false;
            return;
        }

        Console.WriteLine("Device connected!");

        // Configure device
        ftdi.SetBaudRate(115200);
        ftdi.SetDataCharacteristics(
            FTDI.FT_DATA_BITS.FT_BITS_8,
            FTDI.FT_STOP_BITS.FT_STOP_BITS_1,
            FTDI.FT_PARITY.FT_PARITY_NONE);

        ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0x00, 0x00);
        ftdi.SetTimeouts(5000, 5000);

        // Safe filename
        string filePath = $"ftdi_log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";

        try
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
            {
                byte[] buffer = new byte[4096];

                Console.WriteLine("Logging started...");

                while (isRunning)
                {
                    uint bytesAvailable = 0;
                    ftdi.GetRxBytesAvailable(ref bytesAvailable);

                    if (bytesAvailable > 0)
                    {
                        if (bytesAvailable > buffer.Length)
                            bytesAvailable = (uint)buffer.Length;

                        uint bytesRead = 0;
                        ftdi.Read(buffer, bytesAvailable, ref bytesRead);

                        string data = Encoding.ASCII.GetString(buffer, 0, (int)bytesRead);

                        Console.Write(data);

                        writer.Write(data);
                        writer.Flush();
                    }

                    Thread.Sleep(20);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            ftdi.Close();
            Console.WriteLine("Device closed.");
        }
    }
}