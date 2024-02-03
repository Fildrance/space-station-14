using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="CentrifugeWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class CentrifugeBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CentrifugeWindow? _window;

        public CentrifugeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a centrifuge UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = new CentrifugeWindow
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.OpenCentered();
            _window.OnClose += Close;
            _window.StartButton.OnPressed +=
                _ => SendMessage(new CentrifugeStartUnmixMessage());

            // Setup static button actions.
            _window.InputEjectButton.OnPressed +=
                _ => SendMessage(new ItemSlotButtonPressedEvent("mixer"));

            _window.OnReagentButtonPressed += (_, button) => SendMessage(new CentrifugeReagentButtonMessage(button.Id, button.IsTransfer));
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedSolutionContainerMixerSystem"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (CentrifugeBoundUserInterfaceState) state;

            _window?.UpdateState(castState); // Update window state
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
