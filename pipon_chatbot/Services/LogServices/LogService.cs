using System;
using System.Threading.Tasks;
using Chatbot.Database;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Chatbot.Services;

public class LogService : ILogService
{
    private readonly Container _cosmosLogContainer;
    private readonly Container _cosmosFeedbackContainer;
    private readonly ILogger<LogService> _logger;

    public LogService(Container cosmosLogContainer, Container cosmosFeedbackContainer, ILogger<LogService> logger)
    {
        _cosmosLogContainer = cosmosLogContainer;
        _cosmosFeedbackContainer = cosmosFeedbackContainer;
        _logger = logger;
    }

    public async Task SaveConversationAsync(Message message)
    {
        var document = new
        {
            id = Guid.NewGuid().ToString(),
            MessageId = message.MessageId,
            Timestamp = message.Timestamp,
            AadObjectId = message.AadObjectId,
            ConversationId = message.ConversationId,
            TextMessage = message.TextMessage,
            IsUser = message.IsUser
        };

        await _cosmosLogContainer.CreateItemAsync(document);
    }

    public async Task SaveFeedbackAsync(Feedback feedback)
    {
        var document = new
        {
            id = Guid.NewGuid().ToString(),
            FeedbackId = feedback.FeedbackId,
            Timestamp = feedback.Timestamp,
            ConversationId = feedback.ConversationId,
            IsGoodFeedback = feedback.IsGoodFeedback
        };

        await _cosmosFeedbackContainer.CreateItemAsync(document);
        
    }
}
