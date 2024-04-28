using ReactiveUI;

namespace Wabbajack.Messages;

public enum ScreenType
{
    Home,
    ModListGallery,
    Installer,
    Settings,
    Compiler,
    CreateModList,
    ModListContents,
    WebBrowser
}

public class NavigateToGlobal
{

    public ScreenType Screen { get; }
    
    private NavigateToGlobal(ScreenType screen)
    {
        Screen = screen;
    }

    public static void Send(ScreenType screen)
    {
        MessageBus.Current.SendMessage(new NavigateToGlobal(screen));
    }

}