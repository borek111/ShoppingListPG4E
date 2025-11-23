using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace ShoppingListPG4E.ViewModels
{
    public class AllProductsViewModel : ObservableObject, IQueryAttributable
    {
        public ObservableCollection<CategoryViewModel> Categories { get; } = new ObservableCollection<CategoryViewModel>();

        public ICommand NewCommand { get; }

        private readonly Dictionary<string, CategoryViewModel> _categoryViewModels = new(StringComparer.OrdinalIgnoreCase);

        private string CategoriesFile => Path.Combine(FileSystem.AppDataDirectory, "categories.xml");

        public AllProductsViewModel()
        {
            NewCommand = new RelayCommand(OpenAddProductPage);
            LoadCategoriesAndProducts();
        }

        private void OpenAddProductPage()
        {
            Shell.Current.GoToAsync(nameof(ShoppingListPG4E.Views.ProductPage));
        }

        public void LoadCategoriesAndProducts()
        {
            Categories.Clear();
            _categoryViewModels.Clear();
            var definedCategories = LoadCategoriesFromXml();

            var products = Models.Product.LoadAll()
                             .Select(p => new ProductViewModel(p))
                             .ToList();

            var categoryMap = new Dictionary<string, List<ProductViewModel>>(StringComparer.OrdinalIgnoreCase);

            // inicjalizuj wpisy dla zdefiniowanych kategorii
            foreach (var c in definedCategories)
                if (!categoryMap.ContainsKey(c))
                    categoryMap[c] = new List<ProductViewModel>();

            // rozdziel produkty do słownika, dodając brakujące kategorie
            foreach (var pv in products)
            {
                var catName = string.IsNullOrWhiteSpace(pv.Category) ? "Bez kategorii" : pv.Category;
                if (!categoryMap.ContainsKey(catName))
                {
                    categoryMap[catName] = new List<ProductViewModel>();
                    definedCategories.Add(catName);
                }
                categoryMap[catName].Add(pv);
            }

            // wyswietl bez "Inne..."
            var displayCategories = categoryMap.Keys
                .Where(c => !string.Equals(c, "Inne...", StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => definedCategories.FindIndex(dc => dc.Equals(c, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // buduj CategoryViewModel
            foreach (var catName in displayCategories)
            {
                var list = categoryMap[catName]
                    .OrderBy(p => p.Purchased)
                    .ThenBy(p => p.Name)
                    .ToList();

                var catVm = new CategoryViewModel(catName, list);

                foreach (var pv in catVm.Products)
                {
                    pv.PurchasedChangedCallback = _ => catVm.RefreshOrder();
                    pv.DeletedCallback = OnProductDeleted;
                }

                _categoryViewModels[catName] = catVm;
                Categories.Add(catVm);
            }
        }

        private void OnProductDeleted(ProductViewModel deleted)
        {
            var cat = _categoryViewModels.Values.FirstOrDefault(c => c.Products.Contains(deleted));
            if (cat != null)
            {
                cat.Products.Remove(deleted);
                cat.RefreshOrder();
            }
        }

        private List<string> LoadCategoriesFromXml()
        {
            try
            {
                if (!File.Exists(CategoriesFile))
                {
                    var defaults = new List<string> { "Nabiał", "Warzywa", "Owoce", "Elektronika", "AGD", "Inne..." };
                    SaveCategoriesToXml(defaults);
                    return defaults;
                }

                var doc = XDocument.Load(CategoriesFile);
                var list = doc.Root!.Elements("Category").Select(x => x.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                return list;
            }
            catch
            {
                return new List<string> { "Nabiał", "Warzywa", "Owoce", "Elektronika", "AGD", "Inne..." };
            }
        }

        private void SaveCategoriesToXml(IEnumerable<string> categories)
        {
            var doc = new XDocument(new XElement("Categories", categories.Select(c => new XElement("Category", c))));
            doc.Save(CategoriesFile);
        }

        public void RefreshAllCategoriesOrdering()
        {
            foreach (var cat in Categories)
                cat.RefreshOrder();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("saved"))
            {
                LoadCategoriesAndProducts();
            }
        }
    }
}
