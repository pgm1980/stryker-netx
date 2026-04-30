using System;
using System.IO;
using System.Text;
using McMaster.Extensions.CommandLineUtils;
using Spectre.Console;

namespace Stryker.CLI;

internal sealed class ConsoleWrapper : IConsole
{
    private readonly IAnsiConsole _console;

    public ConsoleWrapper(IAnsiConsole console)
    {
        _console = console;
        Out = new OutWriter(this);
        Error = new OutWriter(this);
        In = TextReader.Null;
    }

    public void ResetColor() => throw new NotSupportedException();

    public TextWriter Out { get; init; }
    public TextWriter Error { get; init; }
    public TextReader In { get; }
    public bool IsInputRedirected { get; }
    public bool IsOutputRedirected { get; }
    public bool IsErrorRedirected { get; }
    public ConsoleColor ForegroundColor { get; set; }
    public ConsoleColor BackgroundColor { get; set; }

    // Required by IConsole interface, never raised by this implementation.
#pragma warning disable CS0067 // Event is never used — interface contract requires this declaration
    public event ConsoleCancelEventHandler? CancelKeyPress;
#pragma warning restore CS0067

    private sealed class OutWriter(ConsoleWrapper host) : TextWriter
    {
        private readonly ConsoleWrapper _host = host;

        public override Encoding Encoding => _host._console.Profile.Encoding;

        public override void Write(char value)
        {
            if (value == '\r')
            {
                // Spectre adds a '\r' automatically
                return;
            }
            _host._console.Write(value.ToString());
        }
    }
}
