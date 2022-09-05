using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Web;
using System.Threading;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using Languages;
using RavSoft.GoogleTranslator;
using DialogueMaster.Babel;

namespace SubTran
{
    

    
    public partial class mainForm : BaseForm
    {
        static int ArrayPointer;

        [ThreadStatic]
        static Translator t;
        [ThreadStatic]
        static int currentIndex;

        
        string[] arrSubtitles;
        SubtitleParser parserEngine = null;
        string SourceLang, TargetLang;
        string currentFile;
        Encoding currentEncoding;
        public mainForm()
        {
            Global.Logs.Debug("mainForm Init start");

            InitializeComponent();
            InitializeLanguages();

            Global.Logs.Debug("mainForm Init end");
        }

        private void InitializeLanguages()
        {
            Global.Logs.Debug("InitializeLanguages start");

            foreach(string LanguageName in Languages.Languages._languageModeMap.Keys)
            {
                //DevComponents.Editors.ComboItem langItem;
                //langItem = new DevComponents.Editors.ComboItem();
                Global.Logs.Debug("Adding language " + LanguageName);

                _comboFrom.Items.Add(LanguageName);
                //_comboFrom.Items.Add(langItem);
                _comboTo.Items.Add(LanguageName);
//                langItem.Text = LanguageName;
            }

            Global.Logs.Debug("InitializeLanguages end");
        }


