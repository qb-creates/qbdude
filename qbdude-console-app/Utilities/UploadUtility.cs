using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using qbdude.exceptions;
using qbdude.invocation.results;
using qbdude.Models;
using qbdude.ui;
using Console = qbdude.ui.Console;

namespace qbdude.utilities;

/// <summary>
/// Utility that is used to upload program data to a microcontroller.
/// </summary>
public static class UploadUtility
{
    private const string READY_TO_UPDATE_AKNOWLEDGEMENT = "CTU";
    // private const string COMPLETE_AKNOWLEDGEMENT = "Complete";
    private const string PAGE_AKNOWLEDGEMENT = "Page";
    private const char BYTE_AKNOWLEDGEMENT = '\r';
    private const byte END_OF_PAGE_BTYE = 0xFF;
    private const byte LAST_PAGE_BYTE = 0xFE;

    private static Queue<List<byte>> _pageDataQueue = new Queue<List<byte>>();

    /// <summary>
    /// Will upload the program data to the microcontroller
    /// </summary>
    /// <param name="comPort"></param>
    /// <param name="programData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task UploadProgramData(string comPort, List<byte> programData, Microcontroller mcu, CancellationToken cancellationToken)
    {
        int programDataCount = programData.Count;
        using (SerialPort serialPort = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One))
        {
            OpenComPort(serialPort, cancellationToken);
            BuildPageDataQueue(mcu, programData);
            StartCommunication(serialPort, mcu, programDataCount, cancellationToken);
            await TransmitData(serialPort, mcu, cancellationToken);
        }
    }

    private static void BuildPageDataQueue(Microcontroller mcu, List<byte> programData)
    {
        int pageCount = 0;

        while (programData.Count != 0)
        {
            int lastPageLength = Math.Min(programData.Count, mcu.PageSize);

            // Retrieve
            List<byte> tempByteList = programData.GetRange(0, lastPageLength);

            // Remove tha
            programData.RemoveRange(0, lastPageLength);

            // Fill the byte list with 0xFF until it is equal to the mcuPageSize
            while (tempByteList.Count < mcu.PageSize)
            {
                tempByteList.Add(0xFF);
            }

            // Prepend the page number to the page data. Page is a 16 bit int so we have to separate it into two bytes.
            tempByteList.Insert(0, (byte)pageCount);
            tempByteList.Insert(0, (byte)(pageCount >> 8));

            // Add the ending byte for this page to the list. This byte will inform the mcu that there is more
            byte endingByte = lastPageLength < mcu.PageSize ? LAST_PAGE_BYTE : END_OF_PAGE_BTYE;
            tempByteList.Add(endingByte);

            _pageDataQueue.Enqueue(tempByteList);
            pageCount++;
        }
    }

    private static void OpenComPort(SerialPort serialPort, CancellationToken cancellationToken)
    {
        int openAttempts = 3;
        while (!serialPort.IsOpen)
        {
            Console.Write($"Opening {serialPort.PortName}: {openAttempts} attempts remaining.\r");

            try
            {
                serialPort.Open();
            }
            catch
            {
                --openAttempts;
            }

            if (openAttempts == 0)
            {
                throw new ComPortTimeoutException($"Failed to open {serialPort.PortName}.", new UploadErrorResult(ExitCode.FailedToOpenCom));
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
    private static void StartCommunication(SerialPort serialPort, Microcontroller mcu, int programDataCount, CancellationToken cancellationToken)
    {
        serialPort.ReadTimeout = 100;
        serialPort.Write("RTU\0");

        try
        {
            byte[] signature = new byte[3];
            serialPort.Read(signature, 0, 3);

            if (!mcu.Signature.SequenceEqual(signature))
                throw new CommandException("Signatures do not match", new ErrorResult());

            int highFuseBits = serialPort.ReadByte();
            int mask = highFuseBits & mcu.BootConfigMask;
            
            if (!mcu.BootFlashSizeDictionary.ContainsKey(mask))
                throw new CommandException("Boot Reset not enabled", new ErrorResult());

            if (programDataCount > mcu.FlashSize - mcu.BootFlashSizeDictionary[mask])
            {
                throw new Exception("Selected MCU does not have enough space for this program");
            }

            Console.WriteLine($"{serialPort.PortName} open: Writing flash ({programDataCount} bytes)\r\n");
        }
        catch (TimeoutException) 
        {
            throw new CommunicationFailedException(new UploadErrorResult(ExitCode.CommunicationError));
        }
    }

    private static async Task TransmitData(SerialPort serialPort, Microcontroller mcu, CancellationToken cancellationToken)
    {
        string receivedData = string.Empty;
        int totalBytes = _pageDataQueue.Count * (mcu.PageSize + 3);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        using (ProgressBar progressBar = new ProgressBar("Writing", totalBytes))
        {
            await progressBar.Start();

            while (progressBar.CurrentPercentage != 100)
            {
                cancellationToken.ThrowIfCancellationRequested();
                receivedData += serialPort.ReadExisting();

                if (receivedData.Contains(BYTE_AKNOWLEDGEMENT))
                {
                    var byteAckLength = receivedData.Count(f => f == BYTE_AKNOWLEDGEMENT);

                    progressBar.Update(byteAckLength);
                    receivedData = receivedData.Replace(BYTE_AKNOWLEDGEMENT, '\x00');
                    stopwatch.Restart();
                }

                if (receivedData.Contains(READY_TO_UPDATE_AKNOWLEDGEMENT) || receivedData.Contains(PAGE_AKNOWLEDGEMENT))
                {
                    if (_pageDataQueue.Count > 0)
                    {
                        byte[] data = _pageDataQueue.Dequeue().ToArray();
                        serialPort.Write(data, 0, data.Length);
                    }

                    receivedData = string.Empty;
                    stopwatch.Restart();
                }

                if (stopwatch.Elapsed.Seconds > 5)
                {
                    throw new CommunicationFailedException(new UploadErrorResult(ExitCode.CommunicationError));
                }
            }
        }
    }
}