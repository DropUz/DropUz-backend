using System.Security.Cryptography;
using System.Text;

namespace DropUz.Common.Application.EventBus;

public static class IntegrationEventId
{
    public static Guid Create<TIntegrationEvent>(Guid sourceEventId)
        where TIntegrationEvent : IIntegrationEvent
    {
        string eventTypeName = typeof(TIntegrationEvent).FullName ?? typeof(TIntegrationEvent).Name;
        byte[] typeNameBytes = Encoding.UTF8.GetBytes(eventTypeName);
        byte[] input = new byte[16 + typeNameBytes.Length];
        sourceEventId.TryWriteBytes(input);
        typeNameBytes.CopyTo(input, 16);

        byte[] hash = SHA256.HashData(input);
        return new Guid(hash.AsSpan(0, 16));
    }
}
