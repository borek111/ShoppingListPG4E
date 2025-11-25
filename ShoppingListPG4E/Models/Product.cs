using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ShoppingListPG4E.Models
{
    public class Product
    {
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Unit { get; set; } 
        public double Quantity { get; set; }
        public bool Purchased { get; set; } 
        public string Category { get; set; } 

        private static string XmlPath => Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml");

        public Product()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Unit = "szt.";
            Quantity = 1;
            Purchased = false;
            Category = string.Empty;
        }

        public static XDocument LoadOrCreateDocument()
        {
            if (File.Exists(XmlPath))
            {
                return XDocument.Load(XmlPath);
            }

            var doc = new XDocument(
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
                    new XElement("Products")
                )
            );
            doc.Save(XmlPath);
            return doc;
        }

        public static XElement EnsureSection(XDocument doc, string sectionName)
        {
            var root = doc.Root!;
            var section = root.Element(sectionName);
            if (section == null)
            {
                section = new XElement(sectionName);
                root.Add(section);
            }
            return section;
        }

        public void Save()
        {
            var doc = LoadOrCreateDocument();
            var productsRoot = EnsureSection(doc, "Products");

            var existing = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (existing != null)
            {
                existing.Element("Name")!.Value = Name;
                existing.Element("Unit")!.Value = Unit;
                existing.Element("Quantity")!.Value = Quantity.ToString();
                existing.Element("Purchased")!.Value = Purchased.ToString();
                existing.Element("Category")!.Value = Category;
            }
            else
            {
                productsRoot.Add(new XElement("Product",
                    new XAttribute("Id", Id),
                    new XElement("Name", Name),
                    new XElement("Unit", Unit),
                    new XElement("Quantity", Quantity),
                    new XElement("Purchased", Purchased),
                    new XElement("Category", Category)
                ));
            }

            doc.Save(XmlPath);
        }

        public void Delete()
        {
            if (!File.Exists(XmlPath)) return;

            var doc = XDocument.Load(XmlPath);
            var productsRoot = EnsureSection(doc, "Products");

            var node = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (node != null)
            {
                node.Remove();
                doc.Save(XmlPath);
            }
        }

        public static Product Load(string id)
        {
            var doc = LoadOrCreateDocument();
            var productsRoot = EnsureSection(doc, "Products");

            var node = productsRoot.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == id);
            if (node == null)
            {
                return new Product { Id = id };
            }

            return new Product
            {
                Id = id,
                Name = node.Element("Name")?.Value ?? string.Empty,
                Unit = node.Element("Unit")?.Value ?? "szt.",
                Quantity = double.TryParse(node.Element("Quantity")?.Value, out var q) ? q : 1,
                Purchased = bool.TryParse(node.Element("Purchased")?.Value, out var p) && p,
                Category = node.Element("Category")?.Value ?? string.Empty
            };
        }

        public static IEnumerable<Product> LoadAll()
        {
            if (!File.Exists(XmlPath))
            {
                return new List<Product>();
            }

            var doc = XDocument.Load(XmlPath);
            var productsRoot = EnsureSection(doc, "Products");

            var products = productsRoot.Elements("Product").Select(node => new Product
            {
                Id = node.Attribute("Id")?.Value ?? Guid.NewGuid().ToString(),
                Name = node.Element("Name")?.Value ?? string.Empty,
                Unit = node.Element("Unit")?.Value ?? "szt.",
                Quantity = double.TryParse(node.Element("Quantity")?.Value, out var q) ? q : 1,
                Purchased = bool.TryParse(node.Element("Purchased")?.Value, out var p) && p,
                Category = node.Element("Category")?.Value ?? string.Empty
            });

            return products;
        }
    }
}
