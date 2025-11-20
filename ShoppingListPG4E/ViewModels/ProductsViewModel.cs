using CommunityToolkit.Mvvm.Input;
using ShoppingListPG4E.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;
using System;

namespace ShoppingListPG4E.ViewModels
{
    internal class ProductsViewModel : IQueryAttributable
    {
        public ObservableCollection<ProductViewModel> AllProducts { get; }
        public ICommand NewCommand { get; }

        public ProductsViewModel()
        {
            AllProducts = new ObservableCollection<ProductViewModel>(
                Product.LoadAll().Select(p => new ProductViewModel(p))
            );
            NewCommand = new AsyncRelayCommand(NewProductAsync);
        }

        private async Task NewProductAsync()
        {
            await Shell.Current.GoToAsync(nameof(ShoppingListPG4E.Views.ProductPage));
        }

        void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("deleted"))
            {
                string id = query["deleted"].ToString();
                var matched = AllProducts.FirstOrDefault(x => x.Identifier == id);
                if (matched != null)
                    AllProducts.Remove(matched);
            }
            else if (query.ContainsKey("saved"))
            {
                string id = query["saved"].ToString();
                var matched = AllProducts.FirstOrDefault(n => n.Identifier == id);
                if (matched != null)
                {
                    matched.Reload();
                    AllProducts.Move(AllProducts.IndexOf(matched), 0);
                }
                else
                {
                    AllProducts.Insert(0, new ProductViewModel(Product.Load(id)));
                }
            }
            else if (query.ContainsKey("toggled"))
            {
                string id = query["toggled"].ToString();
                var matched = AllProducts.FirstOrDefault(n => n.Identifier == id);
                if (matched != null)
                {
                    // jeśli zaznaczony jako kupione -> przenieś na koniec
                    if (matched.Purchased)
                    {
                        var idx = AllProducts.IndexOf(matched);
                        if (idx >= 0)
                            AllProducts.Move(idx, AllProducts.Count - 1);
                    }
                    else
                    {
                        // jeśli odznaczony jako niekupione -> przenieś na początek
                        var idx = AllProducts.IndexOf(matched);
                        if (idx >= 0)
                            AllProducts.Move(idx, 0);
                    }
                }
            }
        }
    }
}
