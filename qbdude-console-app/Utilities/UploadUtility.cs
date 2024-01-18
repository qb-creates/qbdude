using System.Diagnostics;
using System.IO.Ports;
using qbdude.exceptions;
using qbdude.invocation.results;
using qbdude.Models;
using qbdude.ui;

namespace qbdude.utilities;

/// <summary>
/// Utility that is used to upload program data to a microcontroller.
/// </summary>
public static class UploadUtility
{
    // Microcontroller commands.
    private const string READY_TO_UPDATE_COMMAND = "RTU\0";

    // Microcontroller acknowledgement responses.
    private const string READY_TO_UPDATE_AKNOWLEDGEMENT = "CTU";
    private const string PAGE_AKNOWLEDGEMENT = "Page";
    private const char BYTE_AKNOWLEDGEMENT = '\r';

    // Page status indicators.
    private const byte PAGE_CONTINUATION_INDICATOR = 0xFF;
    private const byte LAST_PAGE_INDICATOR = 0xFE;

    // Serial Port.
    private const int COMMUNICATION_TIMEOUT = 5;

    private static Queue<List<byte>> _pageDataQueue = new Queue<List<byte>>();
    private static SerialPort _serialPort = new SerialPort();
    private static CancellationToken _cancellationToken;
    private static Microcontroller _selectedMCU = Microcontroller.DeviceDictionary["m128"];
    private static List<byte> _programData = new List<byte>();
    private static int _programDataCount;
    private static bool _forceUpdate;

    /// <summary>
    /// Will upload program data to the microcontroller.
    /// </summary>
    /// <param name="comPort">The comport that will be used to transfer the program data.</param>
    /// <param name="programData">The program data that will be sent to the microcontroller.</param>
    /// <param name="mcu">The type of microcontroller that is being update.</param>
    /// <param name="force">Force the update even if the signatures do not match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
            DefinePageDataQueue();
            StartBootloadProcess();
            await TransmitData();
        }
    }

    /// <summary>
    /// Open the selected comport.
    /// </summary>
    /// <exception cref="ComPortTimeoutException">Exception that is produced when the comport fails to open..</exception>
    private static void OpenComPort()
    {
        ConsoleWrapper.WriteLine($"Opening {_serialPort.PortName}\r\n");

        try
        {
            _serialPort.Open();
        }
        catch
        {
            throw new ComPortTimeoutException($"Failed to open {_serialPort.PortName}.", new UploadErrorResult(ExitCode.FailedToOpenCom));
        }

        _cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Will define a queue where each item in the queue is a list of bytes.
    /// Each list of bytes will contain an entire page of program data. 
    /// The page number is prepended to the front of each list. 
    /// A page status byte is appended to the end of the list. 
    /// The page status byte will indicate if the list of page data is the final or if there is more data to be sent.
    /// </summary>
    private static void DefinePageDataQueue()
    {
        int pageCount = 0;

        while (_programData.Count != 0)
        {
            // Grab an entire page of data or whatever remains
            int dataCount = Math.Min(_programData.Count, _selectedMCU.PageSize);

            // Store the next page of data in a t
            List<byte> tempByteList = _programData.GetRange(0, dataCount);

            // Remove tha
            _programData.RemoveRange(0, dataCount);

            // Fill the temp byte list with 0xFF until it is equal to the mcuPageSize.
            while (tempByteList.Count < _selectedMCU.PageSize)
            {
                tempByteList.Add(0xFF);
            }

            // Prepend the page number to the page data. Page is a 16 bit int so we have to separate it into two bytes.
            tempByteList.Insert(0, (byte)pageCount);
            tempByteList.Insert(0, (byte)(pageCount >> 8));

            // Add the ending byte for this page to the list.
            byte endingByte = _programData.Count != 0 ? PAGE_CONTINUATION_INDICATOR : LAST_PAGE_INDICATOR;
            tempByteList.Add(endingByte);

            _pageDataQueue.Enqueue(tempByteList);
            pageCount++;
        }
    }

    /// <summary>
    /// Will start the bootload process. Will check the device's signature and high fuse bits. If the device's signature
    /// matches the selected part number and there is enough space on the microntroller for the program data, the upload process will begin.
    /// </summary>
    /// <exception cref="DeviceErrorException">Exception that is produced when there is an error with the devices signature or boot configuration.</exception>
    /// <exception cref="ProgramSizeTooLargeException">Exception that is produced when the program data size is too large to fit on the microcontroller.</exception>
    /// <exception cref="CommunicationFailedException">Exception that is produced when communication with the microcontroller is lost.</exception>
    private static void StartBootloadProcess()
    {
        _serialPort.ReadTimeout = 1000;
        _serialPort.Write(READY_TO_UPDATE_COMMAND);

        try
        {
            byte[] signature = new byte[3];
            _serialPort.Read(signature, 0, 3);

            int highFuseBits = _serialPort.ReadByte();
            int bootFlashSize = _selectedMCU.GetBootConfigSize(highFuseBits, out bool bootResetEnabled);

            if (!_selectedMCU.Signature.SequenceEqual(signature) && !_forceUpdate)
            {
                throw new DeviceErrorException("Device signature does not match the part number entered.", new ErrorResult());
            }

            if (!bootResetEnabled)
            {
                throw new DeviceErrorException("Boot Reset bit is not enabled.", new ErrorResult());
            }

            if (_programDataCount > _selectedMCU.FlashSize - bootFlashSize)
            {
                throw new ProgramSizeTooLargeException(new UploadErrorResult(ExitCode.ProgramSizeTooLarge));
            }

            ConsoleWrapper.WriteLine($"{_serialPort.PortName} open: Writing flash ({_programDataCount} bytes)\r\n");
        }
        catch (TimeoutException)
        {
            throw new CommunicationFailedException(new UploadErrorResult(ExitCode.CommunicationError));
        }
    }

    /// <summary>
    /// Will transmit all of the program data to the microcontroller. Each page will be sent to the selected microcontroller one
    /// page at a time. A page acknolowdgement string must be received before the next page is sent. 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="CommunicationFailedException">Exception that is produced when communication with the microcontroller is lost.</exception>
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

                if (stopwatch.Elapsed.TotalSeconds > COMMUNICATION_TIMEOUT)
                {
                    throw new CommunicationFailedException(new UploadErrorResult(ExitCode.CommunicationError));
                }
            }
        }
    }
}