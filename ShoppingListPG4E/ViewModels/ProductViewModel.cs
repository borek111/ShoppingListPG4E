using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Xml.Linq;
using ShoppingListPG4E.Models;

namespace ShoppingListPG4E.ViewModels
{
    public class ProductViewModel : ObservableObject
    {
        private Product _product;

        public ObservableCollection<string> Units { get; private set; }
        public ObservableCollection<string> Categories { get; private set; }
        public ObservableCollection<string> Stores { get; private set; }

        private bool _suppressCategoryPrompt = false;
        private bool _suppressUnitPrompt = false;
        private bool _suppressStorePrompt = false;

        // Callbacks
        public Action<ProductViewModel>? PurchasedChangedCallback { get; set; }
        public Action<ProductViewModel>? DeletedCallback { get; set; }

        public ProductViewModel()
        {
            _product = new Product();
            LoadUnits();
            LoadCategories();
            LoadStores();

            if (Units != null && Units.Count > 0 && string.IsNullOrEmpty(_product.Unit))
                _product.Unit = Units[0];
            if (Categories != null && Categories.Count > 0 && string.IsNullOrEmpty(_product.Category))
                _product.Category = Categories[0];
            if (Stores != null && Stores.Count > 0 && string.IsNullOrEmpty(_product.Store))
                _product.Store = Stores[0];

            InitializeCommands();
        }

        public ProductViewModel(Product product)
        {
            _product = product;
            LoadUnits();
            LoadCategories();
            LoadStores();

            if (string.IsNullOrEmpty(_product.Unit) && Units != null && Units.Count > 0)
                _product.Unit = Units[0];
            if (string.IsNullOrEmpty(_product.Category) && Categories != null && Categories.Count > 0)
                _product.Category = Categories[0];
            if (string.IsNullOrEmpty(_product.Store) && Stores != null && Stores.Count > 0)
                _product.Store = Stores[0];

            InitializeCommands();
        }

        // Properties
        public string Name
        {
            get => _product.Name;
            set
            {
                string cleaned = value?.Trim() ?? string.Empty;
                if (_product.Name != cleaned)
                {
                    _product.Name = cleaned;
                    OnPropertyChanged();
                    (AddCommand as RelayCommand)?.NotifyCanExecuteChanged();
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
                    _ = PromptForCustomUnitAsync(_product.Unit);
                    return;
                }

                _product.Unit = value;
                OnPropertyChanged();
            }
        }

        public string Category
        {
            get => _product.Category;
            set
            {
                if (_product.Category == value) return;

                if (_suppressCategoryPrompt)
                {
                    _product.Category = value;
                    _suppressCategoryPrompt = false;
                    OnPropertyChanged();
                    return;
                }

                if (value == "Inne...")
                {
                    _product.Category = value;
                    OnPropertyChanged();
                    _ = PromptForCustomCategoryAsync(_product.Category);
                    return;
                }

                _product.Category = value;
                OnPropertyChanged();
            }
        }

        public double Opacity => Purchased ? 0.35 : 1.0;


        public string Store
        {
            get => _product.Store;
            set
            {
                if (_product.Store == value) return;

                if (_suppressStorePrompt)
                {
                    _product.Store = value;
                    _suppressStorePrompt = false;
                    OnPropertyChanged();
                    return;
                }

                if (value == "Inne...")
                {
                    _product.Store = value;
                    OnPropertyChanged();
                    _ = PromptForCustomStoreAsync(_product.Store);
                    return;
                }

                _product.Store = value;
                OnPropertyChanged();
            }
        }

        public double Quantity
        {
            get => _product.Quantity;
            set
            {
                _product.Quantity = value;
                _product.Save();
                OnPropertyChanged();  
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
                    OnPropertyChanged(nameof(Opacity));
                    PurchasedChangedCallback?.Invoke(this);
                }
            }
        }

