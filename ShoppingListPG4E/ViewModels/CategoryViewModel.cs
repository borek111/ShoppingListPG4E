using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ShoppingListPG4E.ViewModels
{
    public class CategoryViewModel : ObservableObject
    {
        public string Name { get; }
        public ObservableCollection<ProductViewModel> Products { get; }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleExpandCommand { get; }

        public CategoryViewModel(string name, IEnumerable<ProductViewModel> products = null)
        {
            Name = name ?? string.Empty;
            Products = products != null
                ? new ObservableCollection<ProductViewModel>(products)
                : new ObservableCollection<ProductViewModel>();
            ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
            IsExpanded = false;
        }

        public void RefreshOrder()
        {
            var ordered = Products
                .OrderBy(productViewModel => productViewModel.Purchased)
                .ThenBy(productViewModel => productViewModel.Name)
                .ToList();

            Products.Clear();
            foreach (var productViewModel in ordered)
                Products.Add(productViewModel);
        }
    }
}