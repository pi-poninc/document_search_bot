namespace Chatbot.Services;

public class CosmosDBOptions
{
    public  string Endpoint { get; set; }
    public  string Key { get; set; }
    public  string DatabaseId { get; set; }
    public  string MessageContainerId { get; set; }
    public  string FeedbackContainerId { get; set; }
}
