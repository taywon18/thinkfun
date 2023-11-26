using CommunityToolkit.Maui.Alerts;
using System.ComponentModel;
using System.Windows.Input;

namespace ThinkFun.Views;

public partial class LoginPage 
	: ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		this.BindingContext = this;
	}

	public async Task Login(CancellationToken tk = default)
	{
		if (String.IsNullOrWhiteSpace(Mail.Text))
		{
			Mail.Focus();
            await DisplaySnackBar("Merci d'entrer un nom d'utilisateur ou un mail", tk);
            return;
        }

		if(String.IsNullOrEmpty(Password.Text))
		{
            Password.Focus();
            await DisplaySnackBar("Merci d'entrer un mot de passe", tk);
            return;
        }

        string mail = Mail.Text.Trim();
        string password = Password.Text;

		var result = await LoginManager.Instance.Login(mail, password, tk);
		if (result == null)
		{
            await DisplaySnackBar("Échec de l'authentification", tk);
            return;
        }

        ((AppShell)Shell.Current).FlushConnectionState(false);
        await Navigation.PopAsync();
        return;
    }

    private async Task DisplaySnackBar(string message, CancellationToken tk = default)
    {
        var toast = Snackbar.Make(message);
        HideKeyboard();
        await toast.Show(tk);
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
		Mail.IsEnabled = false;
		Password.IsEnabled = false;
		Validation.IsEnabled = false;

		this.IsBusy = true;

        await Login();

        this.IsBusy = false;

        Mail.IsEnabled = true;
        Password.IsEnabled = true;
        Validation.IsEnabled = true;
    }

	private void HideKeyboard()
	{
#if ANDROID
			if(Platform.CurrentActivity.CurrentFocus != null)
			{

				Platform.CurrentActivity.CurrentFocus.ClearFocus();
			}
#endif


    }
}