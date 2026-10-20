using VampireCommandFramework;

namespace vrising_server_monitor.Commands;

public static class HelloCommands
{
    [Command("hello")]
    public static void Hello(ICommandContext ctx)
    {
        ctx.Reply("Hello from the server monitor");
    }
}