using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using ShoppingListPG4E.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;

namespace ShoppingListPG4E.ViewModels
{
    public class ProductViewModel : ObservableObject
    {
        private Product _product;

        public ObservableCollection<string> Units { get; private set; }

        private string UnitsFile => Path.Combine(FileSystem.AppDataDirectory, "units.xml");

        // flaga żeby uniknąć re-entrancy przy programowym ustawianiu Unit
        private bool _suppressUnitPrompt = false;

        public ProductViewModel()
        {
            _product = new Product();
            LoadUnits();
            if (Units.Count > 0) Unit = Units[0];
            InitializeCommands();
        }

        public ProductViewModel(Product product)
        {
            _product = product;
            LoadUnits();
            if (string.IsNullOrEmpty(_product.Unit) && Units.Count > 0) Unit = Units[0];
            InitializeCommands();
        }

        // --- podstawowe właściwości ---
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
                if (_product.Unit == value) return;

                if (_suppressUnitPrompt)
                {
                    _product.Unit = value;
                    _suppressUnitPrompt = false;
                    OnPropertyChanged();
                    return;
                }

                if (value == "Inne...")
                {
                    _product.Unit = value;
                    OnPropertyChanged();
                    PromptForCustomUnitAsync(_product.Unit);
                    return;
                }

                // normalne ustawienie
                _product.Unit = value;
                OnPropertyChanged();
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

        //XML: load/save Units 
        private void LoadUnits()
        {
           
            if (File.Exists(UnitsFile))
            {
                var doc = XDocument.Load(UnitsFile);
                Units = new ObservableCollection<string>(
                    doc.Root.Elements("Unit").Select(u => u.Value)
                );
            }
            else
            {
                Units = new ObservableCollection<string> { "szt.", "kg", "l", "g", "opak.", "ml" };
                SaveUnits();
            }

            // Dodaj "Inne..." na końcu, jeśli jeszcze nie ma
            if (!Units.Contains("Inne..."))
                Units.Add("Inne...");
           

            OnPropertyChanged(nameof(Units));
        }

        private void SaveUnits()
        {
            var doc = new XDocument(new XElement("Units", Units.Select(u => new XElement("Unit", u))));
            doc.Save(UnitsFile); 
        }

        private void AddUnitToListAndSave(string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return;
            if (Units.Any(u => string.Equals(u, unit, StringComparison.OrdinalIgnoreCase))) return;

            int idx = Units.IndexOf("Inne...");
            if (idx >= 0)
                Units.Insert(idx, unit);
            else
                Units.Add(unit);

            SaveUnits();
        }




        // --- prompt dla "Inne..." ---
        private async Task PromptForCustomUnitAsync(string previousValue)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                string result = await Shell.Current.DisplayPromptAsync(
                    "Nowa jednostka",
                    "Wpisz nazwę jednostki (np. karton):",
                    "Zapisz",
                    "Anuluj",
                    placeholder: "np. karton",
                    maxLength: -1,
                    keyboard: Keyboard.Text);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    // dodaj
                    AddUnitToListAndSave(result.Trim());
                    _suppressUnitPrompt = true;
                    _product.Unit = result.Trim();
                    OnPropertyChanged(nameof(Unit));
                }
                else
                {
                    // anulowano, przywróć poprzednią
                    _suppressUnitPrompt = true;
                    _product.Unit = previousValue;
                    OnPropertyChanged(nameof(Unit));
                }
            });
        }

        // reload produktu z xml
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
