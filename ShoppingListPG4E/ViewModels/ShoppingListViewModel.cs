using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShoppingListPG4E.ViewModels
{
    public class ShoppingListViewModel : ObservableObject
    {
        public ObservableCollection<ProductViewModel> Items { get; } = new ObservableCollection<ProductViewModel>();

        private readonly List<string> _categoryOrder;

        public ShoppingListViewModel()
        {
            _categoryOrder = LoadCategoriesFromXml();
            LoadShoppingItems();
        }

        private List<string> LoadCategoriesFromXml()
        {
            try
            {
                var doc = Models.Product.LoadOrCreateDocument();
                var categoriesRoot = Models.Product.EnsureSection(doc, "Categories");
                var list = categoriesRoot.Elements("Category").Select(x => x.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                if (!list.Contains("Bez kategorii")) list.Add("Bez kategorii");
                return list;
            }
            catch
            {
                return new List<string> { "Nabia³", "Warzywa", "Owoce", "Elektronika", "AGD", "Inne...", "Bez kategorii" };
            }
        }

        public void LoadShoppingItems()
        {
            Items.Clear();

            var products = Models.Product.LoadAll()
                .Where(p => !p.Purchased) //only unpurchased
                .Select(p => new ProductViewModel(p))
                .ToList();

            int CategoryIndex(string? cat)
            {
                var name = string.IsNullOrWhiteSpace(cat) ? "Bez kategorii" : cat!;
                var idx = _categoryOrder.FindIndex(c => c.Equals(name, StringComparison.OrdinalIgnoreCase));
                return idx >= 0 ? idx : int.MaxValue;
            }

            var ordered = products
                .OrderBy(pv => CategoryIndex(pv.Category))
                .ThenBy(pv => pv.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var pv in ordered)
            {
                // After marking as purchased - remove from view
                pv.PurchasedChangedCallback = OnPurchasedChanged;
                pv.DeletedCallback = OnDeleted;
                Items.Add(pv);
            }
        }

        private void OnPurchasedChanged(ProductViewModel changed)
        {
            // if it was marked as purchased – remove from the list
            if (changed.Purchased)
            {
                Items.Remove(changed);
            }
        }

        private void OnDeleted(ProductViewModel deleted)
        {
            Items.Remove(deleted);
        }
    }
}