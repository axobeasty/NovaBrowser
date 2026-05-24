using Windows.UI.StartScreen;

namespace NovaBrowser.Services;

public static class JumpListService
{
    public static void Configure(string homePage)
    {
        try
        {
            var jumpList = JumpList.LoadCurrentAsync().AsTask().GetAwaiter().GetResult();
            if (jumpList is null)
            {
                return;
            }

            jumpList.Items.Clear();
            jumpList.Items.Add(JumpListItem.CreateWithArguments("/newtab", "New tab"));
            jumpList.Items.Add(JumpListItem.CreateWithArguments("/home", "Home"));
            jumpList.Items.Add(JumpListItem.CreateWithArguments("/private", "Private window"));

            if (!string.IsNullOrWhiteSpace(homePage) &&
                Uri.TryCreate(homePage, UriKind.Absolute, out _))
            {
                jumpList.Items.Add(JumpListItem.CreateWithArguments($"/open {homePage}", "Open home page"));
            }

            jumpList.SystemGroupKind = JumpListSystemGroupKind.Recent;
            _ = jumpList.SaveAsync();
        }
        catch
        {
            // Jump lists are optional.
        }
    }
}
