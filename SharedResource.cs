using System.Globalization;
using System.Resources;

namespace English;

public sealed class SharedResource
{
    private static readonly ResourceManager ResourceManager = new(
        "English.Resources.SharedResource",
        typeof(SharedResource).Assembly);

    public static string FullName => Get(nameof(FullName));
    public static string Email => Get(nameof(Email));
    public static string Role => Get(nameof(Role));
    public static string CurrentPassword => Get(nameof(CurrentPassword));
    public static string NewPassword => Get(nameof(NewPassword));
    public static string ConfirmNewPassword => Get(nameof(ConfirmNewPassword));
    public static string RequiredFullName => Get(nameof(RequiredFullName));
    public static string MaxFullName => Get(nameof(MaxFullName));
    public static string PasswordMismatch => Get(nameof(PasswordMismatch));
    public static string RequiredCurrentPassword => Get(nameof(RequiredCurrentPassword));
    public static string RequiredNewPassword => Get(nameof(RequiredNewPassword));
    public static string MinNewPassword => Get(nameof(MinNewPassword));
    public static string RequiredConfirmNewPassword => Get(nameof(RequiredConfirmNewPassword));

    private static string Get(string name)
    {
        return ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
    }
}
