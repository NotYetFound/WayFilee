using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Filee.App.Controls;
using Filee.App.Services;
using Filee.App.ViewModels;
using Filee.App.ViewModels.Pages;
using Filee.App.Views;
using Filee.Core.Localization;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public class DonutAnimationSpeedTests
{
    [AvaloniaFact]
    public void Toolbar_speed_setting_reaches_the_live_donut_and_survives_reload()
    {
        TestServices.EnsureInitialized("en");
        var store = AppHost.Get<UserDataStore>();
        var original = store.Settings.Donut.AnimationSpeed;
        var loc = AppHost.Get<ILocalizer>();
        try
        {
            var vm = new MainWindowViewModel(AppHost.Services, loc, AppHost.Get<UpdateService>());
            vm.Navigate("toolbar");
            var window = new MainWindow { DataContext = vm, Width = 1080, Height = 820 };
            window.Show();
            RenderTests.Pump();

            var toolbar = Assert.IsType<ToolbarPageViewModel>(vm.CurrentPage);
            var speed = window.GetVisualDescendants().OfType<ComboBox>()
                .Single(c => c.Items.Count == 4 && c.Items.OfType<ComboBoxItem>()
                    .Any(i => Equals(i.Content, loc["toolbar.animation_speed.instant"])));
            speed.SelectedIndex = (int)DonutAnimationSpeed.Fast;
            RenderTests.Pump();
            Assert.Equal(DonutAnimationSpeed.Fast, store.Settings.Donut.AnimationSpeed);
            Assert.Equal((int)DonutAnimationSpeed.Fast, toolbar.AnimationSpeedIndex);
            window.Close();

            store.Load();
            Assert.Equal(DonutAnimationSpeed.Fast, store.Settings.Donut.AnimationSpeed);
            var radial = AppHost.Get<RadialViewModel>();
            radial.Load(["/tmp/example.jpg"], clickMode: true);
            Assert.Equal(DonutAnimationSpeed.Fast, radial.AnimationSpeed);
            var donut = new DonutMenu { AnimationSpeed = radial.AnimationSpeed };
            Assert.Equal(DonutAnimationSpeed.Fast, donut.AnimationSpeed);
        }
        finally
        {
            store.Settings.Donut.AnimationSpeed = original;
            store.SaveSettings();
        }
    }
}
