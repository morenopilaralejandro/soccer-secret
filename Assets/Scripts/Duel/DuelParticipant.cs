using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelParticipant
{
    public GameObject GameObj;
    public Player Player;
    public Category Category;
    public DuelAction Action;
    public DuelCommand Command;
    public Element CurrentElement;
    public Secret Secret;
    public bool IsDirect;
    public float Damage;

    public Dictionary<(Category, DuelCommand), Func<Player, Secret, float>> damageFormulas =
        new Dictionary<(Category, DuelCommand), Func<Player, Secret, float>>()
    {
        //Dribble
        {(Category.Dribble, DuelCommand.Phys), (player, secret) =>
            player.GetStat(PlayerStats.Control) +
            player.GetStat(PlayerStats.Body) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {(Category.Dribble, DuelCommand.Skill), (player, secret) =>
            player.GetStat(PlayerStats.Control) +
            player.GetStat(PlayerStats.Speed) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {
            (Category.Dribble, DuelCommand.Secret), (player, secret) => {
                if (secret == null) return 0f;
                float baseDamage =
                    secret.Power * 3.0f +
                    player.GetStat(PlayerStats.Control) * 0.5f +
                    player.GetStat(PlayerStats.Courage);
                if (player.Element == secret.Element)
                    baseDamage *= 1.5f;
                return baseDamage;
            }
        },
        //Block
        {(Category.Block, DuelCommand.Phys), (player, secret) =>
            player.GetStat(PlayerStats.Body) +
            player.GetStat(PlayerStats.Guard) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {(Category.Block, DuelCommand.Skill), (player, secret) =>
            player.GetStat(PlayerStats.Body) +
            player.GetStat(PlayerStats.Control) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {
            (Category.Block, DuelCommand.Secret), (player, secret) => {
                if (secret == null) return 0f;
                float baseDamage =
                    secret.Power * 3.0f +
                    player.GetStat(PlayerStats.Body) * 0.5f +
                    player.GetStat(PlayerStats.Courage);
                if (player.Element == secret.Element)
                    baseDamage *= 1.5f;
                return baseDamage;
            }
        },

        //Shoot
        {(Category.Shoot, DuelCommand.Phys), (player, secret) =>
            player.GetStat(PlayerStats.Kick) +
            player.GetStat(PlayerStats.Body) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {(Category.Shoot, DuelCommand.Skill), (player, secret) =>
            player.GetStat(PlayerStats.Kick) +
            player.GetStat(PlayerStats.Control) * 0.05f +
            player.GetStat(PlayerStats.Speed) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {
            (Category.Shoot, DuelCommand.Secret), (player, secret) => {
                if (secret == null) return 0f;
                float baseDamage =
                    secret.Power * 3.0f +
                    player.GetStat(PlayerStats.Kick) * 0.5f +
                    player.GetStat(PlayerStats.Courage);
                if (player.Element == secret.Element)
                    baseDamage *= 1.5f;
                baseDamage -= GameManager.Instance.GetDistanceToOppGoal(player) * 10f;
                return baseDamage;
            }
        },

        //Catch
        {(Category.Catch, DuelCommand.Phys), (player, secret) =>
            player.GetStat(PlayerStats.Guard) +
            player.GetStat(PlayerStats.Body) * 0.05f +
            player.GetStat(PlayerStats.Stamina) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {(Category.Catch, DuelCommand.Skill), (player, secret) =>
            player.GetStat(PlayerStats.Guard) +
            player.GetStat(PlayerStats.Control) * 0.05f +
            player.GetStat(PlayerStats.Speed) * 0.02f +
            player.GetStat(PlayerStats.Courage)
        },

        {
            (Category.Catch, DuelCommand.Secret), (player, secret) => {
                if (secret == null) return 0f;
                float baseDamage =
                    secret.Power * 3.0f +
                    player.GetStat(PlayerStats.Guard) * 0.5f +
                    player.GetStat(PlayerStats.Courage);
                if (player.Element == secret.Element)
                    baseDamage *= 1.5f;
                return baseDamage;
            }
        }
    };

    public DuelParticipant(
        GameObject gameObj,
        Category category,
        DuelAction action,
        DuelCommand command,
        Secret secret,
        bool isDirect)
    {
        GameObj = gameObj;
        Category = category;
        Action = action;
        Command = command;
        Secret = secret;
        IsDirect = isDirect;

        Player = gameObj.GetComponent<Player>();
        CurrentElement = Secret == null ? Player.Element : Secret.Element;

        // Calculate damage on construction
        if (damageFormulas.TryGetValue((Category, Command), out var formulaFunc))
        {
            Damage = formulaFunc(Player, Secret);
        }
        else
        {
            Debug.LogWarning("No formula for the combination");
            Damage = 0;
        }
    }
}
