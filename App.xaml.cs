namespace MauiImageClassifierApp;

public partial   class App : Application
{
    public App()
    {
        InitializeComponent(); // This must be called from a partial class
        //MainPage = new MainPage();
        MainPage = new NavigationPage(new MainPage());
    }
}
