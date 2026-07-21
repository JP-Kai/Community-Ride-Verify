using System.Collections.Concurrent;

namespace RideSafeSA.Bot.Conversations;

// In-memory only: state resets if the bot restarts, and this won't work
// if more than one bot instance ever runs at once. Fine for an MVP with
// a handful of users; a real deployment would move this to something
// shared (a database table, Redis, etc.).
public class ConversationStore
{
    private readonly ConcurrentDictionary<long, ConversationContext> _contexts = new();

    public ConversationContext GetOrCreate(long chatId) =>
        _contexts.GetOrAdd(chatId, _ => new ConversationContext());
}
