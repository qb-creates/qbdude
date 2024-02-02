using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace QBdude.UI;

/// <summary>
/// Static wrapper for the System.Console class. 
/// Has custom Write/WriteLine methods where the text color and background color can be set.
/// Has custom method for disabling the ability to resize the console window.
/// 
/// For more information on enabling/disabling certain features in the console window visit https://learn.microsoft.com/en-us/windows/win32/api/winuser/
/// </summary>
public static class ConsoleWrapper
{
    private const int MF_BYCOMMAND = 0x00000000;
    private const int SC_MAXIMIZE = 0xF030;
    private const int SC_SIZE = 0xF000;

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    static ConsoleColor defaultTextColor = ConsoleColor.Black;
    static ConsoleColor defaultBackgroundColor = ConsoleColor.Black;

    static ConsoleWrapper()
    {
        defaultTextColor = Console.ForegroundColor;
        defaultBackgroundColor = Console.BackgroundColor;
    }

    /// <summary>
    /// Resize the console window to where all content printed to the console can be scene.
    /// </summary>
    /// <param name="windowWidth">The width of the console window.</param>
    public static void ResizeConsoleWindow(int windowWidth)
    {
        Console.CursorVisible = false;

        if (OperatingSystem.IsWindows())
        {
            if (Console.WindowWidth < windowWidth)
            {
                Console.BufferWidth = windowWidth;
                Console.WindowWidth = windowWidth;
            }
        }
    }

    /// <summary>
    /// Will disable the resize and feature of the console window.
    /// </summary>
    public static void DisableResizeMenuOptions()
    {
        IntPtr handle = GetConsoleWindow();
        IntPtr sysMenu = GetSystemMenu(handle, false);

        if (handle != IntPtr.Zero)
        {
            DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
            DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);//resize
        }
    }

    /// <summary>
    /// Resets the menu for the console back to it's default state. Enables the resize feature of the console window.
    /// </summary>
    public static void ResetConsoleMenu()
    {
        IntPtr handle = GetConsoleWindow();
        GetSystemMenu(handle, true);
        Console.CursorVisible = true;
    }

    /// <summary>
    /// Gets or sets the row position of the cursor within the buffer area.
    /// </summary>
    /// <returns>The current position, in rows, of the cursor.</returns>
    public static int CursorRowPosition
    {
        get
        {
            return Console.CursorTop;
        }
        set
        {
            Console.CursorTop = value;
        }
    }

    /// <summary>
    /// Gets or sets the column position of the cursor within the buffer area.
    /// </summary>
    /// <returns>The current position, in columns, of the cursor.</returns>
    public static int CursorColumnPosition
    {
        get
        {
            return Console.CursorLeft;
        }
        set
        {
            Console.CursorLeft = value;
        }
    }

    /// <summary>
    /// Sets the position of the cursor.
    /// </summary>
    /// <param name="left">The column position of the cursor. Columns are numbered from left to right starting at 0.</param>
    /// <param name="top">The row position of the cursor. Rows are numbered from top to bottom starting at 0.</param>
    public static void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    /// <summary>
    /// Writes the current line terminator to the standard output stream.
    /// </summary>
    public static void WriteLine()
    {
        Console.WriteLine();
    }

    /// <summary>
    /// Writes the specified string value, followed by the current line terminator, to the standard output stream. 
    /// </summary>
    /// <param name="value">The value to write.</param>
    public static void WriteLine(string value)
    {
        Write(value);
        Console.Write("\n\r");
    }

    /// <summary>
    /// Writes the specified string value to the standard output stream. A text color and background color can
    /// be set for a string by wrapping it in a custom <c:"textColor"></c:"backgroundColor"> element. 
    /// Example: This is the color <c:red>Red with a green background</c:green>
    /// The colors have to be one of the selectable console colors.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public static void Write(string value)
    {
        var colorElements = Regex.Matches(value, @"<c:([a-zA-Z]+)?>.*?</c:([a-zA-Z]+)?>").Select(match => match.Value).ToArray();

        if (colorElements.Length == 0)
        {
            Console.Write(value);
            return;
        }

        var textColors = colorElements.Select(element => element.Substring(3, element.IndexOf(">") - 3)).ToArray();
        var backgroundColors = ParseColorElement(colorElements, "</c:", ">");
        var extractedText = ParseColorElement(colorElements, ">", "</c:");

        int startingSubstring = 0;

        for (int i = 0; i < extractedText.Length; i++)
        {
            var extractedTextIndex = value.IndexOf(colorElements[i]);
            value = value.Replace(colorElements[i], extractedText[i]);

            Console.Write(value.Substring(startingSubstring, extractedTextIndex - startingSubstring));

            if (Enum.TryParse(textColors[i], true, out ConsoleColor textColor))
            {
                Console.ForegroundColor = textColor;
            }

            if (Enum.TryParse(backgroundColors[i], true, out ConsoleColor backgroundColor))
            {
                Console.BackgroundColor = backgroundColor;
            }

            Console.Write(extractedText[i]);
            Console.ForegroundColor = defaultTextColor;
            Console.BackgroundColor = defaultBackgroundColor;

            startingSubstring = extractedTextIndex + extractedText[i].Length;
        }

        Console.Write(value.Substring(startingSubstring));
    }

    private static string[] ParseColorElement(string[] colorElements, string firstValue, string secondValue)
    {
        return colorElements.Select(element =>
        {
            int startingIndex = element.IndexOf(firstValue) + firstValue.Length;
            int endingIndex = element.IndexOf(secondValue, startingIndex) - startingIndex;
            return element.Substring(startingIndex, endingIndex);
        }).ToArray();
    }
}