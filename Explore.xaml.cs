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

using static Microsoft.Maui.ApplicationModel.Permissions;

namespace MauiImageClassifierApp;

public partial class Explore : ContentPage
{
    private FileResult? _photo;
    private string? capturedImagePath;
    // Define HttpClient at the class level for efficiency
    private readonly HttpClient _httpClient = new HttpClient();

    // Replace with your Azure Function's URL
    private const string AzureFunctionUrl = "https://campio2025-fl-utilities-dxchefdfgybddmgp.eastus-01.azurewebsites.net/api/upload_file_to_blob?code=UU9ecZtmTzNOpLwcaFfEj8rOGUKz0fE5esEY52mG0N3mAzFuM9aJew==";
    public Explore()
	{
		InitializeComponent();
	}

    private async void OnCallAzureFunctionClicked(object sender, EventArgs e)
    {
        try
        {


            // 1. Prepare the data to send (if any)
            if (_photo != null)
            {
                // 2. Get a stream of the selected image
                using var stream = await _photo.OpenReadAsync();


                string blobName = $"{Guid.NewGuid()}.jpg"; // Generate a unique blob name




                // Example: Sending a simple string or an object as JSON
                var requestData = blobName;
                string jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 2. Make the HTTP POST (or GET/PUT/DELETE) request to the Azure Function
                HttpResponseMessage response = await _httpClient.PostAsync(AzureFunctionUrl, content);

                // 3. Handle the response
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    // Deserialize the response if your Azure Function returns JSON
                    // Example:
                    // var result = JsonSerializer.Deserialize<YourResponseType>(responseBody);
                    await DisplayAlert("Success", $"Azure Function called successfully! Response: {responseBody}", "OK");
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error calling Azure Function: {response.StatusCode} - {errorContent}", "OK");
                }

            }
        }
        catch (HttpRequestException httpEx)
        {
            await DisplayAlert("Network Error", $"Failed to connect to Azure Function: {httpEx.Message}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An unexpected error occurred: {ex.Message}", "OK");
        }
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
                await DisplayAlert("Location Captured", $"Latitude: {currentLocation.Latitude}, Longitude: {currentLocation.Longitude}", "OK");

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
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Capture failed: {ex.Message}", "OK");
        }
    }
    private async void OnSelectAndUploadImageClickedExplore(object sender, EventArgs e)
    {
        try
        {
            { 

                await DisplayAlert("Success", "Image uploaded successfully!", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to upload image: {ex.Message}", "OK");
        }
    }
}