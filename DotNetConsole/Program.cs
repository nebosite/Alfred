using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alfred
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return 0;
            }

            string todoFileName = args[0];

            if (!File.Exists(todoFileName))
            {
                Console.WriteLine("Can't find file '" + todoFileName + "'");
                return 1;
            }

            try
            {
                File.AppendAllText("Alfred.log", "Running against " + todoFileName + " at " + DateTime.Now + Environment.NewLine);

                TodoHandler handler = new TodoHandler(todoFileName);

                handler.WriteBackup();
                handler.WriteFreezerFile();
                handler.WriteDoneFile();
                handler.WriteMemoryFile();
                handler.WriteUpdatedTodoFile();
            }
            catch (Exception e)
            {
                File.AppendAllText("Alfred.log", e.ToString() + Environment.NewLine);
            }

            return 0;
        }


        static void ShowUsage()
        {
                Console.WriteLine(@"
Usage:  Alfred (name of todo file)

Alfred will automatically manage and clean your todo file while
you get to focus on being Batman.

Alfred never deletes any data.  Old items that don't get done are
moved to a 'freezer' file. Finished items are moved to a 'done' file.  
These files both live in the same directory as the original todo file. 
Alfred keeps backups of your todo file just in case. These can
be found in the __alfredbackups folder in the same directory as 
the todo file.

Alfred will place instructions and settings at the bottom of your
todo file the first time that you run it. If you want to see what
Alfred is thinking about your todo file, look for the *_Alfred.* file
in the same directory.

To employ Alfred, the best thing to do is set up a scheduled task 
to run while you are asleep (or running around saving Gotham City).
");

        }
    }
}

