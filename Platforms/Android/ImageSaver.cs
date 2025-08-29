using Android.Content;
using Android.Provider;
using MauiImageClassifierApp.Services;
using Uri = Android.Net.Uri;
using MauiImageClassifierApp;

namespace MauiImageClassifierApp.Platforms.Android;

public class ImageSaver : IImageSaver
{
    public async Task SaveImageAsync(string filePath)
    {
        var context = global::Android.App.Application.Context;

        if (!System.IO.File.Exists(filePath))
            return;

        var filename = Path.GetFileName(filePath);

        var values = new ContentValues();
        values.Put(MediaStore.IMediaColumns.DisplayName, filename);
        values.Put(MediaStore.IMediaColumns.MimeType, "image/jpeg");
        values.Put(MediaStore.IMediaColumns.RelativePath, "DCIM/Camera");

        Uri uri = context.ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);

        using var outputStream = context.ContentResolver.OpenOutputStream(uri!);
        using var inputStream = System.IO.File.OpenRead(filePath);

        await inputStream.CopyToAsync(outputStream!);
    }
}
