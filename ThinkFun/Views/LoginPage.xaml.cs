using System.Windows.Input;

namespace ThinkFun.Views;

public partial class LoginPage : ContentPage
{
	public ICommand LoginCommand { get; }
	private object WorkingMutex = new object();

	public LoginPage()
	{
		InitializeComponent();
		BindingContext = this;

		LoginCommand = new Command(async () =>
		{
			await Login();
		});
	}

	public async Task Login()
	{
		lock (WorkingMutex)
		{
            string mail = Mail.Text.Trim();
            string password = Password.Text.Trim();

			if (mail == string.Empty || password == string.Empty)
				return;


        }
    }
}