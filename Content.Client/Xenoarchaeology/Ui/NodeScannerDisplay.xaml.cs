using Content.Client.UserInterface.Controls;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Xenoarchaeology.Ui;

[GenerateTypedNameReferences]
public sealed partial class NodeScannerDisplay : FancyWindow
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public NodeScannerDisplay(EntityUid scannerEntityUid)
    {
        RobustXamlLoader.Load(this);

        IoCManager.InjectDependencies(this);

        var scannerComponent = _ent.GetComponent<NodeScannerComponent>(scannerEntityUid);

        Update((scannerEntityUid, scannerComponent));
    }

    public void Update(Entity<NodeScannerComponent> ent)
    {
        var triggeredNodesSnapshot = ent.Comp.TriggeredNodesSnapshot;
        if (triggeredNodesSnapshot.Count == 0)
        {
            NoActiveNodeDataLabel.Visible = true;
            ActiveNodesList.Visible = false;
            ActiveNodesList.Children.Clear();
        }
        else
        {
            NoActiveNodeDataLabel.Visible = false;
            ActiveNodesList.Visible = true;

            ActiveNodesList.Children.Clear();

            foreach (var nodeId in triggeredNodesSnapshot)
            {
                var nodeLabel = new Button
                {
                    Text = nodeId,
                    Margin = new Thickness(15, 10, 0, 0),
                    MaxHeight = 40,
                    Disabled = true
                };
                ActiveNodesList.Children.Add(nodeLabel);
            }
        }
    }
}