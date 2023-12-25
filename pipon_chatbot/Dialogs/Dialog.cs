using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Chatbot.Database;
using Chatbot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Chatbot.Dialogs
{
    public class Dialog : ComponentDialog
    {
        private static List<object> conversationHistory = new List<object>();
        private static readonly HttpClient httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogService _logService;
        private string _conversationId;
        private string _aadObjectId;

        public Dialog(UserState userState, IConfiguration configuration, ILogService logService)
            : base(nameof(Dialog))
        {
            _configuration = configuration;
            _logService = logService;

            var waterfallSteps = new WaterfallStep[]
            {
                AskToUserAsync,
                SendAnswerAsync,
                FeedbackAsync,
            };
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        static Dialog()
        {
            httpClient = new HttpClient();
        }

        private async Task<DialogTurnResult> AskToUserAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.ChannelId == "msteams")
            {
                // ユーザーの情報を取得
                var member = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id, cancellationToken);
                // AadObjectId
                _aadObjectId = member.AadObjectId;
            } else {
                _aadObjectId = "testId";
            }

            // CosmosDB用の会話IDを初期化する
            _conversationId = Guid.NewGuid().ToString();

            // ユーザーからのメッセージが空の場合や文字列以外の場合は質問文の入力を促す
            if (string.IsNullOrWhiteSpace(stepContext.Context.Activity.Text) || !stepContext.Context.Activity.Text.Any(char.IsLetterOrDigit))
            {
                var options = new PromptOptions
                {
                    Prompt = MessageFactory.Text("質問を入力してください。"),
                    RetryPrompt = MessageFactory.Text("質問文が認識できませんでした。再入力をお願いします。"),
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await stepContext.NextAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<DialogTurnResult> SendAnswerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userMessage = stepContext.Context.Activity.Text;
            // ユーザーの発言をCosmosDBに保存
            var userMessageLog = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                AadObjectId = _aadObjectId,
                ConversationId = _conversationId,
                TextMessage = userMessage,
                IsUser = true
            };
            await _logService.SaveConversationAsync(userMessageLog);

            // ユーザーの発言を会話履歴に追加
            conversationHistory.Add(new { user = userMessage });

            // FastAPIエンドポイントにGETリクエストを送信
            var content = new StringContent(JsonConvert.SerializeObject(new { messages = conversationHistory }), Encoding.UTF8, "application/json");
            var fastAPIEndpoint = _configuration["FastAPIEndpoint"];
            var response = await httpClient.PostAsync(fastAPIEndpoint, content);

            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var jsonDocument = await JsonDocument.ParseAsync(responseStream);

                if (jsonDocument.RootElement.TryGetProperty("bot", out var botProperty))
                {
                    var botMessage = "";
                    var bot = botProperty.GetString();
                    if (jsonDocument.RootElement.TryGetProperty("metadata", out var metadataProperty))
                    {
                        var metadataArray = metadataProperty.EnumerateArray();
                        var notionURLs = new List<string>();

                        foreach (var metadataElement in metadataArray)
                        {
                            if (metadataElement.TryGetProperty("notion_id", out var notionIdProperty))
                            {
                                var notionURL = notionIdProperty.GetString();
                                var notionURLMarkdown = $"[{notionURL}]({notionURL})";
                                notionURLs.Add(notionURLMarkdown);
                            }
                        }

                        var joinedNotionIds = string.Join("\n\n", notionURLs);

                        botMessage = $"{bot}\n\n{joinedNotionIds}";
                    }
                    // ボットの発言を会話履歴に追加
                    conversationHistory.Add(new { bot = botMessage });
                    // ボットの発言を返信
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(botMessage), cancellationToken);
                    // ボットの発言をCosmosDBに保存
                    var botMessageLog = new Message
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        Timestamp = DateTime.Now,
                        AadObjectId = _aadObjectId,
                        ConversationId = _conversationId,
                        TextMessage = botMessage,
                        IsUser = false
                    };
                    await _logService.SaveConversationAsync(botMessageLog);

                } else {
                    // botプロパティがない場合はエラーを返信
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Error: bot property not found"), cancellationToken);
                }
            }
            else
            {
                // FastAPIエンドポイントからのレスポンスがエラーの場合はエラーを返信
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Error: {response.StatusCode}"), cancellationToken);
            }

            // ユーザーのフィードバックを確認
            return await stepContext
                .PromptAsync(nameof(ConfirmPrompt),
                    new()
                    {
                        Prompt = MessageFactory.Text("こちらの回答で解決しましたか？"),
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<DialogTurnResult> FeedbackAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var isGoodFeedback = (bool)stepContext.Result;

            if (isGoodFeedback)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("お役に立てて光栄です。また何でも聞いてください。"), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("お探しの情報が見つからずに申し訳ありません。"), cancellationToken);
            }
            // ユーザーのフィードバックをCosmosDBに保存
            var feedback = new Feedback
            {
                FeedbackId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                ConversationId = _conversationId,
                IsGoodFeedback = isGoodFeedback
            };
            await _logService.SaveFeedbackAsync(feedback);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

    }
}
