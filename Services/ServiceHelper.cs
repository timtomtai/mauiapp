using Microsoft.Maui.Controls;
using System;

namespace MauiImageClassifierApp.Services
{
    public static class ServiceHelper
    {
        public static T GetService<T>() where T : class
        {
            var service = Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(T)) as T;

            if (service == null)
                throw new InvalidOperationException($"Service of type {typeof(T).Name} not found.");

            return service;
        }
    }
}
