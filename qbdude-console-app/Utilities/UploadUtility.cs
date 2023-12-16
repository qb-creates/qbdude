using System.Diagnostics;
using System.IO.Ports;
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
    private const string PAGE_AKNOWLEDGEMENT = "Page";
    private const char BYTE_AKNOWLEDGEMENT = '\r';
    private const byte END_OF_PAGE_BTYE = 0xFF;
    private const byte LAST_PAGE_BYTE = 0xFE;

    private static Queue<List<byte>> _pageDataQueue = new Queue<List<byte>>();
    private static SerialPort _serialPort = new SerialPort();
    private static CancellationToken _cancellationToken;
    private static Microcontroller _selectedMCU = Microcontroller.DeviceDictionary["m128"];
    private static List<byte> _programData = new List<byte>();
    private static int _programDataCount;
    private static bool _forceUpdate;

    /// <summary>
    /// Will upload the program data to the microcontroller
    /// </summary>
    /// <param name="comPort"></param>
    /// <param name="programData"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task UploadProgramData(string comPort, List<byte> programData, Microcontroller mcu, bool force, CancellationToken cancellationToken)
    {
        _selectedMCU = mcu;
        _programData = programData;
        _programDataCount = programData.Count;
        _forceUpdate = force;
        _cancellationToken = cancellationToken;
        _pageDataQueue.Clear();

        using (_serialPort = new SerialPort(comPort, 115200, Parity.None, 8, StopBits.One))
        {
            OpenComPort();
            BuildPageDataQueue();
            StartBootloadProcess();
            await TransmitData();
        }
    }

    private static void OpenComPort()
    {
        int openAttempts = 3;

        while (!_serialPort.IsOpen)
        {
            Console.Write($"Opening {_serialPort.PortName}: {openAttempts} attempts remaining.\r");

            try
            {
                _serialPort.Open();
            }
            catch
            {
                --openAttempts;
            }

            if (openAttempts == 0)
            {
                throw new ComPortTimeoutException($"Failed to open {_serialPort.PortName}.", new UploadErrorResult(ExitCode.FailedToOpenCom));
            }

            _cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static void BuildPageDataQueue()
    {
        int pageCount = 0;

        while (_programData.Count != 0)
        {
            int lastPageLength = Math.Min(_programData.Count, _selectedMCU.PageSize);

            // Retrieve
            List<byte> tempByteList = _programData.GetRange(0, lastPageLength);

            // Remove tha
            _programData.RemoveRange(0, lastPageLength);

            // Fill the byte list with 0xFF until it is equal to the mcuPageSize
            while (tempByteList.Count < _selectedMCU.PageSize)
            {
                tempByteList.Add(0xFF);
            }

            // Prepend the page number to the page data. Page is a 16 bit int so we have to separate it into two bytes.
            tempByteList.Insert(0, (byte)pageCount);
            tempByteList.Insert(0, (byte)(pageCount >> 8));

            // Add the ending byte for this page to the list. This byte will inform the mcu that there is more
            byte endingByte = lastPageLength < _selectedMCU.PageSize ? LAST_PAGE_BYTE : END_OF_PAGE_BTYE;
            tempByteList.Add(endingByte);

            _pageDataQueue.Enqueue(tempByteList);
            pageCount++;
        }
    }

    private static void StartBootloadProcess()
    {
        _serialPort.ReadTimeout = 100;
        _serialPort.Write("RTU\0");

        try
        {
            byte[] signature = new byte[3];
            _serialPort.Read(signature, 0, 3);
            
            int highFuseBits = _serialPort.ReadByte();
            int bootFlashSize = _selectedMCU.GetBootConfigSize(highFuseBits, out bool bootResetEnabled);

            if (!_selectedMCU.Signature.SequenceEqual(signature) && !_forceUpdate)
            {
                throw new CommandException("Signatures do not match", new ErrorResult());
            }

            if (!bootResetEnabled)
            {
                throw new CommandException("Boot Reset not enabled", new ErrorResult());
            }

            if (_programDataCount > _selectedMCU.FlashSize - bootFlashSize)
            {
                throw new ProgramSizeTooLargeException(new UploadErrorResult(ExitCode.ProgramSizeTooLarge));
            }

            Console.WriteLine($"{_serialPort.PortName} open: Writing flash ({_programDataCount} bytes)\r\n");
        }
        catch (TimeoutException)
        {
            throw new CommunicationFailedException(new UploadErrorResult(ExitCode.CommunicationError));
        }
    }

    private static async Task TransmitData()
    {
        string receivedData = string.Empty;
        int totalBytes = _pageDataQueue.Count * (_selectedMCU.PageSize + 3);

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        using (ProgressBar progressBar = new ProgressBar("Writing", totalBytes))
        {
            await progressBar.Start();

            while (progressBar.CurrentPercentage != 100)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                receivedData += _serialPort.ReadExisting();

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
                        _serialPort.Write(data, 0, data.Length);
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