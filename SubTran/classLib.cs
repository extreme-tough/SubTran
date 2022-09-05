using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;

namespace SubTran
{
    public class SRTLine
    {
        public string Line;
        public SRTLineType LineType;
    }

    public enum SRTLineType
    {
        SEQUENCE,
        TIMER,
        SUBTITLE,
        BLANK
    }

    public static class Global
    {
        public static Logger Logs = LogManager.GetCurrentClassLogger();
    }
}