        private void OpenButton_Click(object sender, EventArgs e)
        {
            Global.Logs.Debug("Open button click start");

            if (SetupOpenDialog() == DialogResult.Cancel) 
            {
                Global.Logs.Debug("Cancel clicked");
                return;
            }


            if (openFileDialog.FileName != "")
            {

                if (",.3gp,.3g2,.asf,.asx,.avi,.flv,.mov,.mp4,.mpg,.rm,.swf,.vob,.wmv,".IndexOf("," + Path.GetExtension(openFileDialog.FileName).ToLower() + ",") > 0)
                {
                    if (File.Exists(Path.GetDirectoryName(openFileDialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) + ".srt"))
                    {
                        Global.Logs.Debug("SRT file exists");
                        parserEngine = new SubRipParser();
                        currentFile = Path.GetDirectoryName(openFileDialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) + ".srt";
                        Global.Logs.Debug("Current file is " + currentFile);
                    }
                    else if (File.Exists(Path.GetDirectoryName(openFileDialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) + ".sub"))
                    {
                        Global.Logs.Debug("SUB file exists " );
                        parserEngine = new MicroDVDSubParser();
                        currentFile = Path.GetDirectoryName(openFileDialog.FileName) + "\\" + Path.GetFileNameWithoutExtension(openFileDialog.FileName) + ".sub";
                        Global.Logs.Debug("Current file is " + currentFile);
                    }
                    else
                    {
                        Global.Logs.Debug("No subtitle found");
                        MessageBox.Show("Cannot find any supported subtitle file for this video", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                }
                else
                    currentFile = openFileDialog.FileName;

                try
                {
                    Global.Logs.Debug("Trying to get the current encoding");
                    currentEncoding = EncodingLib.GetEncodingFromFile(currentFile);
                    Global.Logs.Debug("Encoding of current file is identified as " + currentEncoding.EncodingName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error reading encoding information from the file : " + ex.Message,"ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    Global.Logs.Debug("Error reading encoding info. Load subtitle aborted");
                    return;
                }
                try
                {
                    Global.Logs.Debug("Trying LoadSubtitles()");
                    LoadSubtitles();
                }
                catch (Exception ex)
                {
                    Global.Logs.Debug("LoadSubtitles failed : " + ex.Message );
                    MessageBox.Show("Error reading subtitles from the file : " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                progressBarX.Text = "Detecting language...";
                progressBarX.Refresh();
                Global.Logs.Debug("Trying DetectLanguage()");
                DetectLanguage();
                progressBarX.Text = "";
                buttonSave.Enabled = true;
                buttonSaveAs.Enabled = true;
            }
            Global.Logs.Debug("Open button click end");
        }



        private void DetectLanguage()
        {
            Global.Logs.Debug("Detect language start");

            DialogueMaster.Classification.ICategoryList result;
            IBabelModel m_Model = BabelModel._AllModel;;
            //DialogueMaster
            Global.Logs.Debug("Language model loaded");

            string[] arLangauges = new string[lvwSubtitles.Items.Count];
            foreach (ListViewItem lvwItem in lvwSubtitles.Items)
            {
                result = m_Model.ClassifyText(lvwItem.SubItems[4].Text);
                string langCode = result.ToString();

                for (int index = 0; index < result.Count; index++)
                {
                    if (this._comboFrom.Items.Contains(Languages.Languages.GetLanguageCulture(result[index].Name).DisplayName))
                    {
                        arLangauges[lvwItem.Index] = Languages.Languages.GetLanguageCulture(result[index].Name).DisplayName;
                        break;
                    }
                    Application.DoEvents();
                }
                Application.DoEvents();
            }

            var i = from numbers in arLangauges
                    group numbers by numbers into grouped
                    select new { occuring = grouped.Key, Freq = grouped.Count()};
            int maxCount=0;
            string maxLang="";
            for (int index = 0; index < i.ToList().Count; index++)
            {
                Global.Logs.Debug("Possible langugage identification as : " + i.ToList()[index].occuring);
                if (i.ToList()[index].Freq > maxCount)
                {
                    maxCount = i.ToList()[index].Freq;
                    maxLang = i.ToList()[index].occuring;
                }
                Application.DoEvents();
            }
            if (maxLang != "")
            {
                Global.Logs.Debug("Final langauge detection as " + maxLang);
                _comboFrom.SelectedItem = maxLang;
            }

            Global.Logs.Debug("Detect language end");
            
            return;
        }

        private void LoadSubtitles()
        {
            Global.Logs.Debug("Load Subtitles start");

            List<SubtitleItem> subtitleList = new List<SubtitleItem>();
            ListViewItem lvwItem;
            int Seq;


            switch (Path.GetExtension(currentFile).ToUpper())
            {
                case ".SRT":
                    parserEngine = new SubRipParser();
                    break;
                case ".SUB":
                    parserEngine = new MicroDVDSubParser();
                    break;   
                default:
                    MessageBox.Show("This subtitle format is not supported", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
            }
            

            parserEngine.FileEncoding = currentEncoding;
            try
            {
                subtitleList = parserEngine.ReadFile(currentFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error parsing subtitles from the file : " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            arrSubtitles = new string[subtitleList.Count];

            Seq = 0;
            progressBarX.Minimum = 0;
            progressBarX.Maximum = subtitleList.Count;
            progressBarX.Text = "Reading subtitles...";
            lvwSubtitles.Items.Clear();
            lvwSubtitles.BeginUpdate();
            foreach (SubtitleItem subTitle in subtitleList)
            {
                lvwItem = lvwSubtitles.Items.Add(new ListViewItem((Seq+1).ToString()));
                lvwItem.SubItems.Add(subTitle.FromTime.ToString("HH:mm:ss,fff"));
                lvwItem.SubItems.Add(subTitle.ToTime.ToString("HH:mm:ss,fff"));
                lvwItem.SubItems.Add(subTitle.Duration.ToString());
                lvwItem.SubItems.Add(subTitle.Subtitle);

                arrSubtitles[Seq] = subTitle.Subtitle;
                progressBarX.Value = Seq;
                Seq++;
                Application.DoEvents();
            }
            progressBarX.Value = 0;
            FrameRateBox.Text = parserEngine.FrameRate.ToString();
            lvwSubtitles.EndUpdate();
            lvwSubtitles.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwSubtitles.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwSubtitles.Columns[2].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwSubtitles.Columns[3].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            lvwSubtitles.Columns[4].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);

            Global.Logs.Debug("Load Subtitles end");
        }

        private DialogResult SetupOpenDialog()
        {
            openFileDialog.FileName = "";
            return openFileDialog.ShowDialog();
        }

        private void RedimArray(ref string[] inArray,int size)
        {
            string[] temp = new string[size + 1];
            if (inArray != null)
                Array.Copy(inArray, temp, Math.Min(inArray.Length, temp.Length));
            inArray = temp;
        }

        private void TranslateButton_Click(object sender, EventArgs e)
        {
            string Line;
            SourceLang = _comboFrom.Items[_comboFrom.SelectedIndex].ToString();
            TargetLang = _comboTo.Items[_comboTo.SelectedIndex].ToString();
            if (lvwSubtitles.Items.Count == 0)
            {
                MessageBox.Show("Nothing to translate","Translate",MessageBoxButtons.OK,MessageBoxIcon.Stop);
                return;
            }

            ThreadStart job = new ThreadStart(ThreadTranslate);
            Thread[] threads = new Thread[25];
            for (int i = 0; i < 25; i++)
            {
                threads[i] = new Thread(job);
            }

            progressBarX.Minimum = 0;
            progressBarX.Maximum = lvwSubtitles.Items.Count;

            progressBarX.Text = "Translating from " + SourceLang + " to " + TargetLang + "...";
            ArrayPointer = 0;
            ButtonOpen.Enabled = false;
            buttonSave.Enabled = false;
            buttonSaveAs.Enabled = false;

            for (int i = 0; i < 25; i++)
            {
                threads[i].Start();
            }
            bool threadRunning;
            while (true)
            {
                threadRunning = false;
                for (int i = 0; i < 25; i++)
                {
                    if (threads[i].IsAlive)
                    {
                        threadRunning = true;
                        Application.DoEvents();
                        break;
                    }
                }
                if (threadRunning)
                {
                    Application.DoEvents();
                    continue;
                }
                else
                    break;                
            }
            progressBarX.Text = "";
            progressBarX.Value = 0;
            MessageBox.Show("Translation completd","Translate",MessageBoxButtons.OK,MessageBoxIcon.Information);
            ButtonOpen.Enabled = true;
            buttonSave.Enabled = true;
            buttonSaveAs.Enabled = true;

            _comboFrom.SelectedIndex = _comboTo.SelectedIndex;
        }

        private void ThreadTranslate()
        {
            
            string TranslatedLine;
            while (ArrayPointer < lvwSubtitles.Items.Count)
            {
                currentIndex = ArrayPointer;
                ArrayPointer++;

                t = new Translator();
                t.SourceLanguage = SourceLang;
                t.TargetLanguage = TargetLang;
                t.SourceText = arrSubtitles[currentIndex];

                try
                {
                    t.Translate();
                }
                catch (Exception ex)
                {
                }

                TranslatedLine = HttpUtility.HtmlDecode(t.Translation);

                SetControlPropertyValue(lvwSubtitles, currentIndex, 4, TranslatedLine); //
                SetProgressBarValue(progressBarX, currentIndex);
                
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            _comboFrom.Text = "English";
            _comboTo.Text = "French";

            lblProduct.Text = this.ProductName + " " + Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor;                        
        }


        delegate void SetControlValueCallback(ListView oControl, int Index,int subIndex,  string propValue);
        private void SetControlPropertyValue(ListView oControl, int Index, int subIndex, string propValue)
        {
            if (oControl.InvokeRequired)
            {
                SetControlValueCallback d = new SetControlValueCallback(SetControlPropertyValue);                
                oControl.Invoke(d, new object[] { oControl,Index, subIndex,  propValue });
            }
            else
            {
                oControl.Items[Index].SubItems[subIndex].Text = propValue;
            }
        }

        delegate void SetProgressBarCallback(DevComponents.DotNetBar.Controls.ProgressBarX oControl,int propValue);
        private void SetProgressBarValue(DevComponents.DotNetBar.Controls.ProgressBarX oControl, int propValue)
        {
            if (oControl.InvokeRequired)
            {
                SetProgressBarCallback d = new SetProgressBarCallback(SetProgressBarValue);
                oControl.Invoke(d, new object[] { oControl, propValue });
            }
            else
            {
                if (oControl.Value < propValue)
                    oControl.Value = propValue;
            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            panelAbout.Visible = false;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("mailto:info@cuebiztech.com");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.cuebiztech.com");
        }

        private void aboutButton_Click(object sender, EventArgs e)
        {
            panelAbout.Left = (lvwSubtitles.ClientSize.Width - panelAbout.Width) / 2;
            panelAbout.Top = lvwSubtitles.Top + (lvwSubtitles.ClientSize.Height - panelAbout.Height) / 2;
            panelAbout.Visible = true;
            webBrowser1.Navigate("about:blank");
            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
            }
            webBrowser1.Document.Write(global::SubTran.Properties.Resources.PaypalDonateHTML);
        }

        private void lvwSubtitles_Resize(object sender, EventArgs e)
        {
            panelAbout.Left = (lvwSubtitles.ClientSize.Width - panelAbout.Width) / 2;
            panelAbout.Top = lvwSubtitles.Top + (lvwSubtitles.ClientSize.Height - panelAbout.Height) / 2;
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Application.Exit();
        }

        private void SaveFile(string FileName,SubtitleParser engine)
        {
            SubtitleItem singleSubTitle;
            progressBarX.Minimum = 0;
            progressBarX.Maximum = lvwSubtitles.Items.Count;
            int Seq = 0;
            progressBarX.Text = "Saving. Please wait...";
            this.Cursor = Cursors.WaitCursor;
            engine.InitSave(FileName);
            foreach (ListViewItem lvwItem in lvwSubtitles.Items)
            {
                singleSubTitle = new SubtitleItem();
                singleSubTitle.FromTime = DateTime.ParseExact(lvwItem.SubItems[1].Text, "HH:mm:ss,fff", CultureInfo.InvariantCulture);
                singleSubTitle.ToTime = DateTime.ParseExact(lvwItem.SubItems[2].Text, "HH:mm:ss,fff", CultureInfo.InvariantCulture);
                singleSubTitle.Subtitle = lvwItem.SubItems[4].Text;
                try
                {
                    engine.Save(singleSubTitle);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during saving : " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                } 
                progressBarX.Value = Seq;
                Seq++;
            }
            
            this.Cursor = Cursors.Default;
            progressBarX.Text = "";
            progressBarX.Value = 0;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            SaveFile(currentFile, parserEngine);
        }

        private void buttonSaveAs_Click(object sender, EventArgs e)
        {

            SubtitleParser SaveAsparser=null;

            saveFileDialog.Filter = "Srt subtitles (*.srt)|*.srt|Sub subtitles (*.sub)|*.sub|All files(*.*)|*.*";
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(currentFile);
            if (saveFileDialog.ShowDialog()==DialogResult.Cancel) return;

            switch (Path.GetExtension(saveFileDialog.FileName).ToUpper())
            {
                case ".SRT":
                    SaveAsparser = new SubRipParser();
                    break;
                case ".SUB":
                    if (float.Parse(FrameRateBox.Text.ToString() + "0") == 0)
                    {
                        MessageBox.Show("To save as a SUB file, you need to set the frame rate in options section in the tool bar", "Save As", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        return;
                    }
                    SaveAsparser = new MicroDVDSubParser();
                    SaveAsparser.FrameRate = float.Parse(FrameRateBox.Text);
                    break;
            }

            SaveFile(saveFileDialog.FileName, SaveAsparser);
            currentFile = saveFileDialog.FileName;
        }


        private void picDonate_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.Forms[0].InvokeMember("submit");
        }

    }
}
