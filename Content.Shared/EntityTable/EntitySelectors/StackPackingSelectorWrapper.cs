using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

public sealed partial class StackPackingSelectorWrapper : EntityTableSelector
{
    [DataField(required: true)]
    public EntityTableSelector Inner = default!;

    /// <inheritdoc />
    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(
        System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx
    )
    {
        var fromInner = Inner.GetSpawns(rand, entMan, proto, ctx);

        Dictionary<EntProtoId, int> spawns = new();
        foreach (var entProtoId in fromInner)
        {
            if (!spawns.TryAdd(entProtoId, 1))
            {
                spawns[entProtoId]++;
            }
        }

        foreach (var (entProtoId, count)  in spawns)
        {
            if (count > 1)
            {
                var entityPrototype = proto.Index(entProtoId);
                if(entityPrototype.Components.TryGetComponent("", out var stack) && stack is StackComponent stackComp)
                {
                    
                }
            }
        }

    }
}
