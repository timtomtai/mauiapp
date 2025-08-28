using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using MauiImageClassifierApp.Models;
using Newtonsoft.Json.Linq;

namespace MauiImageClassifierApp.ViewModels
{
    public class NearByViewModel : BaseViewModel
    {
        private readonly HttpClient _httpClient;

        public ObservableCollection<string> BusinessTypes { get; set; }
        public ObservableCollection<int?> RadiusOptions { get; set; }
        public ObservableCollection<Venue> Venues { get; set; }

        private string _selectedBusinessType;
        public string SelectedBusinessType
        {
            get => _selectedBusinessType;
            set
            {
                if (SetProperty(ref _selectedBusinessType, value))
                    ((Command)FindVenuesCommand).ChangeCanExecute();
            }
        }

        private int? _selectedRadius;
        public int? SelectedRadius
        {
            get => _selectedRadius;
            set => SetProperty(ref _selectedRadius, value);
        }

        // API endpoints
        private readonly string AzureFunctionUrl = "https://mcp-server-backend-tools.gentleforest-803ae226.westus.azurecontainerapps.io/api_db_get_business_list_by_lat_lon";
        private readonly string ApiKey = "19lJL+W7LNXte+ASltqdeToLkvI8vioXVrHyo6PDSC+ACRC3cyFHii";
        private readonly string BusinessTypeApiUrl = "https://mcp-server-backend-tools.gentleforest-803ae226.westus.azurecontainerapps.io/api_db_get_business_types";
        private readonly string BusinessTypeApiKey = "19lJL+W7LNXte+ASltqdeToLkvI8vioXVrHyo6PDSC+ACRC3cyFHii";

        public ICommand FindVenuesCommand { get; }

        public NearByViewModel()
        {
            Title = "Nearby";
            _httpClient = new HttpClient();

            BusinessTypes = new ObservableCollection<string>();
            Venues = new ObservableCollection<Venue>();
            RadiusOptions = new ObservableCollection<int?>(
     (new int?[] { null }).Concat(
         Enumerable.Range(1, 400).Select(x => (int?)(x * 5))
     )
 );
            SelectedRadius = null;

            FindVenuesCommand = new Command(async () => await LoadVenuesForSelectedType(),
                () => !string.IsNullOrEmpty(SelectedBusinessType));

            // Load business types automatically
            Task.Run(LoadBusinessTypesAsync);
        }

        private async Task LoadBusinessTypesAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                var request = new HttpRequestMessage(HttpMethod.Post, BusinessTypeApiUrl);
                request.Headers.Add("x-apikey", BusinessTypeApiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var jsonArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                MainThread.BeginInvokeOnMainThread(() => BusinessTypes.Clear());

                foreach (var item in jsonArray)
                {
                    var type = item["business_type"]?.ToString();
                    if (!string.IsNullOrEmpty(type))
                    {
                        MainThread.BeginInvokeOnMainThread(() => BusinessTypes.Add(type));
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    Application.Current.MainPage.DisplayAlert("Error", $"Failed to load business types: {ex.Message}", "OK"));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadVenuesForSelectedType()
        {
            if (string.IsNullOrEmpty(SelectedBusinessType)) return;
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    await Application.Current.MainPage.DisplayAlert("Permission Denied", "Location permission is required.", "OK");
                    return;
                }

                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10)));

                double latitude = location?.Latitude ?? 42.764425;
                double longitude = location?.Longitude ?? 71.026358;

                var requestBody = new SearchRequest
                {
                    category = SelectedBusinessType,
                    latitude = latitude,
                    longitude = longitude,
                    radius_miles = SelectedRadius,
                    return_count = 5
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, AzureFunctionUrl);
                request.Headers.Add("x-api-key", ApiKey);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await Application.Current.MainPage.DisplayAlert("Error", $"API call failed: {response.StatusCode}\n{error}", "OK");
                    return;
                }

                var venues = JsonSerializer.Deserialize<ObservableCollection<Venue>>(await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Venues.Clear();
                    if (venues != null)
                    {
                        foreach (var venue in venues)
                            Venues.Add(venue);
                    }
                });

                await Application.Current.MainPage.DisplayAlert("Result",
                    Venues.Count > 0 ? "Venues loaded." : "No venues found.", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Unexpected error: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
