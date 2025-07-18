using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace jshepler.ngu.mods
{
    [HarmonyPatch]
    internal class AutoBattle
    {
        private static Character Character => Plugin.Character;
        private static Adventure Adventure => Plugin.Character.adventure;
        private static AdventureController Controller => Plugin.Character.adventureController;
        private static PlayerController PlayerController => Controller.playerController;

        //private static readonly StreamWriter sw = new("C:\\Users\\Austin Wiley\\source\\repos\\logs.txt");

        private static Dictionary<int, Func<bool>> Attacks { get; } = new() {
                    { 3, RegularAttack },
                    { 4, StrongAttack },
                    { 5, PierceAttack },
                    { 6, UltimateAttack },
                };

        [HarmonyPrepare]
        private static void Prep(MethodBase original)
        {
            //sw.AutoFlush = true;
            if (original != null)
                return;

            Plugin.OnUpdate += Update;
        }

        private static void Update(object sender, EventArgs e)
        {
            if (Adventure.autoattacking)
                return;

            if (!Controller.fightInProgress)
                return;

            if (PlayerController.moveTimer > 0f)
                return;

            bool isWalderp = Controller.zone == 16;
            //bool isWalderp = Controller.currentEnemy.spriteID == 306;

            if (isWalderp) {
                WalderpAttackMove();
            }
            else {
                ManualAttackMove();
            }
        }

        private static bool ApplyBuffs()
        {
            if (Heal())
                return true;
            if (HyperRegen())
                return true;

            if (UltimateBuff())
                return true;
            if (OffenseBuff())
                return true;
            if (DefenseBuff())
                return true;

            return false;
        }

        private static void ManualAttackMove()
        {
            ///Do buffs?
            if (ApplyBuffs()) {
                return;
            }

            if (Paralyze())
                return;

            ///Do Attacks
            foreach (var item in Attacks.Values.Reverse()) {
                if (item.Invoke())
                    return;
            }
        }

        private static void WalderpAttackMove()
        {
            if (Controller.enemyAI.inWaldoSaysLoop) {
                int waldoAttack = Controller.enemyAI.waldoAttackID;
                bool waldoSays = Controller.enemyAI.waldoSays;

                if (waldoSays) {
                    Attacks[waldoAttack].Invoke();
                }
                else if (!waldoSays && waldoAttack == 3) {
                    foreach (int i in Enumerable.Range(4, 3)) {
                        if (Attacks[i].Invoke()) {
                            return;
                        }
                    }
                }
                else {
                    RegularAttack();
                }
            }
            else {
                if (Paralyze())
                    return;
                if (ApplyBuffs()) return;
                RegularAttack();
            }
        }

        private static bool RegularAttack()
        {
            var attack = Controller.regularAttackMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool StrongAttack()
        {
            var attack = Controller.strongAttackMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool PierceAttack()
        {
            var attack = Controller.pierceMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool UltimateAttack()
        {
            var attack = Controller.ultimateAttackMove;
            if (!attack.button.interactable)
                return false;

            if (!PlayerController.isCharged && UltimateBuffActive()) {
                if (Charge())
                    return true;
            }
            attack.doMove();
            return true;
        }

        private static bool OffenseBuff()
        {
            if (UltimateBuffActive() || OffenseBuffActive())
                return false;
            var attack = Controller.offenseBuffMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool OffenseBuffActive()
        {
            return PlayerController.offenseBuffTime > 0 && PlayerController.offenseBuffTime < Character.offenseBuffDuration();
        }

        private static bool DefenseBuff()
        {
            if (UltimateBuffActive() || DefenseBuffActive())
                return false;
            var attack = Controller.defenseBuffMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool DefenseBuffActive()
        {
            return PlayerController.defenseBuffTime > 0 && PlayerController.defenseBuffTime < Character.defenseBuffDuration();
        }

        private static bool UltimateBuff()
        {
            var attack = Controller.ultimateBuffMove;
            if (UltimateBuffActive())
                return false;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool UltimateBuffActive()
        {
            return PlayerController.ultimateBuffTime > 0 && PlayerController.ultimateBuffTime < Character.ultimateBuffDuration();
        }

        private static bool Charge()
        {
            var attack = Controller.chargeMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool Heal()
        {
            if (Adventure.curHP / Character.totalAdvHP() >= 0.8f)
                return false;
            var attack = Controller.healMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool Paralyze()
        {
            var attack = Controller.paralyzeMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }

        private static bool HyperRegen()
        {
            if (Adventure.curHP / Character.totalAdvHP() >= 0.5f)
                return false;
            var attack = Controller.hyperRegenMove;
            if (!attack.button.interactable)
                return false;
            attack.doMove();
            return true;
        }
    }
}