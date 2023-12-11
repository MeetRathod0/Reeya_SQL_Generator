using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Reeya_SQL_Generator.Support
{
    public class SQLGenerator
    {
        // list of readed lines
        ArrayList INPUT;
        List<List<string>> property_list = new List<List<string>>();
        List<string> cols_name_list;
        List<string> cols_dtype_list;
        FileManager fileManager = new FileManager();
        readonly string[] SqlServerTypes = { "bigint", "binary", "bit", "char", "date", "datetime", "datetime2", "datetimeoffset", "decimal", "filestream", "float", "geography", "geometry", "hierarchyid", "image", "int", "money", "nchar", "ntext", "numeric", "nvarchar", "real", "rowversion", "smalldatetime", "smallint", "smallmoney", "sql_variant", "text", "time", "timestamp", "tinyint", "uniqueidentifier", "varbinary", "varchar", "xml" };
        readonly string[] CSharpTypes = { "long", "byte[]", "bool", "char", "DateTime", "DateTime", "DateTime", "DateTimeOffset", "decimal", "byte[]", "double", "Microsoft.SqlServer.Types.SqlGeography", "Microsoft.SqlServer.Types.SqlGeometry", "Microsoft.SqlServer.Types.SqlHierarchyId", "byte[]", "int", "decimal", "string", "string", "decimal", "string", "Single", "byte[]", "DateTime", "short", "decimal", "object", "string", "TimeSpan", "byte[]", "byte", "Guid", "byte[]", "string", "string" };


        public SQLGenerator()
        {
            string[] ip = fileManager.read();
            // ASSIGN INPUT
            assign_filter_lines(ip);
            // ASSIGN VALUE IN property_list, cols_name_list, cols_dtype_list
            assign_col_property();
        }
        // extract only sql syntax
        void assign_filter_lines(string[] sl)
        {
            ArrayList al = new ArrayList();
            foreach (string i in sl)
            {
                if (i != "" && i != "&" && i != "," && i != "\n" && i != " ")
                    al.Add(i);
            }
            INPUT = al;
            ArrayList ls = new ArrayList();
            Regex rx = new Regex(@"^[A-z0-9_]+ [A-z0-9]+|primary key|null|not null$");
            foreach (var item in INPUT)
            {
                if (rx.IsMatch(item.ToString()))
                    ls.Add(item);
            }
            INPUT = ls;
        }
        void assign_col_property()
        {

            foreach (var i in INPUT)
            {
                var s = i.ToString().Split(" ");
                var properties = new List<string>();
                foreach (var k in s)
                {
                    string chr = k.Trim();
                    chr = chr.Replace(" ", "");
                    chr = chr.Replace(",", "");
                    if (!chr.ToString().Equals(""))
                        //Console.WriteLine(chr);
                        properties.Add(chr);

                }
                property_list.Add(properties);
            }
            cols_name_list = property_list.Select(x => x[0]).ToList();
            cols_dtype_list = property_list.Select(x => x[1]).ToList();
        }
        public bool gen_sp_with_merge_tvp(string sp_name, string table_name, string tvp_name)
        {

            string SP = @"
CREATE OR ALTER PROCEDURE {0} 
	@tvp {1} READONLY
AS BEGIN
	MERGE INTO {2} AS dest
	USING (
		SELECT
{3}
		FROM @tvp
	) AS source ON 
{4}
	WHEN MATCHED THEN UPDATE SET
{5}
	WHEN NOT MATCHED THEN INSERT 
	(
{6}
	) VALUES
	(
{7}
	);
END;";
            try
            {

                string select_cols = string.Join("", cols_name_list.Select(x => "\t\t    " + x + ",\n"));
                select_cols = select_cols.Remove(select_cols.Length - 2);

                var key_match = "";
                var update_cols = "";
                var insert_cols = "";
                // set primary
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("primary")).ToList();
                    if (is_primary.Count() > 0)
                    {
                        if (key_match.Equals(""))
                        {
                            key_match += "        dest." + i[0] + " = source." + i[0];
                        }
                        else
                        {
                            key_match += " AND \n        dest." + i[0] + " = source." + i[0];
                        }
                    }
                }
                // set update
                var temp_list = new List<string>();
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("primary")).ToList();
                    if (is_primary.Count() == 0)
                    {
                        temp_list.Add("dest." + i[0] + " = source." + i[0]);
                    }
                }
                update_cols = string.Join("", temp_list.Select(x => "\t    " + x + ",\n"));
                update_cols = update_cols.Remove(update_cols.Length - 2);

                // set insert
                var temp_list2 = new List<string>();
                temp_list = new List<string>();
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("identity")).ToList();
                    if (is_primary.Count() == 0)
                    {
                        temp_list.Add("source." + i[0]);
                        temp_list2.Add(i[0]);
                    }
                }
                insert_cols = string.Join("", temp_list.Select(x => "\t    " + x + ",\n"));
                insert_cols = insert_cols.Remove(insert_cols.Length - 2);

                string as_insert_cols = string.Join("", temp_list2.Select(x => "\t    " + x + ",\n"));
                as_insert_cols = as_insert_cols.Remove(as_insert_cols.Length - 2);


                SP = string.Format(SP, sp_name, tvp_name, table_name, select_cols, key_match, update_cols, as_insert_cols, insert_cols);
                fileManager.write(SP);
                gen_exec_sp_with_merge_tvp(sp_name,tvp_name);
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public bool gen_sp_with_insert_update(string sp_name, string table_name, string def_null)
        {

            string SP = @"
CREATE OR ALTER PROCEDURE {0} 
{1}
AS BEGIN
    IF EXISTS(SELECT 1 FROM {2} WHERE {3}) BEGIN
        UPDATE {4} SET
{5}
        WHERE
            {6}
    END
    ELSE BEGIN
        INSERT INTO {7}
        (
{8}
        )
        VALUES 
        (
{9}
        );
    END;
	
END;";
            try
            {
                string vars = "";
                if (def_null.Equals("y"))
                {
                    foreach (var i in property_list)
                    {
                        vars += "    @" + i[0] + " " + i[1] + " = NULL,\n";
                    }
                }
                else
                {
                    foreach (var i in property_list)
                    {
                        vars += "    @" + i[0] + " " + i[1] + ",\n";
                    }
                }

                vars = vars.Remove(vars.Length - 2);


                var key_match = "";
                var update_cols = "";
                var insert_cols = "";
                // set primary
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("primary")).ToList();
                    if (is_primary.Count() > 0)
                    {
                        if (key_match.Equals(""))
                        {
                            key_match += i[0] + " = @" + i[0];
                        }
                        else
                        {
                            key_match += " AND " + i[0] + " = @" + i[0];
                        }
                    }
                }
                // set update
                var temp_list = new List<string>();
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("primary")).ToList();
                    if (is_primary.Count() == 0)
                    {
                        temp_list.Add("" + i[0] + " = @" + i[0]);
                    }
                }
                update_cols = string.Join("", temp_list.Select(x => "\t\t    " + x + ",\n"));
                update_cols = update_cols.Remove(update_cols.Length - 2);

                // set insert
                var temp_list2 = new List<string>();
                temp_list = new List<string>();
                foreach (var i in property_list)
                {
                    var is_primary = i.Where(x => x.ToLower().Equals("identity")).ToList();
                    if (is_primary.Count() == 0)
                    {
                        temp_list.Add("@" + i[0]);
                        temp_list2.Add(i[0]);
                    }
                }
                insert_cols = string.Join("", temp_list.Select(x => "\t\t    " + x + ",\n"));
                insert_cols = insert_cols.Remove(insert_cols.Length - 2);

                string as_insert_cols = string.Join("", temp_list2.Select(x => "\t\t    " + x + ",\n"));
                as_insert_cols = as_insert_cols.Remove(as_insert_cols.Length - 2);


                SP = string.Format(SP, sp_name, vars, table_name, key_match, table_name, update_cols, key_match, table_name, as_insert_cols, insert_cols);
                fileManager.write(SP);
                gen_exec_sp_with_insert_update(sp_name);
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }

        List<List<string>> gen_values(int count) {
            List<List<string>> values = new List<List<string>>();
            RandomInput rinp = new RandomInput();

            int rnum = 0;
            string str = "";

            for (int index = 0; index<count; index++)
            {
                var tmp_list = new List<string>();
                foreach (var i in cols_dtype_list)
                {
                    var val = i.ToLower();
                    if (Regex.IsMatch(val, @"^bigint.*$"))
                    {
                        rnum = rinp.RandInt();
                        tmp_list.Add(rnum.ToString());

                    }
                    else if (Regex.IsMatch(val, @"^binary.*$"))
                    {
                        rnum = rinp.RandOneInt();
                        tmp_list.Add(rnum.ToString());
                    }
                    else if (Regex.IsMatch(val, @"^bit.*$"))
                    {
                        rnum = rinp.RandOneInt();
                        tmp_list.Add(rnum.ToString());
                    }
                    else if (Regex.IsMatch(val, @"^char.*$"))
                    {
                        str = rinp.RandString();
                        tmp_list.Add("'"+str+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^date.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandDate()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^datetime.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandDatetime()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^datetime2.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandDatetime()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^datetimeoffset.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandDatetime()+"'");
                    }
                    else if (Regex.IsMatch(val, @"^decimal.*$"))
                    {
                        tmp_list.Add(rinp.RandDecimal().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^filestream.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^float.*$"))
                    {
                        tmp_list.Add(rinp.RandFloat().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^geography.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^geometry.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^hierarchyid.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^image.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^int.*$"))
                    {
                        tmp_list.Add(rinp.RandInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^money.*$"))
                    {
                        tmp_list.Add(rinp.RandInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^nchar.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandString()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^ntext.*$"))
                    {

                        tmp_list.Add("'"+rinp.RandString()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^numeric.*$"))
                    {
                        tmp_list.Add(rinp.RandInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^nvarchar.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandString()+"'");
                    }
                    else if (Regex.IsMatch(val, @"^real.*$"))
                    {
                        tmp_list.Add(rinp.RandInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^rowversion.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^smalldatetime.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^smallint.*$"))
                    {
                        tmp_list.Add(rinp.RandOneInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^smallmoney.*$"))
                    {
                        tmp_list.Add(rinp.RandInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^sql_variant.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^text.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandString()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^time.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandTime()+"'");
                    }
                    else if (Regex.IsMatch(val, @"^timestamp.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandTime()+ "'");
                    }
                    else if (Regex.IsMatch(val, @"^tinyint.*$"))
                    {
                        tmp_list.Add(rinp.RandOneInt().ToString());
                    }
                    else if (Regex.IsMatch(val, @"^uniqueidentelse ifier.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^varbinary.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else if (Regex.IsMatch(val, @"^varchar.*$"))
                    {
                        tmp_list.Add("'"+rinp.RandString()+"'");
                    }
                    else if (Regex.IsMatch(val, @"^xml.*$"))
                    {
                        tmp_list.Add("NULL");
                    }
                    else
                    {

                        tmp_list.Add("NULL");
                    }
                }
                values.Add(tmp_list);
            }

            return values;
        }
        public bool gen_exec_sp_with_merge_tvp(string sp_name,string tvp_name,int vals_count = 1) 
        {


            string SP = @"
DECLARE @tvp {0}
INSERT INTO @tvp 
(
{1}
)
VALUES 
(
{2}
);
EXEC {3} @tvp=@tvp;
"; try
            {
                string as_cols = string.Join("", cols_name_list.Select(x => "\t    " + x + ",\n"));
                as_cols = as_cols.Remove(as_cols.Length - 2);

                string val_cols = "";
                var values_list = gen_values(vals_count);
                var col_names = cols_name_list;
                int idx = 0;
                foreach (var s in values_list[0])
                {
                    if (values_list[0].Count - 1 == idx)
                    {
                        val_cols += "\t    " + s + ",\n";
                    }
                    else
                    {
                        val_cols += "\t    " + s + ", -- " + col_names[idx] + "\n";

                    }
                    idx++;
                }
                val_cols = val_cols.Remove(val_cols.Length - 2);
                val_cols += " -- " + col_names[col_names.Count - 1] + "\n";

                SP = string.Format(SP, tvp_name, as_cols, val_cols, sp_name);
                fileManager.write_test(SP);
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        public bool gen_exec_sp_with_insert_update(string sp_name, int vals_count = 1)
        {
            string SP = @"
EXEC {0} {1} 
"; try
            {
                
                string val_cols = "";
                var values_list = gen_values(vals_count);
                var col_names = cols_name_list;
                int idx = 0;
                foreach (var s in values_list[0])
                {
                    val_cols += "\t    @" + col_names[idx]+"="+ s + ",\n";
                    idx++;
                }
                val_cols = val_cols.Remove(val_cols.Length - 2);
                
                SP = string.Format(SP, sp_name,val_cols);
                fileManager.write_test(SP);
                return true;
            }
            catch (Exception e)
            {

                throw e;
            }
        }




    }

    class RandomInput
    {
        DateTime start;
        Random gen;
        int range;

        public RandomInput()
        {
            start = new DateTime(1995, 1, 1);
            gen = new Random();
            range = (DateTime.Today - start).Days;
        }

         DateTime RandDT()
        {
            return start.AddDays(gen.Next(range)).AddHours(gen.Next(0, 24)).AddMinutes(gen.Next(0, 60)).AddSeconds(gen.Next(0, 60));
        }

        public int RandOneInt() {
            return gen.Next(0,9);
        }
        public int RandInt()
        {
            return gen.Next(69999,99999);
        }
        public string RandString()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(chars.Select(c => chars[gen.Next(chars.Length)]).Take(8).ToArray());
        }

        public string RandDate() {
            string datetime = RandDatetime();
            string date = datetime.Split(" ")[0];
            return date;
        }

        public string RandTime() {
            string datetime = RandDatetime();
            string time = datetime.Split(" ")[1];
            return time;
        }

        public string RandDatetime() {
            string datetime = RandDT().ToString("yyyy-MM-dd HH:mm:ss");
            return datetime;
        }

        public decimal RandDecimal()
        {
            byte scale = (byte)gen.Next(29);
            bool sign = gen.Next(2) == 1;
            return new decimal(gen.Next(),
                               gen.Next(),
                               gen.Next(),
                               sign,
                               scale);

        }

        public float RandFloat() {
            string beforePoint = gen.Next(0, 9).ToString();//number before decimal point
            string afterPoint = gen.Next(0, 9).ToString();//1st decimal point
                                                        //string secondDP = r.Next(0, 9).ToString();//2nd decimal point
            string combined = beforePoint + "." + afterPoint;
            float decimalNumber = float.Parse(combined);
            return decimalNumber;
        }
    }
}
