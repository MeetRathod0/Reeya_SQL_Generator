using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reeya_SQL_Generator.Support
{

    public class FileManager
    {
        public string dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\IO\";
        //public string input_file_name = Path.Combine(Environment.CurrentDirectory,@"IO\","input.txt");
        public string input_file_name = "input.txt";
        public string output_file_name = "output.txt";
        public string sp_test_output_file_name = "sp_test.txt";
        public string[] read()
        {
            string[] lines;
            try
            {
                lines = File.ReadAllLines(dir + input_file_name);
                if (lines.Length == 0)
                    throw new Exception("Input File (input.txt) is empty!");

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return lines;
        }
        public bool write(string input)
        {
            try
            {
                File.WriteAllText(dir + output_file_name, input);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool write_test(string input)
        {
            try
            {
                File.WriteAllText(dir + sp_test_output_file_name, input);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
