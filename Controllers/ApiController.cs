using System.Text;
using Microsoft.AspNetCore.Mvc;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;

[Route("api")]
[ApiController]
public class ApiController : ControllerBase
{
    private readonly IOpenAIService openAIService;
    private readonly IConfiguration config;

    public ApiController(IOpenAIService openAIService,IConfiguration config)
    {
        this.openAIService = openAIService;
        this.config = config;
    }

    [HttpPost("stream")]
    public async Task Completion([FromBody] PromptInputModel input)
    {
        if (input?.Prompt == null)
        {
            return;
        }
        

        var completion = openAIService.ChatCompletion.CreateCompletionAsStream(new OpenAI.GPT3.ObjectModels.RequestModels.ChatCompletionCreateRequest
        {
            Messages = BuildMessages(input.Prompt),
            MaxTokens = 100,
            Temperature = 0.5f,
            TopP = 1,
            FrequencyPenalty = 0,
            PresencePenalty = 0
        }, Models.ChatGpt3_5Turbo);
        async Task ResponseWrite(string? text) {
            if (text == null) return;
            await Response.Body.WriteAsync(Encoding.UTF8.GetBytes(text));
            await Response.Body.FlushAsync();
        }
        await foreach (var completionResponse in completion)
        {
            if (completionResponse.Successful)
            {
                await ResponseWrite(completionResponse.Choices[0].Message.Content);
            }else {
                await ResponseWrite("<ERR>");
                if(completionResponse.Error != null){
                    await ResponseWrite(completionResponse.Error.Code);
                    await ResponseWrite(completionResponse.Error.Message);
                }else {
                    await ResponseWrite("<unknow>");
                }
            }
        }
    }
    private List<ChatMessage> BuildMessages(string question) {
        return new List<ChatMessage>() {
             new ChatMessage("system",config["CharacterDescription"] ?? ""),
             new ChatMessage("user",question)
        };
    }
    private string BuildPrompt(string? prompt)
    {
        var chartacter_desc = config["CharacterDescription"];
        return $"{chartacter_desc}\nQ:{prompt}\n A:";
    }
}