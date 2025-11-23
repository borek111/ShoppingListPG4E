namespace ShoppingListPG4E.Views;

public partial class AllProductsPage : ContentPage
{
    public AllProductsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ShoppingListPG4E.ViewModels.AllProductsViewModel vm)
        {
            vm.LoadCategoriesAndProducts();
        }
    }
}