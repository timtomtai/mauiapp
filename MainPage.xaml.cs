using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using MauiImageClassifierApp.Services;
using Azure.Storage.Blobs;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using System.Net.Http.Headers;
using Microsoft.Maui.Devices.Sensors;
using Azure.Storage.Blobs.Models;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using static System.Net.WebRequestMethods;



namespace MauiImageClassifierApp;

public partial class MainPage : ContentPage
{
    private FileResult? _photo;
    private string? capturedImagePath;
    private const string AzureContentUnderstandingEndpoint = "https://campio-2025-fl-jsa.services.ai.azure.com/contentunderstanding/analyzers/cu-initial-image-analyzer:analyze?api-version=2025-05-01-preview";
    private const string SubscriptionKey ;
    private readonly string AzureFunctionUrl ;
    private readonly string ApiKey ;
	
	SubscriptionKey = Environment.GetEnvironmentVariable("YourSubscriptionKeyName");
    AzureFunctionUrl = Environment.GetEnvironmentVariable("YourAzureFunctionUrlName");
    ApiKey = Environment.GetEnvironmentVariable("YourApiKeyName");
    public MainPage()
    {
        InitializeComponent();
    }

    async void OnCaptureClicked(object sender, EventArgs e)
    {
        try
        {
            // Request location permission (if not already granted)
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Location Permission", "Location permission denied. Cannot capture location data.", "OK");
                    return; // Exit if permission is denied
                }
            }

            // Get the current location
            Location currentLocation = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

            // Display or store location details
            if (currentLocation != null)
            {
                // Example: Display the latitude and longitude
               // await DisplayAlert("Location Captured", $"Latitude: {currentLocation.Latitude}, Longitude: {currentLocation.Longitude}", "OK");

                // You can also store these location details with the captured image
                // (e.g., in a database, local storage, or embedded in image metadata - more advanced)
            }
            else
            {
                await DisplayAlert("Location Not Captured", "Unable to get current location details.", "OK");
            }


