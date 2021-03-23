using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace Triggernometry.CustomControls
{

    public partial class ExpressionTextBox : UserControl
    {

        public enum SupportedExpressionTypeEnum
        {
            String,
            Numeric,
            Regex
        }
        public AutoCompleteStringCollection autoCompleteCollectionModel;

        public bool ReadOnly
        {
            get
            {
                return textBox1.ReadOnly;
            }
            set
            {
                textBox1.ReadOnly = value;
            }
        }

        public string Expression
        {
            get
            {
                return textBox1.Text;
            }

            set
            {
                textBox1.Text = value;
            }
        }

        public override string Text
        {
            get
            {
                return Expression;
            }
            set
            {
                Expression = value;
            }
        }

        private SupportedExpressionTypeEnum _ExpressionType;
        public SupportedExpressionTypeEnum ExpressionType
        { 
            get
            {
                return _ExpressionType;
            }
            set
            {
                if (value != _ExpressionType)
                {
                    _ExpressionType = value;
                    ResetTooltip();
                }
            }
        }

        private void ResetTooltip()
        {
            switch (ExpressionType)
            {
                case SupportedExpressionTypeEnum.Numeric:
                    string temp = I18n.Translate("internal/ExpressionTextBox/numeric1", "This field supports numeric expressions; references to named regular expression groups will be expanded, after which the result will be evaluated as a mathematic expression.");
                    temp += Environment.NewLine;
                    temp += I18n.Translate("internal/ExpressionTextBox/numeric2", "The color of the field will also change depending on whether the numeric expression appears to be valid (green) or not (red).");
                    toolTip1.SetToolTip(panel1, temp);
                    break;
                case SupportedExpressionTypeEnum.String:
                    toolTip1.SetToolTip(panel1, I18n.Translate("internal/ExpressionTextBox/string", "This field supports string expressions; references to named regular expression groups will be expanded."));
                    break;
                case SupportedExpressionTypeEnum.Regex:
                    toolTip1.SetToolTip(panel1, I18n.Translate("internal/RegexTextBox", "This field supports regular expressions; the color of the field will change depending on whether the regular expression is valid (green) or not (red)."));
                    break;
            }
            UpdateBackground();
        }

        private Context ctx;

        public delegate void EnterDelegate();
        public event EnterDelegate OnEnterKeyHit;

        public ExpressionTextBox()
        {
            autoCompleteLastRefreshTime = DateTime.Now;
            InitializeComponent();
            CreateAutocompleteListBox();
            ctx = new Context();
            ctx.testmode = true;
            ResetTooltip();
            textBox1.TextChanged += TextBox1_TextChanged;
            textBox1.KeyDown += TextBox1_KeyDown;
        }
        public AutoCompleteStringCollection getAutoComplete(string text) {
            return autoCompleteCollectionModel;
        }
        public void initAutoComplete(string text)
        {
            autoCompleteCollectionModel = new AutoCompleteStringCollection();
        }
        private void TextBox1_KeyDown(object sender, KeyEventArgs e)
        {
            // These keys show auto-complete selector
            var activatorCodes = new List<Keys> { Keys.Up, Keys.Down };
            if (activatorCodes.Contains(e.KeyCode))
            {
                SwitchToAutoCompleteList();
            }
            if (e.KeyCode == Keys.Enter)
            {
                if (autoListBox.Focused)
                {
                    e.SuppressKeyPress = true;
                }
                else { 
                    if (OnEnterKeyHit != null)
                    {
                        OnEnterKeyHit();
                    }
                }
            }
        }

        private void UpdateBackground()
        {
            if (ExpressionType == SupportedExpressionTypeEnum.Numeric)
            {
                if (textBox1.Text.Length == 0)
                {
                    textBox1.BackColor = SystemColors.Window;
                    return;
                }
                try
                {
                    ctx.EvaluateNumericExpression(null, null, textBox1.Text);
                    textBox1.BackColor = Color.FromArgb(200, 255, 200);
                }
                catch (Exception)
                {
                    textBox1.BackColor = Color.FromArgb(255, 200, 200);
                }
            }
            else if (ExpressionType == SupportedExpressionTypeEnum.String)
            {
                if (textBox1.BackColor != SystemColors.Window)
                {
                    textBox1.BackColor = SystemColors.Window;
                }
            }
            else if (ExpressionType == SupportedExpressionTypeEnum.Regex)
            {
                if (textBox1.Text.Length == 0)
                {
                    textBox1.BackColor = SystemColors.Window;
                    return;
                }
                try
                {
                    Regex rex = new Regex(textBox1.Text);
                    textBox1.BackColor = Color.FromArgb(200, 255, 200);
                }
                catch (Exception)
                {
                    textBox1.BackColor = Color.FromArgb(255, 200, 200);
                }
            }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Focused)
            {
                try
                {

                    showAutoCompleteList();

                }
                
                catch (Exception ex) {
                
                }
            }
            UpdateBackground();
        }

        private void panel1_Click(object sender, EventArgs e)
        {
            ToggleExpand();
        }

        private void panel1_DoubleClick(object sender, EventArgs e)
        {
            ToggleExpand();
        }

        private void ToggleExpand()
        {
            if (textBox1.Multiline == true)
            {
                textBox1.Multiline = false;
                textBox1.MinimumSize = new Size(textBox1.MinimumSize.Width, 0);
                textBox1.ScrollBars = ScrollBars.None;
                Image tmp = panel1.BackgroundImage;
                panel1.BackgroundImage = panel2.BackgroundImage;
                panel2.BackgroundImage = tmp;
            }
            else
            {
                textBox1.Multiline = true;
                textBox1.MinimumSize = new Size(textBox1.MinimumSize.Width, 100);
                textBox1.ScrollBars = ScrollBars.Both;
                Image tmp = panel1.BackgroundImage;
                panel1.BackgroundImage = panel2.BackgroundImage;
                panel2.BackgroundImage = tmp;
            }
        }




        private ListBox autoListBox;
        private void CreateAutocompleteListBox()
        {
            this.autoListBox = new ListBox()
            {
                Left = GetCursorCoordinates(this.textBox1,0).X,
                Top = GetCursorCoordinates(this.textBox1,0).Y,
                MinimumSize = new Size(0, 0),
                Height = 0,
                Width = 200,
                

        };
        //this.textBox1.Top + this.textBox1.Height,
            this.autoListBox.Click += autoListBox_Click;
            this.autoListBox.KeyDown += autoListBox_KeyDown;
            this.autoListBox.Visible = false;
            this.Controls.Add(this.autoListBox);
        }

        private void autoListBox_Click(object sender, EventArgs e)
        {
            AutocompleteFinished();
        }

        private void autoListBox_KeyDown(object sender, KeyEventArgs e)
        {
            var finishCodes = new List<Keys> { Keys.Return, Keys.Space };
            if (finishCodes.Contains(e.KeyCode))
            {
                AutocompleteFinished();
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private string GetLastWord(TextBox textBox)
        {
            string txt = textBox.Text.Substring(0, Math.Max(0,textBox.SelectionStart));
            if (isRegexTextBox())
            {
                char[] seperators = new char[] { ')', ':' };
                string[] splits = txt.Split(seperators);
                return (splits.LastOrDefault() ?? "");
            }
            else
            {
                char[] seperators = new char[] { '(', ')', '{', '}', ',',':','+','*','-','/','%' };
                string[] splits = txt.Split(seperators);
                return (splits.LastOrDefault() ?? "").Trim();
            }
        }
        string[] autoCompleteCommon = new string[] {
            

            "_duration","_event","_incombat","_since","_sincems",
            "_triggerid","_triggername","_timestamp","timestampms",
            "_systemtime","_systemtimems","_zone","_response",
            "jsonresponse[path]","_screenwidth","_screenheight",
            "_lastencounter","_activeencounter","_env[varname]",

            "_ffxivparty","_ffxiventity","_ffxivplayer","_ffxivtime","_ffxivpartyorder",
            "_ffxivprocid","_ffxivprocname","_ffxivmemory[address, offset, length]",

            "_textaura[id]","_imageaura[id]",

            "pi","pi2","pi05","pi025","pi0125",

            "abs(x)","cos(x)","sin(x)","tan(x)","arctan(x)","arctan2(x,y)",
            "arccos(x)","arcsin(x)","distance(x1,y1,x2,y2)",
            "radtodeg(x)","degtorad(x)","max(x1,x2,x3,...)","min(x1,x2,x3,...)",
            "random(x,y)","sqrt(x)","root(x,y)","pow(x,y)","rem(x,y)","exp(x)",
            "log(x)","round(x)","round(x,y)","floor(x)","ceiling(x)","sign(x)",
            "hex2dec(x)","if(x,y,z)","func:","X8float(x)","X4pos(x)","X4Heading(x)",
            "distanceToLine(x0,y0,x1,y1,x2,y2)",
            "direction(x,y)","orderByDistance(index,x_center,y_center,x1,y1,x2,y2, ...)",
            "orderByDistanceToLine(index,x_line1,y_line1,x_line2,y_line2,x1,y1,x2,y2, ...)",
            "numeric:","string:","var:xxx","lvar:xxx.size",
            "lvar:xxx.indexof(hello warudo)","lvar:xxx.lastindexof(hello warudo)",
            "lvar:xxx[1]","lvar:xxx[last]","elvar:xxx",

            "etvar:xxx",
            "tvar:xxx[2][4]","tvarcl:xxx[My column Name][3]","tvarrl:xxx[My Row Name][3]",
            "tvar:xxx.width","tvar:xxx.height","tvar:xxx[last][2]","tvar:xxx[2][last]",
        };
        string[] autoCompleteHeader = new string[] {
            "numeric:","string:","var:xxx","lvar:xxx.size",
            "lvar:xxx.indexof(hello warudo)","lvar:xxx.lastindexof(hello warudo)",
            "lvar:xxx[1]","lvar:xxx[last]","elvar:xxx",

            "etvar:xxx",
            "tvar:xxx[2][4]","tvarcl:xxx[My column Name][3]","tvarrl:xxx[My Row Name][3]",
            "tvar:xxx.width","tvar:xxx.height","tvar:xxx[last][2]","tvar:xxx[2][last]",
        };
        string[] autoCompletePartyAttributes = new string[] {
            "id","name","targetid","inparty","order","x","y","z","heading",
            "worldid","worldname","currentworldid","casttargetid","distance","job","role",
            "jobid","currenthp","currentmp","currentcp","currentgp",
            "maxhp","maxmp","maxcp","maxgp","level",
            "ownerid","type","iscasting","castbuffid","castdurationcurrent","castdurationmax",
            "bnpcnameid","bnpcid","pointer","memory[offset, length]"
        };
        string[] autoCompleteAuraAttributes = new string[] {
            "_x","_y","_w","_h","_opacity"
        }; string[] autoCompleteStringFunctions = new string[] {
            "toupper:","tolower:","length:","dec2hex:","padleft(<character code>,<length>):",
            "padright(<character code>,<length>):","substring(<start>):","substring(<start>,length):",
            "indexof(<string>):","lastindexof(<string>):",
            "trim:","trim(character code, character code, ...):",
            "trimleft:",
            "trimleft(character code, character code, ...):",
            "trimright:",
            "trimright(character code, character code, ...):",
            "format(type, format string):",
        };
        string[] autocompleteRegexs = new string[] {
            "(?<id>[0-9A-F]{8})","(?<actorID>[0-9A-F]{8})","(?<targetID>[0-9A-F]{8})",
            "(?<name>[^:]*?)","(?<actorName>[^:]*?)","(?<targetName>[^:]*?)",
            "(?<x>[^:]*?)","(?<y>[^:]*?)","(?<z>[^:]*?)","(?<param>[0-9A-F]{4})",
            "] 14:(?<actionID>[0-9A-F]{4}):","] 1B:(?<id>[0-9A-F]{8}):(?<name>[^:]*?):[0-9A-F]{4}:[0-9A-F]{4}:(?<marker>[0-9A-F]{4}):",
        };
        char[] attributeSplitters = new char[] { '.' };
        char[] stringFunctionSplitters = new char[] { ':' };
        DateTime autoCompleteLastRefreshTime;
        int autoCompleteLastItemCount;
        private bool isRegexTextBox() {
            return (this.Name == "txtRegexp"|| this. Name == "rexSearch");
        }
        private void showAutoCompleteList()
        {
            
            //this.autoListBox.Left = this.textBox1.Left;
            //this.autoListBox.Top = this.textBox1.Top + this.textBox1.Height + 2;
            this.autoListBox.Items.Clear();
            string lastWord;
            lastWord = GetLastWord(this.textBox1);
            if (isRegexTextBox())
            {
                this.autoListBox.Items.AddRange(this.autocompleteRegexs.Where(aw => {
                    if (lastWord.Length <= 0) return true;
                    if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                    return true;
                }).ToArray());
            }
            else
            {
                //if (lastWord.Length <= 0) return;
                if ((lastWord.StartsWith("_ffxivparty") || lastWord.StartsWith("_ffxiventity")) && lastWord.Contains("."))
                {
                    lastWord = lastWord.Split(attributeSplitters, 2, StringSplitOptions.None)[1];
                    this.autoListBox.Items.AddRange(this.autoCompletePartyAttributes.Where(aw =>
                    {
                        if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                        return true;
                    }).ToArray());
                }
                else if ((lastWord.StartsWith("_textaura") || lastWord.StartsWith("_imageaura")) && lastWord.Contains("."))
                {
                    lastWord = lastWord.Split(attributeSplitters, 2, StringSplitOptions.None)[1];
                    this.autoListBox.Items.AddRange(this.autoCompleteAuraAttributes.Where(aw =>
                    {
                        if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                        return true;
                    }).ToArray());
                }
                else if (lastWord.StartsWith("func:"))
                {
                    lastWord = lastWord.Split(stringFunctionSplitters, 2, StringSplitOptions.None)[1];
                    this.autoListBox.Items.AddRange(this.autoCompleteStringFunctions.Where(aw =>
                    {
                        if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                        return true;
                    }).ToArray());
                }
                else if (textBox1.Text.EndsWith("{")) {
                    this.autoListBox.Items.AddRange(this.autoCompleteHeader.Where(aw => {
                        if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                        return true;
                    }).ToArray());
                }
                else
                {
                    this.autoListBox.Items.AddRange(this.autoCompleteCommon.Where(aw => {
                        if (!aw.ToLower().StartsWith(lastWord.ToLower())) return false;
                        return true;
                    }).ToArray());

                }
            }
            
            
            var autoCompleteRefreshAllowed = false;
            if (DateTime.Now > autoCompleteLastRefreshTime.AddSeconds(1)) autoCompleteRefreshAllowed = true;
            if(autoCompleteLastItemCount<this.autoListBox.Items.Count) autoCompleteRefreshAllowed = true;
            if (autoCompleteRefreshAllowed)
            {
                if (this.autoListBox.Items.Count > 0)
                {
                    this.autoListBox.Visible = true;
                    this.autoListBox.Left = Math.Min( this.Width-200, GetCursorCoordinates(this.textBox1, lastWord.Length).X);
                    this.autoListBox.Top = GetCursorCoordinates(this.textBox1, lastWord.Length).Y;
                    
                    this.autoListBox.Height = Math.Min(200,Math.Max(30,this.autoListBox.Items.Count * 15));
                    autoListBox.BringToFront();
                }
                else
                {
                    this.autoListBox.Visible = false;
                }
                autoCompleteLastRefreshTime = DateTime.Now;
            }
            autoCompleteLastItemCount = this.autoListBox.Items.Count;
        }
        public Point GetCursorCoordinates(TextBox textBox1,int lastWordLength)
        {
            Point p2 = textBox1.GetPositionFromCharIndex(textBox1.SelectionStart-1- lastWordLength);
            Point p = textBox1.Location;
            p.X += p2.X;
            p.Y += p2.Y+30;
            return p;
        }
        private void SwitchToAutoCompleteList()
        {
            this.autoListBox.Focus();
            if (autoListBox.Items.Count > 0) { 
                this.autoListBox.SelectedIndex = 0;
            }
        }

        private void AutocompleteFinished()
        {
            var lastWord = GetLastWord(this.textBox1);
            var nextWord = this.autoListBox.Text;
            if (lastWord.Contains("."))
            {
                lastWord = lastWord.Split(attributeSplitters, StringSplitOptions.None).LastOrDefault();
            }
            if (lastWord.Contains(":"))
            {
                lastWord = lastWord.Split(stringFunctionSplitters, StringSplitOptions.None).LastOrDefault();
            }
            var substr1 = this.textBox1.Text.Substring(0, this.textBox1.SelectionStart - lastWord.Length);
            var substr2 = this.textBox1.Text.Substring(this.textBox1.SelectionStart, this.textBox1.Text.Length-this.textBox1.SelectionStart);
            this.textBox1.Text = substr1;
            this.textBox1.AppendText(nextWord + substr2);
            this.textBox1.Select(substr1.Length+nextWord.Length, 0);
            this.autoListBox.Visible = false;
        }

        private void TextBox1_Leave(object sender, EventArgs e)
        {
            if (!autoListBox.Focused) {
                autoListBox.Visible=false;
            }
        }

        private void TextBox1_Enter(object sender, EventArgs e)
        {
            showAutoCompleteList();
        }
    }

}
