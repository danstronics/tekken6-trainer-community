using System.Globalization;

namespace CommunityTrainer;

internal readonly record struct LocalizedText(string English, string Chinese)
{
    public string Get(bool chinese) => chinese ? Chinese : English;
}

internal enum UiLanguage { Automatic, English, Chinese }

internal sealed class LanguagePreference(string path)
{
    public static LanguagePreference ForCurrentUser() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tekken6.Community", "language.txt"));

    public static bool UsesChinese(UiLanguage language, CultureInfo systemUiCulture) =>
        language == UiLanguage.Chinese || language == UiLanguage.Automatic &&
        systemUiCulture.TwoLetterISOLanguageName == "zh";

    public UiLanguage Load()
    {
        try
        {
            return File.ReadAllText(path).Trim() switch
            {
                "en" => UiLanguage.English,
                "zh-Hans" => UiLanguage.Chinese,
                _ => UiLanguage.Automatic
            };
        }
        catch (IOException) { return UiLanguage.Automatic; }
        catch (UnauthorizedAccessException) { return UiLanguage.Automatic; }
    }

    public bool Save(UiLanguage language)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, language switch
            {
                UiLanguage.English => "en",
                UiLanguage.Chinese => "zh-Hans",
                _ => "auto"
            });
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
