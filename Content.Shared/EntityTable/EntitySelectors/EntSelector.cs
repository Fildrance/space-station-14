using Content.Shared.EntityTable.ValueSelector;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawn for the entity prototype specified at whatever count specified.
/// </summary>
public sealed partial class EntSelector : EntityTableSelector
{
    public const string IdDataFieldTag = "id";

    private const string AmountOverride = "AmountOverride";

    [DataField(IdDataFieldTag, required: true)]
    public EntProtoId Id;

    [DataField]
    public NumberSelector Amount = new ConstantNumberSelector(1);

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var amountSelector = Amount;
        if (ctx.TryGetData<int>(AmountOverride, out var amountOverride))
        {
            amountSelector = new ConstantNumberSelector(amountOverride);
        }

        var num = amountSelector.Get(rand);
        for (var i = 0; i < num; i++)
        {
            yield return Id;
        }
    }
}
