namespace ShoppingListPG4E.Views
{
    public partial class ShoppingListPage : ContentPage
    {
        public ShoppingListPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ShoppingListPG4E.ViewModels.ShoppingListViewModel vm)
            {
                // refresh after returning to the tab
                vm.LoadShoppingItems();
            }
        }
    }
}