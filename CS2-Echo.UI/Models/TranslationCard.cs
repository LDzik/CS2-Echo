using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CS2_Echo.UI.Models;

public partial class TranslationCard : ObservableObject
{
    public string PlayerName { get; set; }
    public string Channel { get; set; } // ALL, T, CT
    public string OriginalText { get; set; }
    public string TranslatedText { get; set; }
    public string SourceLang { get; set; }
    public string ConfidenceLog { get; set; }

    [ObservableProperty] public partial string PrimaryLang { get; set; }

    [ObservableProperty] public partial bool CanIgnorePlayer { get; set; } = true;
}

