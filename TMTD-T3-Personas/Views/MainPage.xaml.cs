using TMTD_T3_Personas.ViewModels;

namespace TMTD_T3_Personas;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new PersonaViewModel();
    }
}