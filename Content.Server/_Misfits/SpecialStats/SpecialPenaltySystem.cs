using Content.Server._Misfits.SpecialStats.Components;
using Content.Server._N14.Special.Speech.Components;
using Content.Shared._Misfits.Special;
using Content.Shared._Misfits.Special.Components;
using Content.Shared._Misfits.SpecialStats;
using Content.Shared.Clumsy;

namespace Content.Server._Misfits.SpecialStats;

/// <summary>
/// Applies negative SPECIAL side effects that are represented by components.
/// </summary>
public sealed class SpecialPenaltySystem : EntitySystem
{
    [Dependency] private readonly SharedSpecialSystem _special = default!;

    private const int LowCharismaThreshold = 5;
    private const int LowIntelligenceThreshold = 2;
    private const float ClumsyLuckOneChance = 0.50f;
    private const float ClumsyLuckTwoChance = 0.05f;
    private const float ClumsyLuckThreeChance = 0.03f;
    private const float ClumsyLuckFourChance = 0.01f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecialChangedEvent>(OnSpecialChanged);
        SubscribeLocalEvent<SpecialStatsReadyEvent>(OnStatsReady);
        SubscribeLocalEvent<SpecialShutdownEvent>(OnSpecialShutdown);
    }

    private void OnSpecialChanged(ref SpecialChangedEvent args)
    {
        if (TryComp<SpecialComponent>(args.ChangedEntity, out var special))
            ApplyPenalties((args.ChangedEntity, special));
    }

    private void OnStatsReady(ref SpecialStatsReadyEvent args)
    {
        if (TryComp<SpecialComponent>(args.Entity, out var special))
            ApplyPenalties((args.Entity, special));
    }

    private void OnSpecialShutdown(ref SpecialShutdownEvent args)
    {
        ClearLowCharisma(args.Entity);
        ClearLowIntelligence(args.Entity);
        ClearLuckClumsy(args.Entity);
    }

    private void ApplyPenalties(Entity<SpecialComponent> ent)
    {
        var charisma = _special.GetEffective(ent.Owner, SpecialStat.Charisma, ent.Comp);
        if (charisma < LowCharismaThreshold)
            ApplyLowCharisma(ent.Owner, charisma);
        else
            ClearLowCharisma(ent.Owner);

        if (_special.GetEffective(ent.Owner, SpecialStat.Intelligence, ent.Comp) < LowIntelligenceThreshold)
            ApplyLowIntelligence(ent.Owner);
        else
            ClearLowIntelligence(ent.Owner);

        var luck = _special.GetEffective(ent.Owner, SpecialStat.Luck, ent.Comp);
        if (TryGetLuckClumsyChance(luck, out var clumsyChance))
            ApplyLuckClumsy(ent.Owner, clumsyChance);
        else
            ClearLuckClumsy(ent.Owner);
    }

    private void ApplyLowCharisma(EntityUid uid, int charisma)
    {
        var comp = EnsureComp<SpecialLowCharismaComponent>(uid);
        comp.Charisma = charisma;
    }

    private void ClearLowCharisma(EntityUid uid)
    {
        RemComp<SpecialLowCharismaComponent>(uid);
    }

    private void ApplyLowIntelligence(EntityUid uid)
    {
        EnsureComp<LowIntelligenceAccentComponent>(uid);
    }

    private void ClearLowIntelligence(EntityUid uid)
    {
        RemComp<LowIntelligenceAccentComponent>(uid);
    }

    private static bool TryGetLuckClumsyChance(int luck, out float chance)
    {
        chance = luck switch
        {
            1 => ClumsyLuckOneChance,
            2 => ClumsyLuckTwoChance,
            3 => ClumsyLuckThreeChance,
            4 => ClumsyLuckFourChance,
            _ => 0f,
        };

        return chance > 0f;
    }

    private void ApplyLuckClumsy(EntityUid uid, float chance)
    {
        var hadClumsy = HasComp<ClumsyComponent>(uid);
        var specialApplied = HasComp<SpecialAppliedClumsyComponent>(uid);

        // Do not weaken or take ownership of clumsy from traits, species, or admin effects.
        if (hadClumsy && !specialApplied)
            return;

        var clumsy = EnsureComp<ClumsyComponent>(uid);
        clumsy.ClumsyDefaultCheck = chance;
        Dirty(uid, clumsy);
        EnsureComp<SpecialAppliedClumsyComponent>(uid);
    }

    private void ClearLuckClumsy(EntityUid uid)
    {
        if (!HasComp<SpecialAppliedClumsyComponent>(uid))
            return;

        RemComp<SpecialAppliedClumsyComponent>(uid);
        RemComp<ClumsyComponent>(uid);
    }
}
