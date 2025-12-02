using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShoppingListPG4E.Models
{
    public class Product
    {
        public static string AppXmlPath => Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml");

        //Sharing a file for export
        public static Task<Stream> OpenReadAsync()
        {
            Stream stream = File.OpenRead(AppXmlPath);
            return Task.FromResult(stream);
        }

        public static async Task ReplaceWithAsync(Stream source, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await using FileStream dst = File.Create(AppXmlPath);
            await source.CopyToAsync(dst, cancellationToken);
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public double Quantity { get; set; }
        public bool Purchased { get; set; }
        public string Category { get; set; }
        public bool Optional { get; set; }
        public string Store { get; set; }

        public Product()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Unit = "szt.";
            Quantity = 1;
            Purchased = false;
            Category = string.Empty;
            Optional = false;
            Store = string.Empty;
        }

        public static XDocument LoadOrCreateDocument()
        {
            if (File.Exists(AppXmlPath))
                return XDocument.Load(AppXmlPath);

            XDocument doc = new XDocument(
                new XElement("ShoppingList",
                    new XElement("Categories",
                        new XElement("Category", "Nabiał"),
                        new XElement("Category", "Warzywa"),
                        new XElement("Category", "Owoce"),
                        new XElement("Category", "Elektronika"),
                        new XElement("Category", "AGD"),
                        new XElement("Category", "Inne...")
                    ),
                    new XElement("Units",
                        new XElement("Unit", "szt."),
                        new XElement("Unit", "kg"),
                        new XElement("Unit", "l"),
                        new XElement("Unit", "g"),
                        new XElement("Unit", "opak."),
                        new XElement("Unit", "ml"),
                        new XElement("Unit", "Inne...")
                    ),
                    new XElement("Stores",
                        new XElement("Store", "Biedronka"),
                        new XElement("Store", "Lidl"),
                        new XElement("Store", "Selgros"),
                        new XElement("Store", "Auchan"),
                        new XElement("Store", "Inne...")
                    ),
                    new XElement("Products")
                )
            );
            doc.Save(AppXmlPath);
            return doc;
        }

        public static XElement EnsureSection(XDocument doc, string sectionName)
        {
            XElement root = doc.Root!;
            XElement? section = root.Element(sectionName);
            if (section == null)
            {
                section = new XElement(sectionName);
                root.Add(section);
            }
            return section;
        }

        public void Save()
        {
            XDocument doc = LoadOrCreateDocument();
            XElement productsRoot = EnsureSection(doc, "Products");

            XElement? existing = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (existing != null)
            {
                existing.Element("Name")!.Value = Name;
                existing.Element("Unit")!.Value = Unit;
                existing.Element("Quantity")!.Value = Quantity.ToString();
                existing.Element("Purchased")!.Value = Purchased.ToString();
                existing.Element("Category")!.Value = Category;
                existing.Element("Optional")!.Value = Optional.ToString();
                existing.Element("Store")!.Value = Store;
            }
            else
            {
                productsRoot.Add(new XElement("Product",
                    new XAttribute("Id", Id),
                    new XElement("Name", Name),
                    new XElement("Unit", Unit),
                    new XElement("Quantity", Quantity),
                    new XElement("Purchased", Purchased),
                    new XElement("Category", Category),
                    new XElement("Optional", Optional),
                    new XElement("Store", Store ?? string.Empty)
                ));
            }

            doc.Save(AppXmlPath);
        }

        public void Delete()
        {
            if (!File.Exists(AppXmlPath)) return;

            XDocument doc = XDocument.Load(AppXmlPath);
            XElement productsRoot = EnsureSection(doc, "Products");

            XElement? node = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (node != null)
            {
                node.Remove();
                doc.Save(AppXmlPath);
            }
        }

        public static Product Load(string id)
        {
            XDocument doc = LoadOrCreateDocument();
            XElement productsRoot = EnsureSection(doc, "Products");

            XElement? node = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == id);
            if (node == null)
                return new Product { Id = id };

            double q;
            bool p;
            bool o;

            return new Product
            {
                Id = id,
                Name = node.Element("Name")?.Value ?? string.Empty,
                Unit = node.Element("Unit")?.Value ?? "szt.",
                Quantity = double.TryParse(node.Element("Quantity")?.Value, out q) ? q : 1,
                Purchased = bool.TryParse(node.Element("Purchased")?.Value, out p) && p,
                Category = node.Element("Category")?.Value ?? string.Empty,
                Optional = bool.TryParse(node.Element("Optional")?.Value, out o) && o,
                Store = node.Element("Store")?.Value ?? string.Empty
            };
        }

        public static IEnumerable<Product> LoadAll()
        {
            if (!File.Exists(AppXmlPath))
                return new List<Product>();

            XDocument doc = XDocument.Load(AppXmlPath);
            XElement productsRoot = EnsureSection(doc, "Products");

            IEnumerable<Product> products = productsRoot.Elements("Product").Select(node => new Product
            {
                Id = node.Attribute("Id")?.Value ?? Guid.NewGuid().ToString(),
                Name = node.Element("Name")?.Value ?? string.Empty,
                Unit = node.Element("Unit")?.Value ?? "szt.",
                Quantity = double.TryParse(node.Element("Quantity")?.Value, out double q) ? q : 1,
                Purchased = bool.TryParse(node.Element("Purchased")?.Value, out bool p) && p,
                Category = node.Element("Category")?.Value ?? string.Empty,
                Optional = bool.TryParse(node.Element("Optional")?.Value, out bool o) && o,
                Store = node.Element("Store")?.Value ?? string.Empty
            });

            return products;
        }
    }
}
