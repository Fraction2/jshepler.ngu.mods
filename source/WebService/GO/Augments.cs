using Mono.Cecil;
using SimpleJSON;
using System;
using System.Globalization;
using System.IO;

namespace jshepler.ngu.mods.WebService.GO
{
    internal class Augments
    {
        private static Action _uiUpdateAction;

        static Augments()
        {
            Plugin.OnUpdate += (o, e) =>
            {
                if (_uiUpdateAction != null)
                {
                    _uiUpdateAction();
                    _uiUpdateAction = null;
                }
            };
        }

        internal static void ApplyAugmentTargets(string json)
        {
            var augments = Plugin.Character.augments.augs;
            var targets = JSON.Parse(json).AsArray;

            for (var x = 0; x < 7; x++)
            {
                var item = (long)double.Parse(targets[x]["level"].ToString().Trim(['"']));
                augments[x].augmentTarget = item > 0 ? item : -1;
                item = (long)double.Parse(targets[x]["upgrade"].ToString().Trim(['"']));
                augments[x].upgradeTarget = item > 0 ? item : -1;
            }

            _uiUpdateAction = Plugin.Character.augmentsController.updateMenu;
        }

        internal static string BuildAugmentStats()
        {
            var root = new JSONObject();
            root.Add("augspeed", Plugin.Character.augmentsController.getTotalSpeedFactor());
            root.Add("ecap", Plugin.Character.totalCapEnergy());
            root.Add("gps", Plugin.Character.goldPerSecond());
            root.Add("lsc", Plugin.Character.challenges.laserSwordChallenge.curCompletions);
            root.Add("nac", Plugin.Character.challenges.noAugsChallenge.curCompletions);
            root.Add("version", (int)Plugin.Character.settings.rebirthDifficulty);

            return root.ToString();
        }

        private class Augment
        {
            public long level;
            public long upgrade;
            public long boost;
        }
    }
}