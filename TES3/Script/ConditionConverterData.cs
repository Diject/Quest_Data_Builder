﻿using Quest_Data_Builder.TES3.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Script
{
    internal struct ConditionDataSruct
    {
        public string RequirementType;
        public string? TypeForPlayer;
        public string? TypeForActor;
        public uint? Attribute;
        public uint? Skill;
    }

    internal static partial class ConditionConverter
    {
        private readonly static Dictionary<string, ConditionDataSruct> conditionData = new(StringComparer.OrdinalIgnoreCase)
        {
            { "GameHour", new ConditionDataSruct(){ RequirementType = RequirementType.CustomGameHour } },

            { "GetBlock", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 0 } },
            { "GetArmorer", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 1 } },
            { "GetMediumArmor", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 2 } },
            { "GetHeavyArmor", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 3 } },
            { "GetBluntWeapon", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 4 } },
            { "GetLongBlade", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 5 } },
            { "GetAxe", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 6 } },
            { "GetSpear", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 7 } },
            { "GetAthletics", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 8 } },
            { "GetEnchant", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 9 } },
            { "GetDestruction", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 10 } },
            { "GetAlteration", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 11 } },
            { "GetIllusion", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 12 } },
            { "GetConjuration", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 13 } },
            { "GetMysticism", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 14 } },
            { "GetRestoration", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 15 } },
            { "GetAlchemy", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 16 } },
            { "GetUnarmored", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 17 } },
            { "GetSecurity", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 18 } },
            { "GetSneak", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 19 } },
            { "GetAcrobatics", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 20 } },
            { "GetLightArmor", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 21 } },
            { "GetShortBlade", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 22 } },
            { "GetMarksman", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 23 } },
            { "GetMerchantile", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 24 } },
            { "GetSpeechcraft", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 25 } },
            { "GetHandToHand", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSkill, Skill = 26 } },

            { "GetStrength", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 0 } },
            { "GetIntelligence", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 1 } },
            { "GetWillpower", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 2 } },
            { "GetAgility", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 3 } },
            { "GetSpeed", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 4 } },
            { "GetEndurance", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 5 } },
            { "GetPersonality", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 6 } },
            { "GetLuck", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttribute, Attribute = 7 } },

            { "GetCastPenalty", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCastPenalty } },
            { "GetLevel", new ConditionDataSruct(){ RequirementType = RequirementType.Custom, TypeForPlayer = RequirementType.PlayerLevel, TypeForActor = RequirementType.NPCLevel } },
            { "GetAIPackageDone", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAIPackageDone } },
            { "GetAngle", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAngle } },
            { "GetArmorType", new ConditionDataSruct(){ RequirementType = RequirementType.CustomArmorType } },
            { "GetAttacked", new ConditionDataSruct(){ RequirementType = RequirementType.NPCAttacked } },
            { "GetBlightDisease", new ConditionDataSruct(){ RequirementType = RequirementType.CustomBlightDisease, TypeForPlayer = RequirementType.PlayerBlightDisease } },
            { "GetButtonPressed", new ConditionDataSruct(){ RequirementType = RequirementType.CustomButtonPressed } },
            { "GetCollidingActor", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCollidingActor } },
            { "GetCollidingPC", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCollidingPC } },
            { "GetCommonDisease", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCommonDisease, TypeForPlayer = RequirementType.PlayerCommonDisease } },
            { "GetCurrentAIPackage", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCurrentAIPackage } },
            { "GetCurrentTime", new ConditionDataSruct(){ RequirementType = RequirementType.CustomGameHour } },
            { "GetCurrentWeather", new ConditionDataSruct(){ RequirementType = RequirementType.Weather } },
            { "GetDeadCount", new ConditionDataSruct(){ RequirementType = RequirementType.Dead } },
            { "GetDetected", new ConditionDataSruct(){ RequirementType = RequirementType.PlayerIsDetected } },
            { "GetDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomDisabled } },
            { "GetDistance", new ConditionDataSruct(){ RequirementType = RequirementType.CustomDistance } },
            { "GetEffect", new ConditionDataSruct(){ RequirementType = RequirementType.CustomEffect } },
            { "GetForceJump", new ConditionDataSruct(){ RequirementType = RequirementType.CustomForceJump } },
            { "GetForceMoveJump", new ConditionDataSruct(){ RequirementType = RequirementType.CustomForceMoveJump } },
            { "GetForceRun", new ConditionDataSruct(){ RequirementType = RequirementType.CustomForceRun } },
            { "GetForceSneak", new ConditionDataSruct(){ RequirementType = RequirementType.CustomForceSneak } },
            { "GetHealthGetRatio", new ConditionDataSruct(){ RequirementType = RequirementType.NPCHealthPercent, TypeForActor = RequirementType.NPCHealthPercent, TypeForPlayer = RequirementType.PlayerHealthPercent } },
            { "GetInterior", new ConditionDataSruct(){ RequirementType = RequirementType.CustomInterior } },
            { "GetItemCount", new ConditionDataSruct(){ RequirementType = RequirementType.Item } },
            { "GetJournalIndex", new ConditionDataSruct(){ RequirementType = RequirementType.Journal } },
            { "GetLineOfSight", new ConditionDataSruct(){ RequirementType = RequirementType.CustomLineOfSight } },
            { "GetLocked", new ConditionDataSruct(){ RequirementType = RequirementType.CustomLocked } },
            { "GetLOS", new ConditionDataSruct(){ RequirementType = RequirementType.CustomLineOfSight } },
            { "GetMasserPhase", new ConditionDataSruct(){ RequirementType = RequirementType.CustomMasserPhase } },
            { "GetPCCell", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCCell } },
            { "GetPCCrimeLevel", new ConditionDataSruct(){ RequirementType = RequirementType.PlayerCrimeLevel } },
            { "GetPCFacRep", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCFacRep } },
            { "GetPCInJail", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCInJail } },
            { "GetPCJumping", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCJumping } },
            { "GetPCRank", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCRank } },
            { "GetPCRunning", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCRunning } },
            { "GetPCSleep", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCSleep } },
            { "GetPCSneaking", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCSneaking } },
            { "GetPCTraveling", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCTraveling } },
            { "GetPlayerControlsDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPlayerControlsDisabled } },
            { "GetPlayerFightingDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPlayerFightingDisabled } },
            { "GetPlayerJumpingDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPlayerJumpingDisabled } },
            { "GetPlayerLookingDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPlayerLookingDisabled } },
            { "GetPlayerMagicDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPlayerMagicDisabled } },
            { "GetPos", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPos } },
            { "GetRace", new ConditionDataSruct(){ RequirementType = RequirementType.CustomRace } },
            { "GetScale", new ConditionDataSruct(){ RequirementType = RequirementType.CustomScale } },
            { "GetSecundaPhase", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSecundaPhase } },
            { "GetSoundPlaying", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSoundPlaying } },
            { "GetSpell", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSpell } },
            { "GetSpellEffects", new ConditionDataSruct(){ RequirementType = RequirementType.CustomEffect } },
            { "GetSpellReadied", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSpellReadied } },
            { "GetSquareRoot", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSquare } },
            { "GetStandingActor", new ConditionDataSruct(){ RequirementType = RequirementType.CustomStandingActor } },
            { "GetStandingPC", new ConditionDataSruct(){ RequirementType = RequirementType.CustomStandingPC } },
            { "GetTarget", new ConditionDataSruct(){ RequirementType = RequirementType.CustomTarget } },
            { "GetVanityModeDisabled", new ConditionDataSruct(){ RequirementType = RequirementType.CustomVanityModeDisabled } },
            { "GetWaterLevel", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWaterLevel } },
            { "GetWeaponDrawn", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWeaponDrawn } },
            { "GetWeaponType", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWeaponType } },
            { "GetWerewolfKills", new ConditionDataSruct(){ RequirementType = RequirementType.PlayerWerewolfKills } },
            { "GetWindSpeed", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWindSpeed } },
            { "HasItemEquipped", new ConditionDataSruct(){ RequirementType = RequirementType.CustomHasItemEquipped } },
            { "HasSoulgem", new ConditionDataSruct(){ RequirementType = RequirementType.CustomHasSoulgem } },
            { "CellChanged", new ConditionDataSruct(){ RequirementType = RequirementType.CustomCellChanged } },
            { "HitAttemptOnMe", new ConditionDataSruct(){ RequirementType = RequirementType.CustomHitOnMe } },
            { "HitOnMe", new ConditionDataSruct(){ RequirementType = RequirementType.CustomHitOnMe } },
            { "UsedOnMe", new ConditionDataSruct(){ RequirementType = RequirementType.CustomUsedOnMe } },

            { "OnActivate", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnActivate } },
            { "OnDeath", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnDeath } },
            { "OnKnockout", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnKnockout } },
            { "OnMurder", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnMurder } },
            { "OnPCAdd", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCAdd } },
            { "OnPCDrop", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCDrop } },
            { "OnPCEquip", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCEquip } },
            { "OnPCHitMe", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCHitMe } },
            { "OnPCRepair", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCRepair } },
            { "OnPCSoulGemUse", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnPCSoulGemUse } },
            { "OnRepair", new ConditionDataSruct(){ RequirementType = RequirementType.CustomOnRepair } },

            { "ScriptRunning", new ConditionDataSruct(){ RequirementType = RequirementType.CustomScriptRunning } },
            { "SayDone", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSayDone } },
            { "GetHealth", new ConditionDataSruct(){ RequirementType = RequirementType.CustomHealth } },
            { "MenuMode", new ConditionDataSruct(){ RequirementType = RequirementType.CustomMenuMode } },
            { "GetFlee", new ConditionDataSruct(){ RequirementType = RequirementType.NPCFlee } },
            { "PCKnownWerewolf", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCKnownWerewolf } },
            { "PCWerewolf", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCWerewolf } },
            { "GetDisposition", new ConditionDataSruct(){ RequirementType = RequirementType.CustomDisposition } },
            { "Day", new ConditionDataSruct(){ RequirementType = RequirementType.CustomDay } },
            { "Month", new ConditionDataSruct(){ RequirementType = RequirementType.CustomMonth } },
            { "Year", new ConditionDataSruct(){ RequirementType = RequirementType.CustomYear } },
            { "PCRace", new ConditionDataSruct(){ RequirementType = RequirementType.CustomPCRace } },
            { "PCVampire", new ConditionDataSruct(){ RequirementType = RequirementType.PlayerIsVampire } },
            { "VampClan", new ConditionDataSruct(){ RequirementType = RequirementType.CustomVampClan } },
            { "random100", new ConditionDataSruct(){ RequirementType = RequirementType.CustomRandom } },

            { "GetFight", new ConditionDataSruct(){ RequirementType = RequirementType.NPCFight } },
            { "IsWerewolf", new ConditionDataSruct(){ RequirementType = RequirementType.NPCIsWerewolf } },
            { "GetAlarm", new ConditionDataSruct(){ RequirementType = RequirementType.NPCAlarm } },
            { "GetResistMagicka", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistMagicka } },
            { "GetResistFire", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistFire } },
            { "GetResistFrost", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistFrost } },
            { "GetResistShock", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistShock } },
            { "GetResistDisease", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistDisease } },
            { "GetResistBlight", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistBlight } },
            { "GetResistCorprus", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistCorprus } },
            { "GetResistPoison", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistPoison } },
            { "GetResistParalysis", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistParalysis } },
            { "GetResistNormalWeapons", new ConditionDataSruct(){ RequirementType = RequirementType.CustomResistNormalWeapons } },
            { "GetWaterBreathing", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWaterBreathing } },
            { "GetChameleon", new ConditionDataSruct(){ RequirementType = RequirementType.CustomChameleon } },
            { "GetWaterWalking", new ConditionDataSruct(){ RequirementType = RequirementType.CustomWaterWalking } },
            { "GetSwimSpeed", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSwimSpeed } },
            { "GetSuperJump", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSuperJump } },
            { "GetFlying", new ConditionDataSruct(){ RequirementType = RequirementType.CustomFlying } },
            { "GetArmorBonus", new ConditionDataSruct(){ RequirementType = RequirementType.CustomArmorBonus } },
            { "GetSilence", new ConditionDataSruct(){ RequirementType = RequirementType.CustomSilence } },
            { "GetBlindness", new ConditionDataSruct(){ RequirementType = RequirementType.CustomBlindness } },
            { "GetParalysis", new ConditionDataSruct(){ RequirementType = RequirementType.CustomParalysis } },
            { "GetInvisible", new ConditionDataSruct(){ RequirementType = RequirementType.CustomInvisible } },
            { "GetInvisibile", new ConditionDataSruct(){ RequirementType = RequirementType.CustomInvisible } },
            { "GetAttackBonus", new ConditionDataSruct(){ RequirementType = RequirementType.CustomAttackBonus } },
            { "GetDefendBonus", new ConditionDataSruct(){ RequirementType = RequirementType.CustomDefendBonus } },
        };
    }
}
