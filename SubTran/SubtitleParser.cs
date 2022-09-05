using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace SubTran
{
    public enum SubtitleType
    {
        SUBRIP_SRT,
        MICRODVD_SUB
    }
    public abstract class SubtitleParser
    {
        public abstract List<SubtitleItem> ReadFile(string subtitleFile);
        public virtual void InitSave(string subtitleFile)
        {
            _saveFile = subtitleFile;
            File.WriteAllText(_saveFile, "");
        }
        public abstract void Save(SubtitleItem SubTitlelet);
        public SubtitleType FileType;
        public float FrameRate = 23.976F;
        public Encoding FileEncoding;

        protected string _saveFile;
    }

    public class SubRipParser : SubtitleParser
    {
        int Seq = 1;
        public SubRipParser()
        {
            FileType = SubtitleType.SUBRIP_SRT;
        }

        public override void InitSave(string subtitleFile)
        {
            Seq = 1;
            base.InitSave(subtitleFile);
        }

        public override void Save(SubtitleItem singleSubTitle)
        {
            StringBuilder writeText = new StringBuilder(Seq.ToString());
            writeText.Append(Environment.NewLine);
            writeText.Append(singleSubTitle.FromTime.ToString("HH:mm:ss,fff"));
            writeText.Append(" --> ");
            writeText.Append(singleSubTitle.ToTime.ToString("HH:mm:ss,fff"));
            writeText.Append(Environment.NewLine );
            writeText.Append(singleSubTitle.Subtitle.Replace("</br>", Environment.NewLine));
            writeText.Append(Environment.NewLine );
            writeText.Append( Environment.NewLine);
            File.AppendAllText(_saveFile, writeText.ToString());
            //File.AppendAllText(_saveFile, );
            //File.AppendAllText(_saveFile, );
            //File.AppendAllText(_saveFile, );
            Seq++;           
        }

        public override List<SubtitleItem> ReadFile(string subtitleFile)
        {
            SRTLineType currentLineType;
            SubtitleItem singleSubTitle = null;
            List<SubtitleItem> returnList = new List<SubtitleItem>();

            string[] arrSubtitles = File.ReadAllLines(subtitleFile, FileEncoding);

            currentLineType = SRTLineType.SEQUENCE;

            foreach (string Line in arrSubtitles)
            {
                if (Line.Trim() == "")
                {
                    currentLineType = SRTLineType.BLANK;
                }

                if (currentLineType == SRTLineType.SUBTITLE)
                {
                    if (singleSubTitle.Subtitle!=null)
                        singleSubTitle.Subtitle = singleSubTitle.Subtitle + "</br>" + Line;
                    else
                        singleSubTitle.Subtitle =  Line;
                }
                else if (currentLineType == SRTLineType.SEQUENCE)
                {
                    // New line starts. So create a new object
                    singleSubTitle = new SubtitleItem();
                    currentLineType = SRTLineType.TIMER;
                }
                else if (currentLineType == SRTLineType.TIMER)
                {
                    string[] arTimings = Line.Split(new string[] { "-->" }, StringSplitOptions.None);
                    singleSubTitle.FromTime = DateTime.ParseExact(arTimings[0].Trim(), "HH:mm:ss,fff", CultureInfo.InvariantCulture);
                    singleSubTitle.ToTime = DateTime.ParseExact(arTimings[1].Trim(), "HH:mm:ss,fff", CultureInfo.InvariantCulture);
                    singleSubTitle.Duration = singleSubTitle.ToTime - singleSubTitle.FromTime;
                    currentLineType = SRTLineType.SUBTITLE;
                }
                else if (currentLineType == SRTLineType.BLANK)
                {
                    //End of subtitle - Add to list
                    returnList.Add(singleSubTitle);
                    currentLineType = SRTLineType.SEQUENCE;
                }
            }
            FrameRate = 0;
            return returnList;
        }

    }

    public class MicroDVDSubParser : SubtitleParser
    {
        int Seq = 0;

        public MicroDVDSubParser()
        {
            FileType = SubtitleType.MICRODVD_SUB;
        }


        public override void InitSave(string subtitleFile)
        {
            base.InitSave(subtitleFile);
        }

        public override void Save(SubtitleItem singleSubTitle)
        {



            if ((singleSubTitle.FromTime.Hour != 0 || singleSubTitle.FromTime.Minute != 0 || singleSubTitle.FromTime.Second != 0) && Seq == 0)
            {
                File.AppendAllText(_saveFile, "{1}");
                File.AppendAllText(_saveFile, "{1}");
                File.AppendAllText(_saveFile, FrameRate.ToString() + Environment.NewLine);
            }
            File.AppendAllText(_saveFile, "{" + Math.Round((singleSubTitle.FromTime - DateTime.Today).TotalSeconds * FrameRate) + "}");
            File.AppendAllText(_saveFile, "{" + Math.Round((singleSubTitle.ToTime - DateTime.Today).TotalSeconds * FrameRate) + "}");
            //TODO: Find alternate method for handling </br> in SUB files
            File.AppendAllText(_saveFile, singleSubTitle.Subtitle.Replace("</br>", " ") + Environment.NewLine);
            Seq++;
            
        }

        public override List<SubtitleItem> ReadFile(string subtitleFile)
        {
            List<SubtitleItem> returnList = new List<SubtitleItem>();
            string[] arrSubtitles = File.ReadAllLines(subtitleFile, FileEncoding);
            SubtitleItem singleSubTitle = null;

            foreach (string Line in arrSubtitles)
            {
                string[] subparts= Line.Split(new string[] { "{", "}" }, StringSplitOptions.None);
                int startFrame =int.Parse( subparts[1]);
                int endFrame = int.Parse(subparts [3]);
                string subTitle = subparts[4];

                if (startFrame == 1 && endFrame == 1)
                    FrameRate = float.Parse(subTitle);
                
                singleSubTitle = new SubtitleItem();
                singleSubTitle.FromTime = ConvertSecondsToTime (startFrame / FrameRate )  ;
                singleSubTitle.ToTime = ConvertSecondsToTime(endFrame / FrameRate);
                singleSubTitle.Duration = singleSubTitle.ToTime - singleSubTitle.FromTime;
                singleSubTitle.Subtitle = subTitle;
                returnList.Add(singleSubTitle);
                
            }

            return returnList;
        }

        public DateTime ConvertSecondsToTime(float Seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(Seconds);

            string answer = string.Format("{0:D2}:{1:D2}:{2:D2},{3:D3}",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds,
                                    t.Milliseconds);

            return DateTime.ParseExact(answer, "HH:mm:ss,fff", CultureInfo.InvariantCulture);
            //DateTime.ParseExact(arTimings[1].Trim(), "HH:mm:ss,fff", CultureInfo.InvariantCulture);
        }
    }


    public class SubtitleItem
    {
        public DateTime FromTime;
        public DateTime ToTime;
        public TimeSpan Duration;
        public string Subtitle;
    }
}
