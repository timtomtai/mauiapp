namespace MauiImageClassifierApp.Services
{
    public interface IOpenAIService
    {
        Task<string> ClassifyImageAsync(Stream imageStream);
    }
}