namespace Winterleaf.SharedServices.Interrogator.Configuration
{
    public class CPPObjectSerializeDeserializeDef
    {
        public string deserializestring;
        public bool isobject;
        public string serializestring;

        public CPPObjectSerializeDeserializeDef(string a, string b, bool i)
        {
            deserializestring = a;
            serializestring = b;
            isobject = i;
        }
    }
}