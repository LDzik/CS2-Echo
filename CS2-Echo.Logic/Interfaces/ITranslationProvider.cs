using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CS2_Echo.Logic.Interfaces;

public interface ITranslationProvider
{
    string Name { get; }

    Task<(string TranslatedText, string DetectedSourceLang)> TranslateAsync(string text, string targetLang);
}
