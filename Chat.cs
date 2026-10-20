using ProjectM;
using Unity.Collections;

namespace vrising_server_monitor;

internal static class Chat
{
    public static void Broadcast(string message)
    {
        var text = new FixedString512Bytes(message);
        ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, ref text);
    }
}