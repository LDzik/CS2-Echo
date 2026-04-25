using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CS2_Echo.UI.Converters;

public class LanguageToFlagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string countryCode = "un";

        if (value is string langCode && !string.IsNullOrWhiteSpace(langCode))
        {
            countryCode = langCode.ToLower() switch
            {
                "en" => "us", // gb
                "ja" => "jp", // Japanese -> Japan
                "cs" => "cz", // Czech -> Czechia
                "ko" => "kr", // Korean -> South Korea
                "zh" => "cn", // Chinese -> China
                "uk" => "ua", // Ukrainian -> Ukraine
                "da" => "dk", // Danish -> Denmark
                "el" => "gr", // Greek -> Greece
                "sv" => "se", // Swedish -> Sweden
                { Length: 2 } code => code, 
                _ => countryCode
            };
            
        }
        return $"https://flagcdn.com/h40/{countryCode}.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