            _photo = await MediaPicker.CapturePhotoAsync();
            if (_photo != null)
            {
                var stream = await _photo.OpenReadAsync();
                CapturedImage.Source = ImageSource.FromStream(() => stream);
                capturedImagePath = _photo.FullPath;
                ImagePreviewFrame.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Capture failed: {ex.Message}", "OK");
        }
    }

    private async void OnSelectAndUploadImageClicked(object sender, EventArgs e)
    {

        string imageUrl = "";
        try
        {
           

            // 1. Pick an image from the device's gallery
            var pickOptions = new PickOptions
            {
                PickerTitle = "Please select a photo",
                FileTypes = FilePickerFileType.Images
            };

            var result = await FilePicker.PickAsync(pickOptions);

            if (result == null)
            {
                ResultLabel.Text = "No photo selected.";
                return;
            }

            var metadata = new Dictionary<string, string>();

            try
            {
                // 2. Read EXIF data from the selected image
                IEnumerable<MetadataExtractor.Directory> directories = ImageMetadataReader.ReadMetadata(result.FullPath);
                var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();

                if (gpsDirectory != null)
                {
                    // GetGeoLocation() will return null if no valid GPS data is available.
                    var location = gpsDirectory.GetGeoLocation();

                    if (location != null)
                    {
                        metadata.Add("Latitude", location.Latitude.ToString());
                        metadata.Add("Longitude", location.Longitude.ToString());
                        ResultLabel.Text += $"\nEXIF GPS: Lat {location.Latitude}, Long {location.Longitude}";
                    }
                    else
                    {
                        ResultLabel.Text += "\nNo EXIF GPS data found.";
                    }
                }
            }
            catch (Exception ex)
            {
                ResultLabel.Text += $"\nFailed to read EXIF metadata: {ex.Message}";
                // Continue with upload even if EXIF data can't be read
            }


            // 3. Upload the image to Azure Blob Storage
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=campio2025flblob;AccountKey=vs96X1c4z/9o4qTAaNA+S7yISAWYq485WRZx2rPBWpumlciztU5/It50U/lYxm9CJ41iFRFRn8TM+AStK0yi1g==;EndpointSuffix=core.windows.net";
            string containerName = "images";
            string blobName = $"{Guid.NewGuid()}{Path.GetExtension(result.FullPath)}";

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using var fileStream = System.IO.File.OpenRead(result.FullPath);

            var uploadOptions = new BlobUploadOptions
            {
                Metadata = metadata,
                HttpHeaders = new BlobHttpHeaders { ContentType = result.ContentType }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);

          

            

        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to upload image: {ex.Message}", "OK");

        }

        
    }
    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        try
        {
            // Pick a photo from the device's gallery
            _photo = await MediaPicker.Default.PickPhotoAsync();

            if (_photo != null)
            {
                // Display the selected image
                CapturedImage.Source = ImageSource.FromStream(() => _photo.OpenReadAsync().Result);
                ImagePreviewFrame.IsVisible = true;
                // Now you can use the photo.FullPath to save the image or send it to your image classification model
                string filePath = _photo.FullPath;
                Console.WriteLine($"Selected image path: {filePath}");

                // You can also add logic here to process the selected image (e.g., using your ImageSaver)
                // await new ImageSaver().SaveImageAsync(filePath);
            }
        }
        catch (FeatureNotSupportedException fnsEx)
        {
            // Handle not supported on device exception
            await DisplayAlert("Error", "Picking photos is not supported on this device.", "OK");
        }
        catch (PermissionException pEx)
        {
            // Handle permission exception
            await DisplayAlert("Error", "Permission to access photos was denied. Please grant the necessary permissions in settings.", "OK");
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
    }
    private void OnToggleResultsClicked(object sender, EventArgs e)
    {
        // Toggle the visibility of the results frame
        ResultsFrame.IsVisible = !ResultsFrame.IsVisible;

        // Optionally, update the button text to reflect the state
        if (sender is Button button)
        {
            button.Text = ResultsFrame.IsVisible ? "Hide Results" : "Show Results";
        }
    }
    private async void OnGoToNewPageClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Explore());
    }


    async void OnSendClickedMCP(object sender, EventArgs e)
    {
        // HIGHLIGHT: Check for a photo selection using MediaPicker as it's assumed from your original code.
        if (_photo == null)
        {
            await DisplayAlert("Selection Cancelled", "No image was selected.", "OK");
            return;
        }

        ResultLabel.Text = "Sending data to MCP...";

        // HIGHLIGHT: Get the current device location.
        Location location = null;
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            location = await Geolocation.GetLocationAsync(request);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Location Error", $"Unable to get location: {ex.Message}", "OK");
            return;
        }

        // HIGHLIGHT: Convert the image from the MediaPicker stream to a byte array.
        byte[] imageData;
        using (var originalStream = await _photo.OpenReadAsync())
        {
            using var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream);
            imageData = memoryStream.ToArray();
        }

        // HIGHLIGHT: Check for null imageData after conversion.
        if (imageData == null || imageData.Length == 0)
        {
            await DisplayAlert("Error", "Could not convert image to byte array.", "OK");
            return;
        }

        // HIGHLIGHT: Create an anonymous object with the byte array, venue, latitude, and longitude.
        var venue = "test#test#MR"; // Replace with your actual venue logic.
        var payload = new
        {
            image_url =   "https://campio2025flblob.blob.core.windows.net/images/location/IMG_0128.JPG",// 
           
            lat = (float?)location?.Latitude, // Use nullable float
            lon = (float?)location?.Longitude // Use nullable float
        };

        // HIGHLIGHT: Serialize the object to a JSON string.
        string jsonPayload = JsonSerializer.Serialize(payload);

        // HIGHLIGHT: Load the image and resize logic removed.
        // The image is now sent as a byte array directly, not as a resized stream.

        using HttpClient client = new HttpClient();
        // Assuming ApiKey and AzureFunctionUrl are defined elsewhere in your class
        client.DefaultRequestHeaders.Add("x-api-key", ApiKey);

        // HIGHLIGHT: Create the HttpContent from the JSON string.
        HttpContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(AzureFunctionUrl, content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            ResultLabel.Text = $"Success! API Response:\n{responseBody}";
        }
        else
        {
            await DisplayAlert("Error", $"API call failed: {response.StatusCode}", "OK");
            ResultLabel.Text = $"API call failed with status: {response.StatusCode}\n{responseBody}";
        }
    }

    private async void OnGoToNearBYClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new NearBy());
    }
    async void OnSendClicked(object sender, EventArgs e)
    {
        if (_photo == null)
        {
            ResultLabel.Text = "No photo captured.";
            return;
        }

        ResultLabel.Text = "Sending image to OpenAI...";
        using var stream = await _photo.OpenReadAsync();
        var result = await AzureOpenAIService.ClassifyImageAsync(stream);
        ResultLabel.Text = result;

        // --- Add the confirmation logic here ---
        bool isConfirmed = await DisplayAlert("Confirmation", $"The result is: {result}\n\nDo you want to save this image to the gallery?", "Yes", "No");

        if (isConfirmed)
        {
            OnSaveToGalleryClicked(sender, e);
        }
    }

    private async void OnClassifyImageClicked(object sender, EventArgs e)
    {
        try
        {
            if (_photo == null)
            {
                await DisplayAlert("Selection Cancelled", "No image was selected.", "OK");
                return;
            }

            // 1. Load the image into an IImage object for manipulation
            Microsoft.Maui.Graphics.IImage originalImage;
            using (var stream = await _photo.OpenReadAsync())
            {
                originalImage = PlatformImage.FromStream(stream);
            }

            if (originalImage == null)
            {
                await DisplayAlert("Error", "Could not load the selected image.", "OK");
                return;
            }

            // 2. Downsize the image (e.g., set max dimension to 800 pixels)
            float maxDimension = 800f;
            Microsoft.Maui.Graphics.IImage resizedImage = originalImage.Downsize(maxDimension, disposeOriginal: true);

            // 3. Save the resized image to a memory stream (e.g., as JPEG with reduced quality)
            using var outputStream = new MemoryStream();
            await resizedImage.SaveAsync(outputStream, ImageFormat.Jpeg, quality: 0.7f);
            outputStream.Position = 0; // Reset stream position for reading


            // 4. Send the image data as application/octet-stream
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);

            // Create a StreamContent directly from the MemoryStream
            HttpContent content = new StreamContent(outputStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // --- Enhanced Debugging ---
            System.Diagnostics.Debug.WriteLine($"--- Sending Request ---");
            System.Diagnostics.Debug.WriteLine($"URL: {AzureContentUnderstandingEndpoint}");
            System.Diagnostics.Debug.WriteLine($"Subscription Key: {SubscriptionKey}");
            System.Diagnostics.Debug.WriteLine($"Content-Type: {content.Headers.ContentType}");
            System.Diagnostics.Debug.WriteLine($"Stream Length: {outputStream.Length} bytes");
            // --- End Enhanced Debugging ---

            HttpResponseMessage response = await client.PostAsync(AzureContentUnderstandingEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorResponseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"--- Error Response ---");
                System.Diagnostics.Debug.WriteLine($"Status Code: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Error Content: {errorResponseContent}");
                System.Diagnostics.Debug.WriteLine($"--- End Error Response ---");
                await DisplayAlert("API Error", $"Status Code: {response.StatusCode}\nError: {errorResponseContent}", "OK");
                return;
            }

            string postResponseContent = await response.Content.ReadAsStringAsync();
            // Parse the initial POST response to get the operationId
            // The response likely contains an object like: { "id": "78a3e3bf-c308-43fe-a2f0-cbc2bac8fbe2" }
            var postResult = JsonDocument.Parse(postResponseContent);
            string operationId = postResult.RootElement.GetProperty("id").GetString();

            System.Diagnostics.Debug.WriteLine($"Initial POST successful. Operation ID: {operationId}");
            await DisplayAlert("Analysis Started", $"Operation ID: {operationId}\nPolling for results...", "OK");


            // 4. Poll for the analysis result
            string pollUrl = $"https://campio-2025-fl-jsa.services.ai.azure.com/contentunderstanding/analyzerResults/{operationId}?api-version=2025-05-01-preview"; 
            bool completed = false;
            string finalResult = string.Empty;
            int maxRetries = 10; // Adjust as needed
            int retryCount = 0;
            TimeSpan delay = TimeSpan.FromSeconds(20); // Start with a 5-second delay

            while (!completed && retryCount < maxRetries)
            {
                await Task.Delay(delay); // Wait before the next poll

                System.Diagnostics.Debug.WriteLine($"Polling attempt {retryCount + 1} for operation ID: {operationId}");

                HttpResponseMessage getResponse = await client.GetAsync(pollUrl);

                if (!getResponse.IsSuccessStatusCode)
                {
                    string errorResponseContent = await getResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Polling Error: Status Code: {getResponse.StatusCode}, Error: {errorResponseContent}");
                    // Decide if you want to stop or continue polling on non-success codes
                    await DisplayAlert("Polling Error", $"Status Code: {getResponse.StatusCode}\nError: {errorResponseContent}", "OK");
                    break;
                }

                string getResponseContent = await getResponse.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Polling Response: {getResponseContent}");

                // Parse the GET response to check the status. 
                // The response likely contains a "status" field, e.g., "running", "succeeded", "failed".
                var getResult = JsonDocument.Parse(getResponseContent);
                string status = getResult.RootElement.GetProperty("status").GetString(); // Assuming a 'status' field

                if (status.Equals("succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    completed = true;
                    finalResult = getResponseContent;
                    System.Diagnostics.Debug.WriteLine($"Analysis Succeeded!");
                }
                else if (status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                {
                    completed = true; // Stop polling
                    finalResult = getResponseContent; // Store the error details
                    System.Diagnostics.Debug.WriteLine($"Analysis Failed!");
                    await DisplayAlert("Analysis Failed", finalResult, "OK");
                    break;
                }
                else
                {
                    // Still running, wait and retry
                    System.Diagnostics.Debug.WriteLine($"Analysis still {status}, retrying in {delay.TotalSeconds} seconds...");
                    retryCount++;
                    // Optional: Implement exponential backoff for delays
                    // delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); 
                }
            }

            if (completed)
            {
                await DisplayAlert("Analysis Complete", finalResult, "OK");


                // --- Additional code to extract fields.class.valueString ---
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(finalResult);
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("result", out JsonElement resultElement) && // Navigate to 'result'
                        resultElement.TryGetProperty("contents", out JsonElement contentsElement) && // Navigate to 'contents'
                        contentsElement.ValueKind == JsonValueKind.Array && // Ensure 'contents' is an array
                        contentsElement.EnumerateArray().Any()) // Check if the array has elements
                    {
                        // Get the first element in the 'contents' array
                        JsonElement firstContent = contentsElement.EnumerateArray().First();

                        if (firstContent.TryGetProperty("fields", out JsonElement fieldsElement) && // Navigate to 'fields'
                            fieldsElement.TryGetProperty("class", out JsonElement classElement) && // Navigate to 'class'
                            classElement.TryGetProperty("valueString", out JsonElement valueStringElement)) // Navigate to 'valueString'
                        {
                            string classificationValue = valueStringElement.GetString(); // Get the string value

                            await DisplayAlert("Classification Result", $"Class: {classificationValue}", "OK");
                            System.Diagnostics.Debug.WriteLine($"Extracted Class: {classificationValue}");
                        }
                        else
                        {
                            await DisplayAlert("Parsing Error", "Could not find 'fields.class.valueString' in the response.", "OK");
                            System.Diagnostics.Debug.WriteLine("Parsing Error: Could not find 'fields.class.valueString'.");
                        }
                    }
                    else
                    {
                        await DisplayAlert("Parsing Error", "Response structure unexpected (missing result/contents or contents not an array).", "OK");
                        System.Diagnostics.Debug.WriteLine("Parsing Error: Response structure unexpected.");
                    }
                }
                catch (Exception jsonEx)
                {
                    await DisplayAlert("JSON Parsing Error", $"Failed to parse final result: {jsonEx.Message}", "OK");
                    System.Diagnostics.Debug.WriteLine($"JSON Parsing Error: {jsonEx.Message}");
                }
                // --- End Additional code ---
            }
            else
            {
                await DisplayAlert("Analysis Timed Out", "Analysis did not complete within the allowed time.", "OK");
            }
        }
        catch (HttpRequestException httpEx)
        {
            System.Diagnostics.Debug.WriteLine($"--- HTTP Request Exception ---");
            System.Diagnostics.Debug.WriteLine($"Status Code: {httpEx.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Message: {httpEx.Message}");
            System.Diagnostics.Debug.WriteLine($"--- End HTTP Request Exception ---");
            await DisplayAlert("HTTP Request Error", $"Status Code: {httpEx.StatusCode}\nMessage: {httpEx.Message}", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"--- General Exception ---");
            System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
            System.Diagnostics.Debug.WriteLine($"--- End General Exception ---");
            await DisplayAlert("Error", $"Classification/Analysis failed: {ex.Message}", "OK");
        }
    }

    

    private async void OnSaveToGalleryClicked(object sender, EventArgs e)
    {
        // Highlighted Change: Check if the _photo field has a value.
        if (_photo != null)
        {
            try
            {
                var imageSaver = ServiceHelper.GetService<IImageSaver>();
                if (imageSaver != null)
                {
                    // Highlighted Change: Use _photo.FullPath to get the file path.
                    await imageSaver.SaveImageAsync(_photo.FullPath);
                    await DisplayAlert("Success", "Image saved to gallery", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "ImageSaver service not found.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Save failed: {ex.Message}", "OK");
            }
        }
        else
        {
            await DisplayAlert("Error", "No image to save", "OK");
        }
    }
}
