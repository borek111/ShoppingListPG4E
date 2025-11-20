using ShoppingListPG4E.Models;
using ShoppingListPG4E.ViewModels;

namespace ShoppingListPG4E.Views;

public partial class ProductPage : ContentPage
{
    ProductViewModel VM => BindingContext as ProductViewModel;

    public ProductPage()
    {
        InitializeComponent();
    }

}
