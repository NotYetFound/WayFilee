using Filee.App.Services.Triggers;
using Filee.Core.Settings;

namespace Filee.App.Tests;

public sealed class WaylandInputHookTests
{
    [Theory]
    [InlineData(0x110u, TriggerMouseButton.Left)]
    [InlineData(0x111u, TriggerMouseButton.Right)]
    [InlineData(0x112u, TriggerMouseButton.Middle)]
    [InlineData(0x113u, TriggerMouseButton.X1)]
    [InlineData(0x114u, TriggerMouseButton.X2)]
    public void Linux_button_codes_map_to_gesture_buttons(uint code, TriggerMouseButton expected) =>
        Assert.Equal(expected, WaylandInputHook.MouseButton(code));

    [Fact]
    public void Modifier_state_tracks_press_and_release()
    {
        var state = WaylandInputHook.UpdateModifiers(ModifierKeys.None, 29, true);
        state = WaylandInputHook.UpdateModifiers(state, 42, true);
        Assert.Equal(ModifierKeys.Ctrl | ModifierKeys.Shift, state);

        state = WaylandInputHook.UpdateModifiers(state, 29, false);
        Assert.Equal(ModifierKeys.Shift, state);
    }

    [Fact]
    public void Tab_is_available_for_donut_flipping() =>
        Assert.Equal("Tab", WaylandInputHook.KeyName(15));
}
