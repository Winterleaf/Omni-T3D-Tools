namespace Winterleaf.SharedServices.Interrogator.Containers
{
    public class InitPersistData
    {
        #region StructureType enum

        public enum StructureType
        {
            NotSet = 0,
            TypeVariable,
            TypeEnumeration,
            T3DObject
        }

        #endregion

        private string _mCSharpType = string.Empty;

        private string _mClassName = string.Empty;
        private string _mComment = string.Empty;
        private string _mElementCount = string.Empty;
        private string _mGroup = string.Empty;
        private string _mName = string.Empty;
        private string _mOffsetClass = string.Empty;
        private string _mOffsetVar = string.Empty;
        private StructureType _mStructureType;
        private string _mType = string.Empty;

        public InitPersistData()
        {
            _mStructureType = StructureType.NotSet;
        }

        public InitPersistData(string cn, string n, string t, string ov, string oc, string c, StructureType st)
        {
            _mClassName = cn;
            _mName = n;
            _mType = t;
            _mOffsetVar = ov;
            _mOffsetClass = oc;
            _mComment = c;
            mStructureType = st;
        }

        public string MName
        {
            get { return _mName; }
            set { _mName = value; }
        }

        public string MType
        {
            get { return _mType; }
            set { _mType = value; }
        }

        public string MOffsetVar
        {
            get { return _mOffsetVar; }
            set { _mOffsetVar = value; }
        }

        public string MOffsetClass
        {
            get { return _mOffsetClass; }
            set { _mOffsetClass = value; }
        }

        public string MComment
        {
            get { return _mComment; }
            set { _mComment = value; }
        }

        public string MClassName
        {
            get { return _mClassName; }
            set { _mClassName = value; }
        }

        public string mCSharpType
        {
            get { return _mCSharpType; }
            set { _mCSharpType = value; }
        }

        public StructureType mStructureType
        {
            get { return _mStructureType; }
            set { _mStructureType = value; }
        }

        public string MGroup
        {
            get { return _mGroup; }
            set { _mGroup = value; }
        }

        public string MElementCount
        {
            get { return _mElementCount; }
            set { _mElementCount = value; }
        }

        public override string ToString()
        {
            return "Var Name: '" + MName + "' Type: '" + MType + "' OffsetVar:'" + MOffsetVar + "' OffsetClass:'" + MOffsetClass; // +"' Comment:'" + MComment + "\r\n";
        }
    }
}