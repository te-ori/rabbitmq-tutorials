using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using static System.Console;

namespace Common;

public static class Defaults
{
    public static AsyncEventingBasicConsumer AsyncEventingBasicConsumer(IChannel channel, string defaultMessage, ConsoleColor fontColor = ConsoleColor.White)
    {
        var handler = new AsyncEventingBasicConsumer(channel);
        handler.ReceivedAsync += async (sender, eventArgs) =>
                {
                    var message = eventArgs.Body.ToMessageString();
                    ForegroundColor = fontColor;
                    WriteLine($"[{defaultMessage}] Received message: {message}");
                    ResetColor();
                    await Task.CompletedTask;
                };
        return handler;
    }
}
