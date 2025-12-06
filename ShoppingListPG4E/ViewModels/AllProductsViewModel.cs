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
                var newValue = value ?? "Wszystkie";
                if (_selectedStore != newValue)
                {
                    _selectedStore = newValue;
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
            // pass selected store, but skip "Wszystkie" (no filter)
            var store = string.Equals(SelectedStore, "Wszystkie", StringComparison.OrdinalIgnoreCase) ? string.Empty : SelectedStore;
            var storeQuery = string.IsNullOrWhiteSpace(store) ? string.Empty : $"?store={Uri.EscapeDataString(store)}";
            Shell.Current.GoToAsync($"{nameof(ShoppingListPG4E.Views.ProductPage)}{storeQuery}");
        }

        private List<string> LoadCategoriesFromXml()
        {
            try
            {
                var document = Models.Product.LoadOrCreateDocument();
                var categoriesRoot = Models.Product.EnsureSection(document, "Categories");
                var categoryList = categoriesRoot
                    .Elements("Category")
                    .Select(categoryElement => categoryElement.Value)
                    .ToList();
                if (!categoryList.Contains("Inne...")) categoryList.Add("Inne...");
                return categoryList;
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
                var document = Models.Product.LoadOrCreateDocument();
                var storesRoot = Models.Product.EnsureSection(document, "Stores");
                var storeList = storesRoot
                    .Elements("Store")
                    .Select(storeElement => storeElement.Value)
                    .ToList();
                if (!storeList.Contains("Inne...")) storeList.Add("Inne...");
                foreach (var storeName in storeList) Stores.Add(storeName);
            }
            catch
            {
                foreach (var storeName in new[] { "Biedronka", "Lidl", "Selgros", "Auchan", "Inne..." })
                    Stores.Add(storeName);
            }

            // if SelectedStore is not in the list (e.g. after changes), set it to "Wszystkie"
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
                    .Where(product => string.Equals(product.Store, SelectedStore, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var categoryToProductsMap = BuildCategoryMap(definedCategories, products);
            var displayCategories = ComputeDisplayCategories(categoryToProductsMap.Keys, definedCategories);

            BuildAndAttachCategoryViewModels(displayCategories, categoryToProductsMap);
        }

        private void ResetCollections()
        {
            Categories.Clear();
            _categoryViewModels.Clear();
        }

        private List<ProductViewModel> LoadAllProducts()
        {
            return Models.Product.LoadAll()
                .Select(product => new ProductViewModel(product))
                .ToList();
        }

        private Dictionary<string, List<ProductViewModel>> BuildCategoryMap(List<string> definedCategories, List<ProductViewModel> products)
        {
            var categoryToProductsMap = new Dictionary<string, List<ProductViewModel>>(StringComparer.OrdinalIgnoreCase);

            // initialize entries for defined categories
            foreach (var category in definedCategories)
            {
                if (!categoryToProductsMap.ContainsKey(category))
                    categoryToProductsMap[category] = new List<ProductViewModel>();
            }

            // separate products into dictionary, adding missing categories
            foreach (var productViewModel in products)
            {
                var categoryName = string.IsNullOrWhiteSpace(productViewModel.Category) ? "Bez kategorii" : productViewModel.Category;

                if (!categoryToProductsMap.ContainsKey(categoryName))
                {
                    categoryToProductsMap[categoryName] = new List<ProductViewModel>();
                    definedCategories.Add(categoryName);
                }

                categoryToProductsMap[categoryName].Add(productViewModel);
            }

            return categoryToProductsMap;
        }

        private List<string> ComputeDisplayCategories(IEnumerable<string> categoriesInMap, List<string> definedCategories)
        {
            return categoriesInMap
                .Where(category => !string.Equals(category, "Inne...", StringComparison.OrdinalIgnoreCase))
                .OrderBy(category => definedCategories.FindIndex(definedCategory => definedCategory.Equals(category, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        private void BuildAndAttachCategoryViewModels(List<string> displayCategories, Dictionary<string, List<ProductViewModel>> categoryToProductsMap)
        {
            foreach (var categoryName in displayCategories)
            {
                var orderedProducts = categoryToProductsMap[categoryName]
                    .OrderBy(product => product.Purchased)
                    .ThenBy(product => product.Name)
                    .ToList();

                var categoryViewModel = new CategoryViewModel(categoryName, orderedProducts);

                foreach (var productViewModel in categoryViewModel.Products)
                {
                    productViewModel.PurchasedChangedCallback = _ => categoryViewModel.RefreshOrder();
                    productViewModel.DeletedCallback = OnProductDeleted;
                }

                _categoryViewModels[categoryName] = categoryViewModel;
                Categories.Add(categoryViewModel);
            }
        }

        private void OnProductDeleted(ProductViewModel deletedProductViewModel)
        {
            var categoryViewModel = _categoryViewModels.Values.FirstOrDefault(vm => vm.Products.Contains(deletedProductViewModel));
            if (categoryViewModel != null)
            {
                categoryViewModel.Products.Remove(deletedProductViewModel);
                categoryViewModel.RefreshOrder();
            }
        }

        public void RefreshAllCategoriesOrdering()
        {
            foreach (var categoryViewModel in Categories)
                categoryViewModel.RefreshOrder();
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
