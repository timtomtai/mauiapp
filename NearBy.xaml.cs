using Microsoft.Maui.Controls;
using MauiImageClassifierApp.ViewModels;

namespace MauiImageClassifierApp
{
    public partial class NearBy : ContentPage
    {
        public NearBy()
        {
            InitializeComponent();
            BindingContext = new NearByViewModel();
        }
    }
}
