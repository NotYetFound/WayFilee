using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.Views;
using Filee.App.Views.Pages;
using Filee.Core.Localization;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public sealed class MainWindowNavigationCacheTests
{
    [AvaloniaFact]
    public void Large_tabs_reuse_their_view_until_the_preset_library_changes()
    {
        TestServices.EnsureInitialized("en");
        var vm = new MainWindowViewModel(AppHost.Services, AppHost.Get<ILocalizer>(), AppHost.Get<UpdateService>());
        var window = new MainWindow { DataContext = vm };
        try
        {
            window.Show();
            vm.Navigate("toolbar");
            Pump();
            var toolbarModel = vm.CurrentPage;
            var toolbarView = window.GetVisualDescendants().OfType<ToolbarPage>().Single();

            vm.Navigate("presets");
            Pump();
            var presetsModel = vm.CurrentPage;
            var presetsView = window.GetVisualDescendants().OfType<PresetsPage>().Single();

            vm.Navigate("toolbar");
            Pump();
            Assert.Same(toolbarModel, vm.CurrentPage);
            Assert.Same(toolbarView, window.GetVisualDescendants().OfType<ToolbarPage>().Single());

            vm.Navigate("presets");
            Pump();
            Assert.Same(presetsModel, vm.CurrentPage);
            Assert.Same(presetsView, window.GetVisualDescendants().OfType<PresetsPage>().Single());

            AppHost.Get<UserDataStore>().SaveLibrary();
            vm.Navigate("home");
            vm.Navigate("presets");
            Pump();
            Assert.NotSame(presetsModel, vm.CurrentPage);
        }
        finally
        {
            window.Close();
            vm.Dispose();
        }
    }

    private static void Pump()
    {
        for (var i = 0; i < 5; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
