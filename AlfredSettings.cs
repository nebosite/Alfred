using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alfred
{
    class AlfredSettings
    {
        const string SETTINGTOKEN = "--Alfred:";
        public List<string> CommentTokens = new List<string>();
        public List<string> StaticTokens = new List<string>();
        public List<string> DoneTokens = new List<string>();
        public List<Tuple<string, string>> ReplaceTokens = new List<Tuple<string, string>>();

        public int SettingsFound { get; private set; }
        public string CommentStart { get; private set; }
        public string CommentEnd { get; private set; }
        public bool IncludeIndentedItemsAsChildren { get; private set; }
        public int SpacesPerTab { get; private set; }
        public TimeSpan TimeToLive { get; private set; }

        public  AlfredSettings(string todoFileName)
        {
            CommentTokens.Add("--");
            char[] splitChars = new char[]{':','='};
            IncludeIndentedItemsAsChildren = false;
            SpacesPerTab = 4;
            TimeToLive = TimeSpan.FromDays(30);
            CommentStart = "<!--";
            CommentEnd = "-->";

            foreach (var line in File.ReadAllLines(todoFileName))
            {
                if (line.StartsWith(SETTINGTOKEN))
                {
                    SettingsFound++;
                    string[] parts = line.Trim().Split(splitChars, 3);
                    if (parts.Length < 2) continue;
                    string name = parts[1].ToLower();
                    string value = null;
                    if (parts.Length == 3) value = parts[2].Trim();

                    switch (name)
                    {
                        case "commenttag":
                            if (value != null && value != "") CommentTokens.Add(value);
                            break;
                        case "statictag":
                            if (value != null && value != "") StaticTokens.Add(value);
                            break;
                        case "ignoresectionstarttag":
                            if (value != null && value != "") CommentStart = value;
                            break;
                        case "ignoresectionendtag":
                            if (value != null && value != "") CommentEnd = value;
                            break;
                        case "donetag":
                            if (value != null && value != "") DoneTokens.Add(value);
                            break;
                        case "replace":
                            if (value != null && value != "")
                            {
                                var valueParts = value.Split('`');
                                var tuple = new Tuple<string, string>(
                                    valueParts[0],
                                    (valueParts.Length > 1) ? valueParts[1] : "");
                                ReplaceTokens.Add(tuple);
                            }
                            break;
                        case "includeindenteditemsaschildren":
                            IncludeIndentedItemsAsChildren = true;
                            break;
                        case "spacespertab":
                            if (value != null && value != "") SpacesPerTab = int.Parse(value);
                            break;
                        case "daystolive":
                            if (value != null && value != "") TimeToLive = TimeSpan.FromDays(double.Parse(value));
                            break;
                        default:
                            throw new ArgumentException("Unknown Setting: " + parts[1]);
                            break;
                    }
                }

            }

        }



    }
}
