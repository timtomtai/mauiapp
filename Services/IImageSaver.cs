namespace MauiImageClassifierApp.Services
{
    public interface IImageSaver
    {
        Task SaveImageAsync(string filePath);
    }
}
