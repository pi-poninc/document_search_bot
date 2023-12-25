using System.Threading.Tasks;
using Chatbot.Database;

namespace Chatbot.Services;

public interface ILogService
{
    Task SaveConversationAsync(Message message);
    Task SaveFeedbackAsync(Feedback feedback);
}
