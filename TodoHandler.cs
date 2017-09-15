using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Alfred
{
    class TodoHandler
    {
        internal const string LINEPREFIX = "_alfred__";
        internal const string LINEPOSTFIX = "]";
        internal const string LINEMATCH = "^" + LINEPREFIX + "(.*?)" + LINEPOSTFIX;

        AlfredSettings _settings;

        Dictionary<string, Line> _linesLookup = new Dictionary<string, Line>();
        Dictionary<string, Line> _previousLinesLookup = new Dictionary<string, Line>();
        List<Line> _allLines = new List<Line>();
        List<Line> _frozenLines = new List<Line>();
        List<Line> _doneLines = new List<Line>();
        string _originalDirectory;
        string _backupDirectory;
        string _nameTemplate;
        DateTime _freezerCutoff;
        DateTime _runTime = DateTime.Now;


        private string AlfredFileName { get { return _originalDirectory + @"\" + _nameTemplate.Replace("^REPLACEME^", "_Alfred"); } }
        private string DoneFileName { get { return _originalDirectory + @"\" + _nameTemplate.Replace("^REPLACEME^", "_Done"); } }
        private string FreezerFileName { get { return _originalDirectory + @"\" + _nameTemplate.Replace("^REPLACEME^", "_Freezer"); } }
        private string BackupFileName { get { return _backupDirectory + @"\" + _nameTemplate.Replace("^REPLACEME^", _runTime.ToString("_yyyyMMdd_HHmmss")); } }
        private string OriginalFileName { get { return _originalDirectory + @"\" + _nameTemplate.Replace("^REPLACEME^", ""); } }

        public TodoHandler(string todoFileName)
        {
            _settings = new AlfredSettings(todoFileName);
            _freezerCutoff = DateTime.Now - _settings.TimeToLive;
            GenerateNameTemplate(todoFileName);
            ReadStoredContents();
            ReadRawContents(File.ReadAllText(todoFileName));
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Read stuff that Alfred remembered from last time
        /// </summary>
        //------------------------------------------------------------------------------
        private void ReadStoredContents()
        {
            if (!File.Exists(AlfredFileName)) return;

            using (StreamReader reader = new StreamReader(AlfredFileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Line newLine = new Line(line, _settings.SpacesPerTab);
                    if (_previousLinesLookup.ContainsKey(newLine.Contents))
                    {
                        // THis is bad.  We'll ignore for now, but everything should be unique
                    }
                    else _previousLinesLookup.Add(newLine.Contents, newLine);
                }
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Read in the actual todo file and integrate with stuff alfred remembers
        /// </summary>
        //------------------------------------------------------------------------------
        private void ReadRawContents(string contents)
        {
            StringReader reader = new StringReader(contents);
            string line;
            int lineNumber = 1;
            bool inCommentSection = false;
            bool inDoneSection = false;
            bool inFrozenSection = false;
            int sectionIndent = 0;
            Stack<Line> parentStack = new Stack<Line>();
            int stackIndent = -1;
            Line currentParent = null;
            Line lastParentableItem = null;

            while ((line = reader.ReadLine()) != null)
            {
                Line newLine = new Line(line, _settings.SpacesPerTab) { LineNumber = lineNumber };
                
                // Look at the old dates for lines in the todo file and remember them
                if (_previousLinesLookup.ContainsKey(newLine.Contents))
                {
                    newLine.DateFirstAppeared = _previousLinesLookup[newLine.Contents].DateFirstAppeared;
                }

                lineNumber++;
                string decorator = "";
                int decoratorCount = 0;
                if (newLine.Contents.StartsWith(_settings.CommentStart)) inCommentSection = true;
                if (newLine.Contents.StartsWith(_settings.CommentEnd)) inCommentSection = false;
                if (newLine.Indent <= sectionIndent)
                {
                    inDoneSection = false;
                    inFrozenSection = false;
                }

                if (newLine.Contents == "" || LineIsComment(newLine) || LineIsStatic(newLine) || inCommentSection)
                {
                    if (LineIsStatic(newLine)) ReplaceItems(newLine);
                    _allLines.Add(newLine);

                    // Empty lines and comments constitute a break in continuity
                    inDoneSection = false;
                    inFrozenSection = false;
                    parentStack.Clear();
                    stackIndent = -1;
                    currentParent = null;
                    lastParentableItem = null;
                }
                else
                {
                    // Modify the ages of indented items so that we don't accidentally
                    // freeze children where the child is older than the parent
                    if (_settings.IncludeIndentedItemsAsChildren)
                    {
                        if (stackIndent == -1) stackIndent = newLine.Indent;

                        if (newLine.Indent > stackIndent)
                        {
                            parentStack.Push(lastParentableItem);
                            currentParent = lastParentableItem;
                        }
                        else if (newLine.Indent < stackIndent)
                        {
                            while (parentStack.Count > 0 && parentStack.Peek().Indent >= newLine.Indent) parentStack.Pop();
                            if (parentStack.Count == 0) currentParent = null;
                            else currentParent = parentStack.Peek();
                        }

                        if (currentParent != null)
                        {
                            if (newLine.DateFirstAppeared < currentParent.DateFirstAppeared)
                            {
                                newLine.DateFirstAppeared = currentParent.DateFirstAppeared.AddSeconds(5);
                            }
                        }

                        stackIndent = newLine.Indent;
                        lastParentableItem = newLine;
                    }



                    if (inDoneSection)
                    {
                        _doneLines.Add(newLine);
                    }
                    else if (inFrozenSection)
                    {
                        _frozenLines.Add(newLine);
                    }
                    else if (LineIsDone(newLine))
                    {
                        _doneLines.Add(newLine);
                        inDoneSection = _settings.IncludeIndentedItemsAsChildren;
                        sectionIndent = newLine.Indent;
                    }
                    else if (newLine.DateFirstAppeared < _freezerCutoff)
                    {
                        _frozenLines.Add(newLine);
                        inFrozenSection = _settings.IncludeIndentedItemsAsChildren;
                        sectionIndent = newLine.Indent;
                    }
                    else
                    {
                        ReplaceItems(newLine);

                        while (_linesLookup.ContainsKey(newLine.Contents + decorator))
                        {
                            decoratorCount++;
                            decorator = " ~" + decoratorCount;
                        }
                        newLine.Contents += decorator;

                        if (newLine.Contents != "") _linesLookup.Add(newLine.Contents, newLine);
                        _allLines.Add(newLine);
                    }
                }
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// ReplaceItems 
        /// names.
        /// </summary>
        //------------------------------------------------------------------------------
        private void ReplaceItems(Line newLine)
        {
            foreach (var replaceSet in _settings.ReplaceTokens)
            {
                newLine.Contents = newLine.Contents.Replace(replaceSet.Item1, replaceSet.Item2);
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Turn the original todo file name into a template for alfred's utility file 
        /// names.
        /// </summary>
        //------------------------------------------------------------------------------
        private void GenerateNameTemplate(string todoFileName)
        {
            string directory = Path.GetDirectoryName(todoFileName);
            if (directory == "") directory = Directory.GetCurrentDirectory();
            _originalDirectory = directory;
            _backupDirectory = Path.Combine(_originalDirectory, "__AlfredBackups");
            string filename = Path.GetFileNameWithoutExtension(todoFileName);
            string extension = Path.GetExtension(todoFileName);
            _nameTemplate = filename + "^REPLACEME^" + extension;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Write out the parts of the todo file we need to remember
        /// </summary>
        //------------------------------------------------------------------------------
        internal void WriteMemoryFile()
        {
            List<Line> sortMe = new List<Line>();

            foreach (var line in _linesLookup.Values)
            {
                sortMe.Add(line);
            }
            sortMe.Sort((line1, line2) => line1.LineNumber.CompareTo(line2.LineNumber)); 

            using (StreamWriter writer = new StreamWriter(AlfredFileName))
            {
                foreach (var line in sortMe)
                {
                    writer.WriteLine(LINEPREFIX + line.DateFirstAppeared + LINEPOSTFIX + line.LeadingWhiteSpace + line.Contents);
                }
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Indicate if this line starts with a comment token
        /// </summary>
        //------------------------------------------------------------------------------
        private bool LineIsComment(Line line)
        {
            foreach (var token in _settings.CommentTokens)
            {
                if (line.Contents.StartsWith(token)) return true;
            }
            return false;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Indicate if this line starts with a static token
        /// </summary>
        //------------------------------------------------------------------------------
        private bool LineIsStatic(Line line)
        {
            foreach (var token in _settings.StaticTokens)
            {
                if (line.Contents.StartsWith(token)) return true;
            }
            return false;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Indicate if this line starts with a "done" token
        /// </summary>
        //------------------------------------------------------------------------------
        private bool LineIsDone(Line line)
        {
            foreach (var token in _settings.DoneTokens)
            {
                if (line.Contents.StartsWith(token)) return true;
            }
            return false;
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Put frozen lines at the top of a freezer file
        /// </summary>
        //------------------------------------------------------------------------------
        internal void WriteFreezerFile()
        {
            if (_frozenLines.Count == 0) return;

            string tempFileName = "AlfredFreezerTemp" + DateTime.Now.Ticks + ".dat";
            using (StreamWriter writer = new StreamWriter(tempFileName))
            {
                writer.WriteLine("--- FROZEN " + DateTime.Now + " ----------------------------------------------");
                foreach (var line in _frozenLines)
                {
                    writer.WriteLine(line.LeadingWhiteSpace + line.Contents);
                }
                writer.WriteLine();
            }

            if (File.Exists(FreezerFileName))
            {
                File.AppendAllText(tempFileName, File.ReadAllText(FreezerFileName));
            }
            File.Copy(tempFileName, FreezerFileName, true);
            File.Delete(tempFileName);
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Write back the updated todo file
        /// </summary>
        //------------------------------------------------------------------------------
        internal void WriteUpdatedTodoFile()
        {
            using (StreamWriter writer = new StreamWriter(OriginalFileName))
            {
                foreach (var line in _allLines)
                {
                    writer.WriteLine(line.LeadingWhiteSpace + line.Contents);
                }

                if (_settings.SettingsFound == 0)
                {
                    writer.Write(@"
-------- Alfred Settings --------------------------------------------------
--This setting section was complimentarily added by Alfred. Feel free to 
--modify it as you see fit.  (Note: For most settings, Alfred ignores the 
--white space at the beginning of the line.)
--
--Alfred:CommentTag=##
--  More than one comment tag can be specified.  Any lines beginning with 
--  a comment tag will be ignored by Alfred.  Alfred always recognizes '--' 
--  as a comment.  
--
--Alfred:StaticTag=##
--  More than one static tag can be specified.  Any lines beginning with 
--  a static tag will be ignored by Alfred.  Static tags are different
--  than comment tags in that text on static lines can be replaced
--
--Alfred:IgnoreSectionStartTag=<!--
--Alfred:IgnoreSectionEndTag=-->
--  Alfred will ignore all text between the IgnoreSectionStartTag and 
--  IgnoreSectionEndTag. These tags must appear as the first part of the 
--  line they are on.
--
--Alfred:DoneTag=[x]
--  If Alfred sees a 'done' tag at the beginning of a line, Alfred will 
--  automatically move that item to the *_Done.* file in the same directory 
--  as the todo file.  You can specify more than one done tag.
--
--Alfred:Replace=item`newitem
--  For any lines that are not comments, alfred will replace any instance
--  if item with newitem.  Use this to automatically clean up tags when
--  Alfred runs. 
--
--Alfred:DaysToLive=30
--  This settings tells Alfred how long to let an item languish in your 
--  todo list before it goes to the freezer.  After this date, Alfred 
--  will automatically move on old item to the *_Freezer.* file in the same 
--  directory as the todo file.  If you make any change to the contents of 
--  a line, Alfred will reset his timer for it.  To see how old Alfred 
--  thinks a line is, look in the *_Alfred.* file in the same directory
--  as the todo file.   
--
--Alfred:IncludeIndentedItemsAsChildren
--  Specifying this setting will cause Alfred to consider indented lines 
--  immediately following a line as 'children' of that line.  Alfred will 
--  include children when moving a task that is done or frozen.
--
--Alfred:SpacesPerTab=4
--  To help Alfred understand how much something is indented,
--  he has to convert tabs to spaces, so 'spacesPerTab' is how Alfred 
--  figures this out.
--
--## Random Things to Know About Alfred ##
--
-- - Alfred insists that every task is unique.  If he finds two identical
--   tasks, he will tag a small bit of text at the end to tell them apart.
-- - Alfred usually understands when you move tasks around in your list, but
--   it is possible to confuse Alfred by creating identical tasks, or by 
--   drastically changing indentation, so be careful.  Remember that you
--   can always tell what Alfred is thinking by looking at the contents of
--   the *_Alfred.* file.   
-- - Alfred will add this settings section to any todo file that does not
--   have any settings in it.  So, if there is at least one setting, 
--   Alfred will not try to be 'helpful' by adding this section.   
-- - Alfred is 100% loyal and will always do his best to serve you. 
------------------------------------------------------------------------------
");

                }
            }
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Puts the todo file into a permanent storage folder
        /// </summary>
        //------------------------------------------------------------------------------
        internal void WriteBackup()
        {
            if (!Directory.Exists(_backupDirectory)) Directory.CreateDirectory(_backupDirectory);
            File.Copy(OriginalFileName, BackupFileName);           
        }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Put items marked as done into a special file
        /// </summary>
        //------------------------------------------------------------------------------
        internal void WriteDoneFile()
        {
            if (_doneLines.Count == 0) return;

            string tempFileName = "AlfredDoneTemp" + DateTime.Now.Ticks + ".dat";
            using (StreamWriter writer = new StreamWriter(tempFileName))
            {
                writer.WriteLine("--- DONE " + DateTime.Now + " ----------------------------------------------");
                foreach (var line in _doneLines)
                {
                    writer.WriteLine(line.LeadingWhiteSpace + line.Contents);
                }
                writer.WriteLine();
            }

            if (File.Exists(DoneFileName))
            {
                File.AppendAllText(tempFileName, File.ReadAllText(DoneFileName));
            }
            File.Copy(tempFileName, DoneFileName, true);
            File.Delete(tempFileName);
        }
    }

    //------------------------------------------------------------------------------
    /// <summary>
    /// Represents a single line from the todo file
    /// </summary>
    //------------------------------------------------------------------------------
    class Line
    {
        public int LineNumber { get; set; }
        public int Indent { get; set; }
        public string LeadingWhiteSpace { get; set; }
        public string Contents { get; set; }
        public DateTime DateFirstAppeared { get; set; }

        //------------------------------------------------------------------------------
        /// <summary>
        /// Constructor
        /// </summary>
        //------------------------------------------------------------------------------
        public Line(string lineContents, int spacesPerTab)
        {
            DateFirstAppeared = DateTime.Now;
            lineContents = lineContents.TrimEnd();

            // Pull the date off of lines marked by alfred
            Match match = Regex.Match(lineContents, TodoHandler.LINEMATCH);
            if(match.Success)
            {
                DateFirstAppeared = DateTime.Parse(match.Groups[1].Value);
                lineContents = lineContents.Substring(match.Groups[0].Value.Length);
            }

            // Break the line into components we care about
            int firstContentsSpot = 0;
            while (firstContentsSpot < lineContents.Length
                && Char.IsWhiteSpace(lineContents[firstContentsSpot])) firstContentsSpot++;

            LeadingWhiteSpace = lineContents.Substring(0, firstContentsSpot);
            Indent = LeadingWhiteSpace.Replace("\t", new string(' ', spacesPerTab)).Length;
            Contents = lineContents.Substring(firstContentsSpot);
        }
    }
}
