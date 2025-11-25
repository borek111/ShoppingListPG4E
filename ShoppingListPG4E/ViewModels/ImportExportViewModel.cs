using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Storage; 
using Microsoft.Maui.Storage;
using ShoppingListPG4E.Models;
using System.Windows.Input;

namespace ShoppingListPG4E.ViewModels
{
    public partial class ImportExportViewModel : ObservableObject
    {
        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }

        public ImportExportViewModel()
        {
            ExportCommand = new AsyncRelayCommand(ExportAsync);
            ImportCommand = new AsyncRelayCommand(ImportAsync);
        }

        private static string AppXmlPath => Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml");

        private async Task ExportAsync()
        {
            try
            {
                if (!File.Exists(AppXmlPath))
                {
                    var doc = Product.LoadOrCreateDocument();
                    doc.Save(AppXmlPath);
                }

                await using var readStream = File.OpenRead(AppXmlPath);
                var saveResult = await FileSaver.Default.SaveAsync("ShoppingList.xml", readStream, CancellationToken.None);

                if (saveResult.IsSuccessful)
                    Status = $"Zapisano do: {saveResult.FilePath}";
                else if (saveResult.Exception is not null)
                    Status = $"B³¹d zapisu: {saveResult.Exception.Message}";
                else
                    Status = "Anulowano zapis.";
            }
            catch (Exception ex)
            {
                Status = $"B³¹d exportu: {ex.Message}";
            }
        }

        private async Task ImportAsync()
        {
            try
            {
                var xmlTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/xml", "text/xml" } },
                    { DevicePlatform.WinUI,   new[] { ".xml" } },
                    { DevicePlatform.iOS,     new[] { "public.xml" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.xml" } },
                });

                var picked = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik ShoppingList.xml do importu",
                    FileTypes = xmlTypes
                });

                if (picked is null)
                {
                    Status = "Anulowano import.";
                    return;
                }

                await using var src = await picked.OpenReadAsync();
                Directory.CreateDirectory(FileSystem.AppDataDirectory);
                await using var dst = File.Create(AppXmlPath);
                await src.CopyToAsync(dst);

                Status = "Zaimportowano listê zakupów.";
            }
            catch (Exception ex)
            {
                Status = $"B³¹d importu: {ex.Message}";
            }
        }
    }
}