namespace ShoppingListPG4E
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ShoppingListPG4E.Views.ProductPage), typeof(ShoppingListPG4E.Views.ProductPage));
        }
    }
}