        public bool Optional
        {
            get => _product.Optional;
            set
            {
                if (_product.Optional != value)
                {
                    _product.Optional = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Identifier => _product.Id;

        // Commands
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
            AddCommand = new RelayCommand(AddProduct, CanAddProduct);
            CancelCommand = new RelayCommand(CancelProduct);
        }

        private bool CanAddProduct() => !string.IsNullOrWhiteSpace(_product.Name);

        // Command Methods
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
            DeletedCallback?.Invoke(this);
        }

        private void TogglePurchased()
        {
            Purchased = !Purchased;
        }

        private void AddProduct()
        {
            if (string.IsNullOrWhiteSpace(_product.Name))
                return;

            if (string.IsNullOrWhiteSpace(_product.Category) && Categories != null && Categories.Count > 0)
                _product.Category = Categories[0];

            if (string.IsNullOrWhiteSpace(_product.Store) && Stores != null && Stores.Count > 0)
                _product.Store = Stores[0];

            _product.Save();
            Shell.Current.GoToAsync($"..?saved={_product.Id}");
        }

        private void CancelProduct()
        {
            _product.Delete();
            Shell.Current.GoToAsync($"..?deleted={_product.Id}");
        }

        private void LoadCategories()
        {
            try
            {
                XDocument doc = Product.LoadOrCreateDocument();
                XElement categoriesRoot = Product.EnsureSection(doc, "Categories");
                List<string> list = categoriesRoot.Elements("Category").Select(x => x.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                Categories = new ObservableCollection<string>(list);
            }
            catch
            {
                Categories = new ObservableCollection<string> { "Nabiał", "Warzywa", "Owoce", "Elektronika", "AGD", "Inne..." };
            }

            OnPropertyChanged(nameof(Categories));
        }

        private void SaveCategories()
        {
            XDocument doc = Product.LoadOrCreateDocument();
            XElement categoriesRoot = Product.EnsureSection(doc, "Categories");
            categoriesRoot.RemoveAll();
            foreach (string c in Categories)
                categoriesRoot.Add(new XElement("Category", c));
            doc.Save(Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml"));
        }

        private void AddCategoryToListAndSave(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            if (Categories.Any(u => string.Equals(u, category, StringComparison.OrdinalIgnoreCase))) return;

            int idx = Categories.IndexOf("Inne...");
            if (idx >= 0)
                Categories.Insert(idx, category);
            else
                Categories.Add(category);

            SaveCategories();
            OnPropertyChanged(nameof(Categories));
        }

        private void LoadUnits()
        {
            try
            {
                XDocument doc = Product.LoadOrCreateDocument();
                XElement unitsRoot = Product.EnsureSection(doc, "Units");
                List<string> list = unitsRoot.Elements("Unit").Select(u => u.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                Units = new ObservableCollection<string>(list);
            }
            catch
            {
                Units = new ObservableCollection<string> { "szt.", "kg", "l", "g", "opak.", "ml", "Inne..." };
            }

            OnPropertyChanged(nameof(Units));
        }

        private void SaveUnits()
        {
            XDocument doc = Product.LoadOrCreateDocument();
            XElement unitsRoot = Product.EnsureSection(doc, "Units");
            unitsRoot.RemoveAll();
            foreach (string u in Units)
                unitsRoot.Add(new XElement("Unit", u));
            doc.Save(Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml"));
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
            OnPropertyChanged(nameof(Units));
        }

        private void LoadStores()
        {
            try
            {
                XDocument doc = Product.LoadOrCreateDocument();
                XElement storesRoot = Product.EnsureSection(doc, "Stores");
                List<string> list = storesRoot.Elements("Store").Select(s => s.Value).ToList();
                if (!list.Contains("Inne...")) list.Add("Inne...");
                Stores = new ObservableCollection<string>(list);
            }
            catch
            {
                Stores = new ObservableCollection<string> { "Biedronka", "Lidl", "Selgros", "Auchan", "Inne..." };
            }

            OnPropertyChanged(nameof(Stores));
        }

        private void SaveStores()
        {
            XDocument doc = Product.LoadOrCreateDocument();
            XElement storesRoot = Product.EnsureSection(doc, "Stores");
            storesRoot.RemoveAll();
            foreach (string s in Stores)
                storesRoot.Add(new XElement("Store", s));
            doc.Save(Path.Combine(FileSystem.AppDataDirectory, "ShoppingList.xml"));
        }

        private void AddStoreToListAndSave(string store)
        {
            if (string.IsNullOrWhiteSpace(store)) return;
            if (Stores.Any(s => string.Equals(s, store, StringComparison.OrdinalIgnoreCase))) return;

            int idx = Stores.IndexOf("Inne...");
            if (idx >= 0)
                Stores.Insert(idx, store);
            else
                Stores.Add(store);

            SaveStores();
            OnPropertyChanged(nameof(Stores));
        }

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
                    AddUnitToListAndSave(result.Trim());
                    _suppressUnitPrompt = true;
                    _product.Unit = result.Trim();
                    OnPropertyChanged(nameof(Unit));
                }
                else
                {
                    _suppressUnitPrompt = true;
                    _product.Unit = previousValue ?? Units.FirstOrDefault();
                    OnPropertyChanged(nameof(Unit));
                }
            });
        }

        private async Task PromptForCustomCategoryAsync(string previousValue)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                string result = await Shell.Current.DisplayPromptAsync(
                    "Nowa kategoria",
                    "Wpisz nazwę kategorii (np. Nabiał):",
                    "Zapisz",
                    "Anuluj",
                    placeholder: "np. Nabiał",
                    maxLength: -1,
                    keyboard: Keyboard.Text);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    AddCategoryToListAndSave(result.Trim());
                    _suppressCategoryPrompt = true;
                    _product.Category = result.Trim();
                    OnPropertyChanged(nameof(Category));
                }
                else
                {
                    _suppressCategoryPrompt = true;
                    _product.Category = previousValue ?? Categories.FirstOrDefault();
                    OnPropertyChanged(nameof(Category));
                }
            });
        }

        private async Task PromptForCustomStoreAsync(string previousValue)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                string result = await Shell.Current.DisplayPromptAsync(
                    "Nowy sklep",
                    "Wpisz nazwę sklepu (np. Biedronka):",
                    "Zapisz",
                    "Anuluj",
                    placeholder: "np. Biedronka",
                    maxLength: -1,
                    keyboard: Keyboard.Text);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    AddStoreToListAndSave(result.Trim());
                    _suppressStorePrompt = true;
                    _product.Store = result.Trim();
                    OnPropertyChanged(nameof(Store));
                }
                else
                {
                    _suppressStorePrompt = true;
                    _product.Store = previousValue ?? Stores.FirstOrDefault();
                    OnPropertyChanged(nameof(Store));
                }
            });
        }

        public void Reload()
        {
            _product = Product.Load(_product.Id);
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Unit));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(Store));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(Purchased));
            OnPropertyChanged(nameof(Opacity));
        }
    }
}
