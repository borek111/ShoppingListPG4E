using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using ShoppingListPG4E.Models;

namespace ShoppingListPG4E.Models
{
    public static class ShoppingListFile
    {
        public static string AppXmlPath => Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml");

        public static void EnsureCreated()
        {
            if (File.Exists(AppXmlPath))
                return;

            var doc = Product.LoadOrCreateDocument();
            doc.Save(AppXmlPath);
        }

        // Zwraca strumieñ tylko-do-odczytu bie¿¹cego pliku XML.
        public static Task<Stream> OpenReadAsync()
        {
            EnsureCreated();
            Stream stream = File.OpenRead(AppXmlPath);
            return Task.FromResult(stream);
        }

        public static async Task ReplaceWithAsync(Stream source, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await using var dst = File.Create(AppXmlPath);
            await source.CopyToAsync(dst, cancellationToken);
        }
    }
}