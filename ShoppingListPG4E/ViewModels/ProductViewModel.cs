using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShoppingListPG4E.Models;
using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace ShoppingListPG4E.ViewModels
{
    internal class ProductViewModel : ObservableObject
    {
        public List<string> Units { get; } = new List<string> { "szt.", "kg", "l", "g", "opak.", "ml" };

        private Product _product;

        public string Name
        {
            get => _product.Name;
            set
            {
                if (_product.Name != value)
                {
                    _product.Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Unit
        {
            get => _product.Unit;
            set
            {
                if (_product.Unit != value)
                {
                    _product.Unit = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Quantity
        {
            get => _product.Quantity;
            set
            {
                if (Math.Abs(_product.Quantity - value) > 1e-6)
                {
                    _product.Quantity = value;
                    _product.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool Purchased
        {
            get => _product.Purchased;
            set
            {
                if (_product.Purchased != value)
                {
                    _product.Purchased = value;
                    _product.Save();
                    OnPropertyChanged();
                    // powoduje przesunięcie elementu w AllProductsPage
                    Shell.Current.GoToAsync($"..?toggled={Identifier}");
                }
            }
        }

        public string Identifier => _product.Id;

        public ICommand IncreaseCommand { get; private set; }
        public ICommand DecreaseCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand TogglePurchasedCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public ProductViewModel()
        {
            _product = new Product();
            Quantity = _product.Quantity;
            if (Units.Count > 0) Unit = Units[0];
            InitializeCommands();
        }

        public ProductViewModel(Product product)
        {
            _product = product;
            if (string.IsNullOrEmpty(_product.Unit) && Units.Count > 0) Unit = Units[0];
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            IncreaseCommand = new RelayCommand(Increase);
            DecreaseCommand = new RelayCommand(Decrease);
            DeleteCommand = new RelayCommand(Delete);
            TogglePurchasedCommand = new RelayCommand(TogglePurchased);
            AddCommand = new RelayCommand(AddProduct);
            CancelCommand = new RelayCommand(CancelProduct);
        }

        private void Increase()
        {
            _product.Quantity++;
            _product.Save();
            OnPropertyChanged(nameof(Quantity));
        }

        private void Decrease()
        {
            _product.Quantity--;
            _product.Save();
            OnPropertyChanged(nameof(Quantity));
        }

        private void Delete()
        {
            _product.Delete();
            Shell.Current.GoToAsync($"..?deleted={_product.Id}");
        }

        private void TogglePurchased()
        {
            Purchased = !Purchased;
        }

        private void AddProduct()
        {
            _product.Save();
            Shell.Current.GoToAsync($"..?saved={_product.Id}");
        }

        private void CancelProduct()
        {
            _product.Delete();
            Shell.Current.GoToAsync($"..?deleted={_product.Id}");
        }

        public void Reload()
        {
            _product = Product.Load(_product.Id);
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Unit));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(Purchased));
        }
    }
}
