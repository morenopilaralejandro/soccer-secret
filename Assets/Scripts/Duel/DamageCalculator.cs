using System;
using System.Collections.Generic;
using UnityEngine;

public static class DamageCalculator
{
    // Multiplier constants
    private const float MAIN_MULTIPLIER   = 1f;
    private const float SUB_MULTIPLIER    = 0.5f;
    private const float SECRET_MULTIPLIER = 5.0f;
    private const float MATCH_MULTIPLIER  = 1.5f;
    private const float DISTANCE_MULTIPLIER  = 10f;

    // Helper function for Phys/Skill formulas
    private static float CalcFormula(Player player, PlayerStats main, PlayerStats sub0, PlayerStats sub1)
    {
        return
            player.GetStat(main) +
            player.GetStat(sub0) * MAIN_MULTIPLIER +
            player.GetStat(sub1) * SUB_MULTIPLIER +
            player.GetStat(PlayerStats.Courage);
    }

    // Helper function for Secret formulas
    private static float CalcSecret(Player player, Secret secret, PlayerStats main)
    {
        if (secret == null) return 0f;
        float baseDamage =
            secret.Power * SECRET_MULTIPLIER +
            player.GetStat(main) * MAIN_MULTIPLIER +
            player.GetStat(PlayerStats.Courage);
        if (player.Element == secret.Element)
            baseDamage *= MATCH_MULTIPLIER;
        return baseDamage;
    }

    // Special for Shoot (minus distance)
    private static float CalcDistanceReduction(Player player)
    {
        return GameManager.Instance.GetDistanceToOppGoal(player) * DISTANCE_MULTIPLIER;
    }

    public static Dictionary<(Category, DuelCommand), Func<Player, Secret, float>> damageFormulas =
        new Dictionary<(Category, DuelCommand), Func<Player, Secret, float>>()
    {
        // Dribble
        {(Category.Dribble, DuelCommand.Phys),  (p, s) => CalcFormula(p, PlayerStats.Control, PlayerStats.Body, PlayerStats.Stamina)},
        {(Category.Dribble, DuelCommand.Skill), (p, s) => CalcFormula(p, PlayerStats.Control, PlayerStats.Kick, PlayerStats.Speed)},
        {(Category.Dribble, DuelCommand.Secret), (p, s) => CalcSecret(p, s, PlayerStats.Control)},

        // Block
        {(Category.Block, DuelCommand.Phys),    (p, s) => CalcFormula(p, PlayerStats.Body, PlayerStats.Guard, PlayerStats.Stamina)},
        {(Category.Block, DuelCommand.Skill),   (p, s) => CalcFormula(p, PlayerStats.Body, PlayerStats.Control, PlayerStats.Speed)},
        {(Category.Block, DuelCommand.Secret),  (p, s) => CalcSecret(p, s, PlayerStats.Body)},

        // Shoot
        {(Category.Shoot, DuelCommand.Phys),    (p, s) => CalcFormula(p, PlayerStats.Kick, PlayerStats.Body, PlayerStats.Stamina)},
        {(Category.Shoot, DuelCommand.Skill),   (p, s) => CalcFormula(p, PlayerStats.Kick, PlayerStats.Control, PlayerStats.Speed)},
        {(Category.Shoot, DuelCommand.Secret),  (p, s) => CalcSecret(p, s, PlayerStats.Kick)},

        // Catch
        {(Category.Catch, DuelCommand.Phys),    (p, s) => CalcFormula(p, PlayerStats.Guard, PlayerStats.Body, PlayerStats.Stamina)},
        {(Category.Catch, DuelCommand.Skill),   (p, s) => CalcFormula(p, PlayerStats.Guard, PlayerStats.Control, PlayerStats.Speed)},
        {(Category.Catch, DuelCommand.Secret),  (p, s) => CalcSecret(p, s, PlayerStats.Guard)}
    };

    public static float GetDamage(Category cat, DuelCommand cmd, Player p, Secret s)
    {
        float damage = 0f;
        if (damageFormulas.TryGetValue((cat, cmd), out var formula))
            damage = formula(p, s);
        if (cat == Category.Shoot)
            damage -= CalcDistanceReduction(p);
        return damage;
    }
}
