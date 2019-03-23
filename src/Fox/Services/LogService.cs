using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;

namespace Fox.Services
{
    public sealed class LogService
    {
        private readonly SemaphoreSlim _semaphore;

        public LogService()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        public void Print(LogLevel level, string name, string message)
        {
            PrintAsync(level, name, message).GetAwaiter().GetResult();
        }

        public async Task PrintAsync(LogLevel level, string name, string message)
        {
            await _semaphore.WaitAsync();

            try
            {
                switch (level)
                {
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        break;
                    case LogLevel.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        break;
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    case LogLevel.Critical:
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.Black;
                        break;
                }

                Console.Write($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] [{level}] [{name}]");
                Console.ResetColor();
                Console.WriteLine($" {message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
