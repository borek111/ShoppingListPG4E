namespace ShoppingListPG4E.Views;

public partial class AllProductsPage : ContentPage
{
    public AllProductsPage()
    {
        InitializeComponent();
        BindingContext = new ShoppingListPG4E.ViewModels.ProductsViewModel();
    }
}