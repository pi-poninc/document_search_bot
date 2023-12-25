using System;

namespace Chatbot.Database;

public class Feedback
{
    public  string FeedbackId { get; set; }
    public  DateTime Timestamp { get; set; }
    public  string ConversationId { get; set; }
    public  bool IsGoodFeedback { get; set; }
}
