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

        public ObservableCollection<string> Stores { get; } = new ObservableCollection<string>();

        public ICommand NewCommand { get; }

        private readonly Dictionary<string, CategoryViewModel> _categoryViewModels = new(StringComparer.OrdinalIgnoreCase);

        private string _selectedStore = "Wszystkie"; 
        public string SelectedStore
        {
            get => _selectedStore;
            set
            {
                var newVal = value ?? "Wszystkie";
                if (_selectedStore != newVal)
                {
                    _selectedStore = newVal;
                    OnPropertyChanged();
                    LoadCategoriesAndProducts(); 
                }
            }
        }

        public AllProductsViewModel()
        {
            NewCommand = new RelayCommand(OpenAddProductPage);
            LoadStores();
            LoadCategoriesAndProducts();

        }

        private void OpenAddProductPage()
        {
            // przekazuj wybrany sklep, ale pomiń "Wszystkie" (brak filtra)
            var store = string.Equals(SelectedStore, "Wszystkie", StringComparison.OrdinalIgnoreCase) ? string.Empty : SelectedStore;
            var storeQuery = string.IsNullOrWhiteSpace(store) ? string.Empty : $"?store={Uri.EscapeDataString(store)}";
            Shell.Current.GoToAsync($"{nameof(ShoppingListPG4E.Views.ProductPage)}{storeQuery}");
        }

        private List<string> LoadCategoriesFromXml()
        {
            try
            {
                var doc = Models.Product.LoadOrCreateDocument();
                var categoriesRoot = Models.Product.EnsureSection(doc, "Categories");
                var list = categoriesRoot.Elements("Category").Select(x => x.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                return list;
            }
            catch
            {
                return new List<string> { "Nabiał", "Warzywa", "Owoce", "Elektronika", "AGD", "Inne..." };
            }
        }

        private void LoadStores()
        {
            Stores.Clear();
            Stores.Add("Wszystkie");

            try
            {
                var doc = Models.Product.LoadOrCreateDocument();
                var storesRoot = Models.Product.EnsureSection(doc, "Stores");
                var list = storesRoot.Elements("Store").Select(x => x.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                foreach (var s in list) Stores.Add(s);
            }
            catch
            {
                foreach (var s in new[] { "Biedronka", "Lidl", "Selgros", "Auchan", "Inne..." })
                    Stores.Add(s);
            }

            // jeśli SelectedStore nie jest na liście (np. po zmianach), ustaw "Wszystkie"
            if (!Stores.Contains(SelectedStore))
                SelectedStore = "Wszystkie";
        }

        public void LoadCategoriesAndProducts()
        {
            ResetCollections();

            var definedCategories = LoadCategoriesFromXml();
            var products = LoadAllProducts();

            if (!string.Equals(SelectedStore, "Wszystkie", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(SelectedStore))
            {
                products = products
                    .Where(p => string.Equals(p.Store, SelectedStore, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var categoryMap = BuildCategoryMap(definedCategories, products);
            var displayCategories = ComputeDisplayCategories(categoryMap.Keys, definedCategories);

            BuildAndAttachCategoryViewModels(displayCategories, categoryMap);
        }

        private void ResetCollections()
        {
            Categories.Clear();
            _categoryViewModels.Clear();
        }

        private List<ProductViewModel> LoadAllProducts()
        {
            return Models.Product.LoadAll()
                .Select(p => new ProductViewModel(p))
                .ToList();
        }

        private Dictionary<string, List<ProductViewModel>> BuildCategoryMap(List<string> definedCategories, List<ProductViewModel> products)
        {
            var categoryMap = new Dictionary<string, List<ProductViewModel>>(StringComparer.OrdinalIgnoreCase);

            // inicjalizuj wpisy dla zdefiniowanych kategorii
            foreach (var c in definedCategories)
            {
                if (!categoryMap.ContainsKey(c))
                    categoryMap[c] = new List<ProductViewModel>();
            }

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

            return categoryMap;
        }

        private List<string> ComputeDisplayCategories(IEnumerable<string> categoriesInMap, List<string> definedCategories)
        {
            return categoriesInMap
                .Where(c => !string.Equals(c, "Inne...", StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => definedCategories.FindIndex(dc => dc.Equals(c, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private void BuildAndAttachCategoryViewModels(List<string> displayCategories, Dictionary<string, List<ProductViewModel>> categoryMap)
        {
            foreach (var catName in displayCategories)
            {
                var orderedProducts = categoryMap[catName]
                    .OrderBy(p => p.Purchased)
                    .ThenBy(p => p.Name)
                    .ToList();

                var catVm = new CategoryViewModel(catName, orderedProducts);

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

            if (query.TryGetValue("store", out var storeObj))
            {
                var store = storeObj?.ToString() ?? string.Empty;
                SelectedStore = string.IsNullOrWhiteSpace(store) ? "Wszystkie" : store;
            }
        }
    }
}
