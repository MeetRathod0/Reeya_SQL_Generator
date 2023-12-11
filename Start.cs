using Reeya_SQL_Generator.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reeya_SQL_Generator
{
    public class Start
    {
        string app_name = ">>>>>>>>> Reeya's SQL Generator <<<<<<<<<";
        string pat = "******************************************";
        void center()
        {
            Console.SetCursorPosition((Console.WindowWidth - app_name.Length) / 2, Console.CursorTop);
        }
        void title()
        {
            Console.SetCursorPosition((Console.WindowWidth - app_name.Length) / 2, Console.CursorTop);
            Console.WriteLine(pat);
            Console.SetCursorPosition((Console.WindowWidth - app_name.Length) / 2, Console.CursorTop);
            Console.WriteLine(app_name);
            Console.SetCursorPosition((Console.WindowWidth - app_name.Length) / 2, Console.CursorTop);
            Console.WriteLine(pat);
        }
        public void start_input()
        {
            ;
            title();
            Console.Write(@"
[1]. SP with Merge & TVP
[2]. SP with Update & Insert
[0]. Exit
Enter your choice >> ");
            string input = Console.ReadLine();
            if (input.Equals("1"))
            {
                Console.Clear();
                title();
                center();

                Console.Write("1. Enter SP Name >> ");

                string sp_name = Console.ReadLine();
                center();
                Console.Write("2. Enter Table Name >> ");
                string table_name = Console.ReadLine();
                center();
                Console.Write("3. Enter TVP Name >> ");
                string tvp_name = Console.ReadLine();
                bool f = new SQLGenerator().gen_sp_with_merge_tvp(sp_name, table_name, tvp_name);
                Console.Clear();
                title();
                center();
                if (f)
                {
                    Console.WriteLine("Done! Check dir: IO/output.txt");
                }
                else
                {
                    Console.WriteLine("Fail! Check input dir: IO/input.txt");
                }

            }
            else if (input.Equals("2"))
            {
                Console.Clear();
                title();
                center();
                Console.Write("1. Enter SP Name >> ");

                string sp_name = Console.ReadLine();
                center();
                Console.Write("2. Enter Table Name >> ");
                string table_name = Console.ReadLine();
                center();
                Console.Write("3. You wants to add default NULL parameters? [Y/N] >> ");
                string def_null = Console.ReadLine().ToLower();
                bool f = new SQLGenerator().gen_sp_with_insert_update(sp_name, table_name, def_null);
                Console.Clear();
                title();
                center();
                if (f)
                {
                    Console.WriteLine("Done! Check dir: IO/output.txt");
                }
                else
                {
                    Console.WriteLine("Fail! Check input dir: IO/input.txt");
                }
            }
            else
            {
                Console.Clear();
                title();
                center();
                Console.WriteLine("Bye!");
            }

        }

    }
}
