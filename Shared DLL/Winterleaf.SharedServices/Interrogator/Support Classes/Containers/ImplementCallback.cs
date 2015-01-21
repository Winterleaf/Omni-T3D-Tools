namespace Winterleaf.SharedServices.Interrogator.Containers
{
    public class ImplementCallback
    {
        private string _mClassname = string.Empty;
        private string _mComments = string.Empty;
        private string _mFunction = string.Empty;
        private string _mParams = string.Empty;
        private string _mReturnType = string.Empty;

        public ImplementCallback()
        {
        }

        public ImplementCallback(string c, string f, string r, string p, string cm)
        {
            _mClassname = c;
            _mFunction = f;
            _mReturnType = r;
            _mParams = p;
            _mComments = cm;
        }

        public string mClassname
        {
            get { return _mClassname; }
            set { _mClassname = value; }
        }

        public string mFunction
        {
            get { return _mFunction; }
            set { _mFunction = value; }
        }

        public string mReturnType
        {
            get { return _mReturnType; }
            set { _mReturnType = value; }
        }

        public string mParams
        {
            get { return _mParams; }
            set { _mParams = value; }
        }

        public string mComments
        {
            get { return _mComments; }
            set { _mComments = value; }
        }

        public void trim()
        {
            _mClassname = mClassname.Trim();
            _mFunction = mFunction.Trim();
            _mReturnType = mReturnType.Trim();
            _mParams = mParams.Trim();
            mComments = mComments.Trim();
            mComments = mComments.Replace(@"""", "").Replace(@"\n", "");
            mComments = mComments.Replace("\r\n", "").Replace("&", " and ");
        }
    }
}