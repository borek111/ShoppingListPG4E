using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ShoppingListPG4E.Models
{
    public class Product
    {
        public string Id { get; set; } // unikalny identyfikator
        public string Name { get; set; }
        public string Unit { get; set; } // np. szt., kg, l
        public double Quantity { get; set; }
        public bool Purchased { get; set; } // czy kupione

        private static string XmlPath => Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml");

        public Product()
        {
            Id = Guid.NewGuid().ToString();
            Name = string.Empty;
            Unit = "szt.";
            Quantity = 1;
            Purchased = false;
        }

        public void Save()
        {
            XElement root;
            if (File.Exists(XmlPath))
                root = XElement.Load(XmlPath);
            else
                root = new XElement("Products");

            var existing = root.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (existing != null)
            {
                existing.Element("Name").Value = Name;
                existing.Element("Unit").Value = Unit;
                existing.Element("Quantity").Value = Quantity.ToString();
                existing.Element("Purchased").Value = Purchased.ToString();
            }
            else
            {
                root.Add(new XElement("Product",
                    new XAttribute("Id", Id),
                    new XElement("Name", Name),
                    new XElement("Unit", Unit),
                    new XElement("Quantity", Quantity),
                    new XElement("Purchased", Purchased)
                ));
            }

            root.Save(XmlPath);
        }

        public void Delete()
        {
            if (!File.Exists(XmlPath)) return;

            XElement root = XElement.Load(XmlPath);
            var node = root.Elements("Product").FirstOrDefault(x => x.Attribute("Id")?.Value == Id);
            if (node != null)
            {
                node.Remove();
                root.Save(XmlPath);
            }
        }

        public static Product Load(string id)
        {
            XElement root = XElement.Load(XmlPath);
            var node = root.Elements("Product").FirstOrDefault(x => x.Attribute("Id").Value == id);

            return new Product
            {
                Id = id,
                Name = node.Element("Name").Value,
                Unit = node.Element("Unit").Value,
                Quantity = double.Parse(node.Element("Quantity").Value),
                Purchased = bool.Parse(node.Element("Purchased").Value)
            };
        }

        public static IEnumerable<Product> LoadAll()
        {
            if (!File.Exists(XmlPath)) return new List<Product>();

            XElement root = XElement.Load(XmlPath);
            var products = root.Elements("Product").Select(node => new Product
            {
                Id = node.Attribute("Id").Value,
                Name = node.Element("Name").Value,
                Unit = node.Element("Unit").Value,
                Quantity = double.Parse(node.Element("Quantity").Value),
                Purchased = bool.Parse(node.Element("Purchased").Value)
            });

            // Najpierw niekupione, potem kupione
            return products.OrderBy(p => p.Purchased).ThenByDescending(p => p.Id);
        }
    }
}
