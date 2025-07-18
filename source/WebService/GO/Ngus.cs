using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace jshepler.ngu.mods.WebService.GO
{
    internal class Ngus
    {
        private static Action _uiUpdateAction;
        private static StreamWriter sw;

        static Ngus()
        {
            Plugin.OnUpdate += (o, e) => {
                if (_uiUpdateAction != null) {
                    _uiUpdateAction();
                    _uiUpdateAction = null;
                }
            };
        }

        internal static void ApplyTargets(string json)
        {
            sw = new("C:\\Users\\Austin Wiley\\source\\repos\\logs.txt") { AutoFlush = true };
            sw.WriteLine(json);
            var energyNgus = Plugin.Character.NGU.skills.ToList();
            var magicNgus = Plugin.Character.NGU.magicSkills.ToList();
            var targets = JSON.Parse(json).AsArray;
            static long ParseValue(string json)
            {
                var item = json.Replace("\"", "").Trim(['"']).Split(' ').First();
                sw.WriteLine(item);
                var val = (long)double.Parse(item);
                return val > 0 ? val : -1;
            }
            try {
                for (var x = 0; x < 9; x++) {
                    energyNgus[x].target = ParseValue(targets[x]["normal"].ToString());
                    energyNgus[x].evilTarget = ParseValue(targets[x]["evil"].ToString());
                    energyNgus[x].sadisticTarget = ParseValue(targets[x]["sadistic"].ToString());
                }
                for (var x = 0; x < 7; x++) {
                    magicNgus[x].target = ParseValue(targets[x + 9]["normal"].ToString());
                    magicNgus[x].evilTarget = ParseValue(targets[x + 9]["evil"].ToString());
                    magicNgus[x].sadisticTarget = ParseValue(targets[x + 9]["sadistic"].ToString());
                }
            } catch (Exception ex) {
                sw.WriteLine(ex.Message);
            }

            sw.Flush();
            sw.Close();
            _uiUpdateAction = Plugin.Character.NGUController.refreshMenu;
        }
    }
}