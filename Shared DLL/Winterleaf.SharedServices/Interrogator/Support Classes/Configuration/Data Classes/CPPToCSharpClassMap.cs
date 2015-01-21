using Winterleaf.SharedServices.Interrogator.Configuration;

namespace SharedServices.Interrogator.Support_Classes.Configuration.Data_Classes
{
    public class CPPToCSharpClassMap
    {
        public string cstype = "";
        public CPPEntityType itype;

        public CPPToCSharpClassMap(string a, CPPEntityType i)
        {
            cstype = a;
            itype = i;
        }
    }
}