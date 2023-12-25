using System;

namespace Chatbot.Database;

public class Message
{
    public string MessageId { get; set; }
    public DateTime Timestamp { get; set; }
    public string AadObjectId { get; set; }
    public string ConversationId { get; set; }
    public string TextMessage { get; set; }
    public bool IsUser { get; set; }

}
