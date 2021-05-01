using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Alfred
{
    public class DocumentSection
    {
        public int IndentSize { get; set; }
        public string Contents { get; set; }
        public bool IsComment { get; set; }
        public List<DocumentSection> Children { get; set; } = new List<DocumentSection>();

        public static DocumentSection[] Parse(string text)
        {
            var output = new List<DocumentSection>();
            var blankLineEscrow = new List<DocumentSection>();
            var lines = text.Split('\n');
            for(int i = 0; i < lines.Length; i++)
            {
                var currentLine = lines[i].Trim();
                var indentSize = 0;
                var spaceMatch = Regex.Match(lines[i], @"^(\s*)[^\s]");
                if (spaceMatch.Success) indentSize = spaceMatch.Groups[1].Value.Length;
                var newSection = new DocumentSection()
                    { 
                        Contents = currentLine,
                        IndentSize = indentSize
                    };

                if(newSection.Contents == "")
                {
                    blankLineEscrow.Add(newSection);
                }
                else
                {
                    // Look for a parent
                    var parentFound = false;
                    for(int j = output.Count-1; j >=0; j--)
                    {
                        if(output[j].Contents != "" && newSection.IndentSize > output[j].IndentSize)
                        {
                            foreach(var blank in blankLineEscrow)
                            {
                                blank.IndentSize = newSection.IndentSize;
                                output[j].Children.Add(blank);
                            }
                            blankLineEscrow.Clear();
                            output[j].Children.Add(newSection);
                            parentFound = true;
                            break;
                        }
                    }
                    if (!parentFound)
                    {
                        output.AddRange(blankLineEscrow);
                        blankLineEscrow.Clear();
                        output.Add(newSection);
                    }
                }

            }
            return output.ToArray();
        }
    }
}
