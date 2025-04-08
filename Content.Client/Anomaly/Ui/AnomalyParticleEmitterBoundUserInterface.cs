namespace Content.Client.Anomaly.Ui;

public class AnomalyParticleEmitterBoundUserInterface : BoundUserInterface
{
    /// <inheritdoc />
    public AnomalyParticleEmitterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.HasComponent<DoorRemoteComponent>(Owner))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        var models = CreateButtons();
        _menu.SetButtons(models);

        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOption> CreateButtons()
    {
        return new[]
        {
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.OpenClose)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Doors/Airlocks/Standard/basic.rsi"), "assembly"),
                ToolTip = "open/close"
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleBolts)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "bolt_door"),
                ToolTip = "bolt"
            },
            new RadialMenuActionOption<OperatingMode>(HandleRadialMenuClick, OperatingMode.ToggleEmergencyAccess)
            {
                Sprite = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Actions/actions_ai.rsi"), "emergency_on"),
                ToolTip = "emergency access"
            },
        };
    }

    private void HandleRadialMenuClick(OperatingMode mode)
    {
        SendPredictedMessage(new DoorRemoteModeChangeMessage { Mode = mode });
    }
}
