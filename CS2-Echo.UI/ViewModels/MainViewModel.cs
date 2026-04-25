using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CS2_Echo.Infrastructure.Services;

namespace CS2_Echo.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{

    [ObservableProperty]
    public partial string WindowTitle { get; set; } = "CS2 Echo";

    public MainViewModel()
    {
    }
}

