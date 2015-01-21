#region

using System;
using System.Linq;

#endregion

namespace Winterleaf.SharedServices.Interrogator.Containers
{
    public class Externdata
    {
        public Externdata(string filename, string n, string r, string o, string p, string b, string ht, string d)
        {
            m_name = n;
            if ((r == "SimTime") || (r == "SimObjectId"))
                r = "U32";
            else if (r == "StringTableEntry")
                r = "const char*";

            m_returntype = r;
            m_objecttype = o;
            m_filename = filename;

            string newparameters = string.Empty;
            string[] parameters;
            parameters = p.Trim().ToLower() == "void" ? new string[0] : p.Split(',');
            int c = 0;
            foreach (string rparameter in parameters)
                {
                if (rparameter.Trim().Length <= 0)
                    continue;
                string parameter = rparameter.Trim();

                if (parameter.Trim().Length <= 0)
                    continue;
                parameter = getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                if (i == -1)
                    parameter = "";
                if (parameter.Trim().Length <= 0)
                    continue;
                string ptype = "";
                string pname = "";
                try
                    {
                    ptype = parameter.Substring(0, i).Trim();
                    if ((ptype == "SimTime") || (ptype == "SimObjectId"))
                        ptype = "U32";
                    else if (ptype == "StringTableEntry")
                        ptype = "const char*";

                    pname = parameter.Substring(i).Trim();
                    }
                catch (Exception err)
                    {
                    }

                if (newparameters.Length > 0)
                    newparameters += ", ";
                newparameters += ptype.Trim() + " " + pname.Trim();
                }

            m_params = newparameters; // p;

            m_body = b;
            m_helptext = ht;
            m_minparams = -1;
            m_maxparams = -1;
            m_defaults = d;

            if (m_defaults != "")
                Console.WriteLine("Defaults: " + m_defaults);
        }

        public Externdata(string filename, string n, string r, string o, string p, string b, string ht, int minp, int maxp, string d)
        {
            m_name = n;
            if ((r == "SimTime") || (r == "SimObjectId"))
                r = "U32";
            else if (r == "StringTableEntry")
                r = "const char*";
            m_returntype = r;
            m_objecttype = o;
            m_filename = filename;
            m_defaults = d;

            string newparameters = string.Empty;
            string[] parameters;
            parameters = p.Trim().ToLower() == "void" ? new string[0] : p.Split(',');
            int c = 0;
            foreach (string rparameter in parameters)
                {
                if (rparameter.Trim().Length <= 0)
                    continue;
                string parameter = rparameter.Trim();

                if (parameter.Trim().Length <= 0)
                    continue;
                parameter = getridofdoublespace(parameter);
                int i = parameter.Trim().LastIndexOf(' ');
                if (i == -1)
                    parameter = "";
                if (parameter.Trim().Length <= 0)
                    continue;
                string ptype = "";
                string pname = "";
                try
                    {
                    ptype = parameter.Substring(0, i).Trim();
                    if ((ptype == "SimTime") || (ptype == "SimObjectId"))
                        ptype = "U32";
                    else if (ptype == "StringTableEntry")
                        ptype = "const char*";

                    pname = parameter.Substring(i).Trim();
                    }
                catch (Exception err)
                    {
                    }

                if (newparameters.Length > 0)
                    newparameters += ", ";
                newparameters += ptype.Trim() + " " + pname.Trim();
                }
            m_params = newparameters; // p;
            //m_params = p;
            m_body = b;
            m_helptext = ht;
            m_minparams = minp;
            m_maxparams = maxp;

            if (m_defaults != "")
                Console.WriteLine("Defaults: " + m_defaults);
        }

        public string m_filename { get; set; }
        public string m_name { get; set; }
        public string m_returntype { get; set; }
        public string m_objecttype { get; set; }
        public string m_params { get; set; }
        public string m_body { get; set; }
        public string m_helptext { get; set; }
        public int m_minparams { get; set; }
        public int m_maxparams { get; set; }
        public string m_defaults { get; set; }

        private string getridofdoublespace(string sparams)
        {
            sparams = sparams.Trim();
            string newstring = "";
            char lastchar = ' ';
            for (int i = 0; i < sparams.Count(); i++)
                {
                if (sparams[i] != ' ')
                    newstring += sparams[i];
                else if ((sparams[i] == ' ') && (lastchar != ' '))
                    {
                    if (sparams[i + 1] != '*')
                        newstring += sparams[i];
                    }
                if (sparams[i] == '*')
                    newstring += ' ';
                lastchar = sparams[i];
                }

            sparams = newstring;
            newstring = "";
            foreach (char v in sparams)
                {
                if (v == '*')
                    {
                    newstring += '*';
                    newstring += ' ';
                    }
                else
                    newstring += v;
                }
            return newstring;
        }
    }
}