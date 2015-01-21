using System;
using System.Reflection;
using System.Text;

namespace Winterleaf.SharedServices.Interrogator.Logger
{
    public class Logger
    {
        public delegate void NewEventRecorded(String Text);

        public delegate void ProgressChange(double percent, string Text);

        private readonly NewEventRecorded _mNewEventCallBack;
        private readonly ProgressChange _mProgressChange;
        private readonly ProgressChange _mSubProgressChange;
        public StringBuilder _mLog = new StringBuilder();
        private string mSection = "";
        private string mSubSection = "";

        public Logger(ref NewEventRecorded cb, ref ProgressChange pc, ref ProgressChange spc)
        {
            _mNewEventCallBack = cb;
            _mProgressChange = pc;
            _mSubProgressChange = spc;
        }

        public void onProgressChange(double percent, String Text = "")
        {
            if (_mProgressChange != null)
                _mProgressChange(percent, Text);
        }

        public void onProgressSubChange(double percent, string Text = "")
        {
            if (_mSubProgressChange != null)
                _mSubProgressChange(percent, Text);
        }

        private void onNewEvent(string text)
        {
            _mLog.Append(text);
            if (_mNewEventCallBack != null)
                _mNewEventCallBack(text);
        }

        internal void LogEvent(string system, string subsystem, string message, bool isheader = false)
        {
            const int maxsize = 30;
            string blanks = "                                                                                       ".Substring(0, maxsize);
            system = (system + "                                                                          ").Substring(0, maxsize);
            subsystem = (subsystem + "                                                                          ").Substring(0, maxsize);
            if (isheader)
                onNewEvent(DateTime.Now.ToString("MM/dd/yyyy mm:hh:ss ") + system + " " + subsystem + " " + message + "\r\n");
            else
                onNewEvent(DateTime.Now.ToString("MM/dd/yyyy mm:hh:ss ") + blanks + " " + blanks + " " + message + "\r\n");
        }

        internal void SectionStart(string Section)
        {
            mSection = Section;
            mSubSection = "";
            LogEvent(Section, "", "(START)", true);
        }

        internal void SectionStart(string Section, string subsection)
        {
            mSection = Section;
            mSubSection = subsection;
            LogEvent(mSection, "", "(START)", true);
            LogEvent("", mSubSection, "(START)", true);
        }

        internal void SubSectionStart(string subsection)
        {
            if (mSubSection.Trim() != "")
                LogEvent("", mSubSection, "(END)", true);
            mSubSection = subsection;
            LogEvent("", mSubSection, "(START)", true);
        }

        internal void SubSectionEnd()
        {
            LogEvent("", mSubSection, "(END)", true);
            mSubSection = "";
        }

        internal void SectionEnd()
        {
            LogEvent("", mSubSection, "(END)", true);
            LogEvent(mSection, "", "(END)", true);
            mSubSection = "";
            mSection = "";
        }

        internal void NewConfigEvent(EventStatus status, string filename, string message = "")
        {
            switch (status)
                {
                    case EventStatus.START:
                        LogEvent(mSection, mSubSection, "(" + status + ") Parsing Configuration File:'" + filename + "'.");
                        break;
                    case EventStatus.END:
                        LogEvent(mSection, mSubSection, "(" + status + ") Parsing Configuration File:'" + filename + "'.");
                        break;
                    case EventStatus.ERROR:
                        LogEvent(mSection, mSubSection, "(" + status + ") " + message);
                        break;
                    case EventStatus.DETAIL:
                        LogEvent(mSection, mSubSection, "(" + status + ") " + message);
                        break;
                }
        }

        internal void NewErrorEvent(string filename, string msg)
        {
            NewConfigEvent(EventStatus.ERROR, filename, "                  " + msg);
        }

        internal void NewEvent(string filename, string msg)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  " + msg);
        }

        internal void Stage1Log(MemberInfo callingMethod, string check, string message, string filename, int linenum)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  Message : " + check + " " + message + " @ Line (" + linenum + ") ");
        }

        internal void Stage11Log(string filename, string message)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  (InitPersist Found) : " + message);
        }

        internal void Stage12Log(string filename, string message)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  (C++ Enumeration Found) : " + message);
        }

        internal void Stage13Log(string filename, string message)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  (CONSOLETYPE Found) : " + message);
        }

        internal void Stage14Log(string filename, string message)
        {
            NewConfigEvent(EventStatus.DETAIL, filename, "                  (CLEANUP) : " + message);
        }

        #region Nested type: EventStatus

        internal enum EventStatus
        {
            START,
            END,
            SUCCESS,
            FAIL,
            ERROR,
            DETAIL
        }

        #endregion
    }
}