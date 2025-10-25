using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore;

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

            //QualitySettings.vSyncCount = 0;
            //Application.targetFrameRate = 0;
            Plugin.OnUpdate += Update;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InventoryController), nameof(InventoryController.dumpAllIntoQuest))]
        private static bool patch_dumpAllIntoQuest(int itemID)
        {
            bool flag = false;
            for (int i = 0; i < Character.inventory.inventory.Count; i++) {
                if (Character.inventory.inventory[i].removable) {
                    if ((Character.adventure.itopod.perkLevel[66] <= 0) ? Character.beastQuestController.checkItemConsumed(Character.inventory.inventory[i].id) : Character.beastQuestController.checkItemConsumed(Character.inventory.inventory[i].id, Character.inventory.inventory[i].level)) {
                        Character.inventory.deleteItem(i);
                        flag = true;
                    }
                }
            }

            if (flag) {
                Character.inventoryController.updateInventory();
                Character.inventoryController.tooltip.showTooltip("BLOOP! All applicable Quest Items have been deposited!", 2f);
                //Character.inventoryController.tooltip.showTooltip("MWAAAA",2f);
            }
            return false;
        }

        private static void Update(object sender, EventArgs e)
        {
            Character.challenges.trollCounter = 1;
            //Character.inventory.weapon.level = 100;
            //Character.inventory.head.level = 100;
            //Character.inventory.chest.level = 100;
            //Character.inventory.legs.level = 100;
            //Character.inventory.boots.level = 100;

            ///Fruit hackers?
            //var fruit = Character.yggdrasil.fruits[7];
            //long fruitMaxTime = fruit.maxTier * 3600;
            //if (fruit.growing() && fruit.seconds < fruitMaxTime) {
            //    fruit.seconds = fruitMaxTime - 60;
            //}

            static void CheckEnergy()
            {
                var bt = Character.training.attackEnergy.Sum() + Character.training.defenseEnergy.Sum();
                var augs = Character.augments.augs.Sum(a => a.augEnergy + a.upgradeEnergy);
                var at = Character.advancedTraining.energy.Sum();
                var tm = Character.machine.speedEnergy;
                var wand = Character.wandoos98.wandoosEnergy;
                var ngus = Character.NGU.skills.Sum(n => n.energy);
                var wishes = Character.wishes.wishes.Sum(w => w.energy);

                var totalEnergy = bt + augs + at + tm + wand + ngus + wishes + Character.idleEnergy;

                if (Character.curEnergy != totalEnergy) {
                    Character.curEnergy = totalEnergy;
                }
            }
            static void CheckMagic()
            {
                var tm = Character.machine.goldMultiMagic;
                var bm = Character.bloodMagic.ritual.Sum(r => r.magic);
                var wand = Character.wandoos98.wandoosMagic;
                var wishes = Character.wishes.wishes.Sum(w => w.magic);
                var ngus = Character.NGU.magicSkills.Sum(n => n.magic);

                var totalMagic = tm + bm + wand + wishes + ngus + Character.magic.idleMagic;
                if (Character.magic.curMagic != totalMagic) {
                    Character.magic.curMagic = totalMagic;
                }
            }
            CheckEnergy();
            CheckMagic();

            if (Character.settings.nguLevelTrack == difficulty.evil) {
                if (!(Character.NGU.skills.Take(Character.NGU.NGUEnergySize()).Any(x => x.evilLevel < x.evilTargetLevel()) || Character.NGU.magicSkills.Take(Character.NGU.NGUMagicSize()).Any(x => x.evilLevel < x.evilTargetLevel()))) {
                    Character.NGUController.toNormalLevelTrack();
                }
            }

            bool disabled = true;
            if (disabled) {
                return;
            }
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