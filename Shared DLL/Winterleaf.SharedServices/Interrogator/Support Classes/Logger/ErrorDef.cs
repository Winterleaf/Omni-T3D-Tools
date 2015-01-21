namespace Winterleaf.SharedServices.Interrogator.Logger
{
    public class ErrorDef
    {
        private string _mCaller;
        private string _mCheck;
        private string _mMessage;
        private string _mfilename;
        private int _mlinenumber;

        public ErrorDef(string c, string ch, string msg, string fn, int ln)
        {
            _mCaller = c;
            _mCheck = ch;
            _mMessage = msg;
            _mfilename = fn;
            _mlinenumber = ln;
        }

        public string Parser_Routine
        {
            get { return _mCaller; }
            set { _mCaller = value; }
        }

        public string Validation
        {
            get { return _mCheck; }
            set { _mCheck = value; }
        }

        public string Message
        {
            get { return _mMessage; }
            set { _mMessage = value; }
        }

        public string Mfilename
        {
            get { return _mfilename; }
            set { _mfilename = value; }
        }

        public int Mlinenumber
        {
            get { return _mlinenumber; }
            set { _mlinenumber = value; }
        }
    }
}