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
            try {
                sw = new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source\\repos\\logs.txt")) { AutoFlush = true };
            } catch (Exception ex) {
                Plugin.Character.inventoryController.tooltip.showTooltip($"Exception: {ex.Message}");
                return;
            }
            sw.WriteLine(json);
            var energyNgus = Plugin.Character.NGU.skills.ToList();
            var magicNgus = Plugin.Character.NGU.magicSkills.ToList();
            var targets = JSON.Parse(json).AsArray;
            Dictionary<string, bool> anyChanged = [];
            List<string> difficulties = ["normal", "evil", "sadistic"];
            foreach (var diff in difficulties) {
                anyChanged.Add(diff, false);
            }
            List<NGUData> ParseResponse(JSONArray json)
            {
                List<NGUData> retVal = [];
                for (var x = 0; x < 9; x++) {
                    retVal.Add(new NGUData(targets[x]["normal"].ToString(), energyNgus[x].target, "normal", x, "energy"));
                    retVal.Add(new NGUData(targets[x]["evil"].ToString(), energyNgus[x].evilTarget, "evil", x, "energy"));
                    retVal.Add(new NGUData(targets[x]["sadistic"].ToString(), energyNgus[x].sadisticTarget, "sadistic", x, "energy"));
                }
                for (var x = 0; x < 7; x++) {
                    retVal.Add(new NGUData(targets[x + 9]["normal"].ToString(), magicNgus[x].target, "normal", x + 9, "magic"));
                    retVal.Add(new NGUData(targets[x + 9]["evil"].ToString(), magicNgus[x].evilTarget, "evil", x + 9, "magic"));
                    retVal.Add(new NGUData(targets[x + 9]["sadistic"].ToString(), magicNgus[x].sadisticTarget, "sadistic", x + 9, "magic"));
                }
                return retVal;
            }
            var data = ParseResponse(targets);
            NGUData.ProcessValues(data);

            try {
                for (var x = 0; x < 9; x++) {
                    energyNgus[x].target = data.Where(o => o.index == x && o.difficulty == "normal").First()?.newTarget ?? 0;
                    energyNgus[x].evilTarget = data.Where(o => o.index == x && o.difficulty == "evil").First()?.newTarget ?? 0;
                    energyNgus[x].sadisticTarget = data.Where(o => o.index == x && o.difficulty == "sadistic").First()?.newTarget ?? 0;
                }
                for (var x = 0; x < 7; x++) {
                    magicNgus[x].target = data.Where(o => o.index == x + 9 && o.difficulty == "normal").First()?.newTarget ?? 0;
                    magicNgus[x].evilTarget = data.Where(o => o.index == x + 9 && o.difficulty == "evil").First()?.newTarget ?? 0;
                    magicNgus[x].sadisticTarget = data.Where(o => o.index == x + 9 && o.difficulty == "sadistic").First()?.newTarget ?? 0;
                }
            } catch (Exception ex) {
                sw.WriteLine(ex.Message);
            }

            sw.Flush();
            sw.Close();
            _uiUpdateAction = Plugin.Character.NGUController.refreshMenu;
        }

        public class NGUData
        {
            public long newTarget;
            public long oldTarget;
            public int index;
            public string difficulty;
            public double multiplier;
            public string type;

            public NGUData(string jsonString, long currentTarget, string difficulty, int index, string type)
            {
                this.oldTarget = currentTarget;
                this.index = index;
                this.difficulty = difficulty;
                this.type = type;
                newTarget = ParseValue(jsonString);
            }

            private long ParseValue(string jsonString)
            {
                //sw.WriteLine("--------------------");
                //sw.WriteLine(jsonString);
                var items = jsonString.Replace("\"", "").Trim(['"', ' ']).Split(' ');
                var newLevel = items.First();
                var multiplierString = items.Last().Trim([')', '(', (char)215]);
                //sw.WriteLine($"multiplier string: {multiplierString} from {jsonString}");
                multiplier = float.Parse(multiplierString);
                if (multiplier < 1.1f) {
                    return -1;
                }
                //sw.WriteLine(item);
                var val = ParseNumber(newLevel);
                sw.WriteLine($"{newLevel} parsed to {val}");
                return val > 0 ? val : -1;
            }

            public static void ProcessValues(List<NGUData> data)
            {
                var types = data.Select(x => x.type).Distinct();
                var difficulties = data.Select(x => x.difficulty).Distinct();
                foreach (var type in types) {
                    foreach (var diff in difficulties) {
                        var these = data.Where(x => x.type == type && x.difficulty == diff);
                        if (!these.Any(x => x.newTarget > 0)) {
                            foreach (var item in these) {
                                item.newTarget = 0;
                            }
                        }
                    }
                }
            }
        }

        private static long ParseNumber(string input)
        {
            input = input.ToLower();
            long multiplier = 0;
            double number = 0;
            long retVal = 0;
            var lastChar = input.Last();
            if (char.IsLetter(lastChar)) {
                multiplier = lastChar switch {
                    'k' => 1000,
                    'm' => 1000000,
                    'b' => 1000000000,
                    _ => 1
                };
                var numberString = input.Substring(0, input.Length - 1);
                var digitsAfterDecimal = 0;
                number = double.Parse(numberString);
                if (numberString.Contains('.')) {
                    digitsAfterDecimal = numberString.Length - numberString.IndexOf('.');
                }
                number *= Math.Pow(10, digitsAfterDecimal);
                multiplier /= (long)Math.Pow(10, digitsAfterDecimal);

                retVal = (long)number;
                retVal *= multiplier;
                return retVal;
            }
            else {
                number = double.Parse(input);
                return (long)number;
            }
        }
    }
}