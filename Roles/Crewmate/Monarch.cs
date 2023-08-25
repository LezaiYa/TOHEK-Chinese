﻿using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TOHE.Options;
using static TOHE.Translator;

namespace TOHE.Roles.Crewmate;

public static class Monarch
{
    private static readonly int Id = 7055522;
    private static List<byte> playerIdList = new();

    public static OptionItem KnightCooldown;
    public static OptionItem KnightMax;
    public static OptionItem KnightAdditionalVote;
    public static OptionItem KnightHideVote;
    

    private static int KnightLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Monarch);
        KnightCooldown = FloatOptionItem.Create(Id + 10, "MonarchKnightCooldown", new(0f, 990f, 2.5f), 10f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Seconds);
        KnightMax = IntegerOptionItem.Create(Id + 12, "MonarchKnightMax", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
            .SetValueFormat(OptionFormat.Times);
        KnightAdditionalVote = IntegerOptionItem.Create(Id + 13, "MonarchKnightAdditionalVote", new(1, 99, 1), 3, TabGroup.CrewmateRoles, false)
         .SetParent(CustomRoleSpawnChances[CustomRoles.Monarch])
             .SetValueFormat(OptionFormat.Votes);
        KnightHideVote = BooleanOptionItem.Create(Id + 14, "MonarchKnightHideVote", false, TabGroup.CrewmateRoles, false)
        .SetParent(CustomRoleSpawnChances[CustomRoles.Monarch]);
    }
    public static void Init()
    {
        playerIdList = new();
        KnightLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        KnightLimit = KnightMax.GetInt();

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    private static void SendRPC()
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetMonarchKnightLimit, SendOption.Reliable, -1);
        writer.Write(KnightLimit);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        KnightLimit = reader.ReadInt32();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KnightCooldown.GetFloat();
    public static bool CanUseKillButton(PlayerControl player) => !player.Data.IsDead && KnightLimit >= 1;
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (KnightLimit < 1) return false;
        if (CanBeKnighted(target))
        {
            KnightLimit--;
            target.RpcSetCustomRole(CustomRoles.Knighted);

            killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchKnightedPlayer")));
            target.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("KnightedByMonarch")));
            Utils.NotifyRoles();

            killer.ResetKillCooldown();
            killer.SetKillCooldown();
      //      killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(killer);
            target.RpcGuardAndKill(target);

            Logger.Info("设置职业:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Knighted.ToString(), "Assign " + CustomRoles.Knighted.ToString());
            if (KnightLimit < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
            return true;
        }
        
        if (KnightLimit < 0)
            HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        killer.Notify(Utils.ColorString(Utils.GetRoleColor(CustomRoles.Monarch), GetString("MonarchInvalidTarget")));
        Logger.Info($"{killer.GetNameWithRole()} : 剩余{KnightLimit}次招募机会", "Monarch");
        return false;
    }
    public static string GetKnightLimit() => Utils.ColorString(KnightLimit >= 1 ? Utils.GetRoleColor(CustomRoles.Monarch) : Color.gray, $"({KnightLimit})");
    public static bool CanBeKnighted(this PlayerControl pc)
    {
        return pc != null && (!pc.GetCustomRole().IsNotKnightable() && !pc.Is(CustomRoles.Knighted) && !pc.Is(CustomRoles.TicketsStealer))
        && !(
            false
            );
    }
}