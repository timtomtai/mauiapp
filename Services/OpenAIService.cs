using System.Net.Http.Headers;
using System.Text;
using System.Text.Json; // Using System.Text.Json for serialization
using System.Threading.Tasks;

namespace MauiImageClassifierApp.Services;

public static class AzureOpenAIService
{
    // Replace with your Azure OpenAI Endpoint and API Key
    private const string AzureOpenAIEndpoint = "https://campio-2025-fl-jsa.openai.azure.com/"; // e.g., https://your-resource-name.openai.azure.com/
    private const string ApiKey = "1e5Q5192EaIlWD1f7WElMr8Xl3GLzCqS1tTAI65Y6Hynkrg1kFqAJQQJ99BGAC4f1cMXJ3w3AAAAACOGCkIt"; // From Azure OpenAI resource keys

    // Replace with your Azure OpenAI deployment name for vision model
    // This is the name you gave your deployed model (e.g., 'gpt-4o-vision-deployment')
    private const string AzureOpenAIDeploymentName = "gpt-4.1-nano";

    // Azure OpenAI requires an API version parameter
    private const string AzureOpenAIApiVersion = "2024-02-15-preview"; // Use the API version compatible with your deployed model

    public static async Task<string> ClassifyImageAsync(Stream imageStream)
    {
        using var httpClient = new HttpClient();

        // Convert image to base64
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms);
        string base64Image = Convert.ToBase64String(ms.ToArray());

        var requestBody = new
        {
            // For Azure OpenAI, 'model' refers to the deployment name
            model = AzureOpenAIDeploymentName,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = "Identify the category in which this image will fall. The expected categories are Store hours, Menus, Wi-Fi passwords and default to new_business" },
                        new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{base64Image}" } }
                    }
                }
            },
            max_tokens = 1000
        };

        // Construct the Azure OpenAI endpoint with the deployment name and API version
        // The path structure for chat completions in Azure OpenAI is typically /openai/deployments/{deployment-id}/chat/completions
        // And the api-version is passed as a query parameter
        string requestUri = $"{AzureOpenAIEndpoint}openai/deployments/{AzureOpenAIDeploymentName}/chat/completions?api-version={AzureOpenAIApiVersion}";

        var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

        // For Azure OpenAI with API Key authentication, you'll use the "api-key" header.
        request.Headers.Add("api-key", ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // You might want to log the errorContent for debugging in a real application
            return $"Error: {response.StatusCode}\n{responseBody}";
        }

        // Parse the response to extract the content
        using var doc = JsonDocument.Parse(responseBody);
        string result = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return result;
    }
}