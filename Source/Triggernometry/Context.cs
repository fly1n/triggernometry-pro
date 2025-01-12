﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Web.Script.Serialization;
using Triggernometry.Variables;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Triggernometry
{

    internal class Context
    {

		internal bool testmode;
        internal RealPlugin plug;
        internal Trigger trig;
        internal Action.TriggerForceTypeEnum force;

        internal RealPlugin.ActionExecutionHook soundhook;
        internal RealPlugin.ActionExecutionHook ttshook;

        internal static Regex rex = new Regex(@"\$\{(?<id>[^\}\{\$]*)\}");
        internal static Regex rex2 = new Regex(@"\$\<(?<id>[^\>\<\$]*)\>");
        internal static Regex rexnum = new Regex(@"\$(?<id>[0-9]+)");
        internal static Regex rexnump = new Regex(@"\[(?<index>.+?)\]\.(?<prop>[a-zA-Z]+)");
        internal static Regex rexnumprp3 = new Regex(@"\[(?<index>.+?)\]\.(?<prop>[a-zA-Z]+)(\((?<arg1>[^,]+),(?<arg2>[^,]+),(?<arg3>[^\)]+)\))");
        internal static Regex rexnumpnumnum = new Regex(@"\[(?<index>.+?)\]\.(?<prop>[a-zA-Z]+)\[(?<arg1>[0-9]+?),(?<arg2>[0-9]+?)\]");
        internal static Regex rexnumparg = new Regex(@"\[(?<index>.+?)\]\.(?<prop>[a-zA-Z]+)\((?<arg>[^\)]+)\)");
        internal static Regex rexlidx = new Regex(@"(?<name>[^\[]+)\[(?<index>.+?)\]");
        internal static Regex rexposstart = new Regex(@"pos\[(?<arg>.+?)\](?<others>.*)");
        internal static Regex rexpos = new Regex(@"\.(?<name>[^\[]+)\[(?<arg>.*?)\](?<others>.*)");
        internal static Regex rexlidx3 = new Regex(@"(?<name>[^\[]+)\[(?<index1>.+?),(?<index2>.+?),(?<index3>.+?)\]");
        internal static Regex rextidx = new Regex(@"(?<name>[^\[]+)\[(?<column>.+?)\]\[(?<row>.+?)\]");
        internal static Regex rexlprp = new Regex(@"(?<name>[^\.]+)\.(?<prop>[a-zA-Z]+)(\((?<arg>[^\)]+)\)){0,1}");
        internal static Regex rexlprp3 = new Regex(@"(?<name>[^\.]+)\.(?<prop>[a-zA-Z]+)(\((?<arg1>[^,]+),(?<arg2>[^,]+),(?<arg3>[^\)]+)\))");
        internal static Regex rexfunc = new Regex(@"(?<name>[^\(]{1,})(\((?<arg>[^\)]+)\)){0,1}");
        internal static Regex jvarfunc = new Regex(@"(?<name>[a-zA-Z]+)\((?<arg>.*)\)");
        internal Dictionary<string, string> namedgroups;
		internal List<string> numgroups;
        internal DateTime triggered;
        internal string contextResponse = "";
        internal dynamic contextJsonResponse;
        internal bool contextJsonParsed = false;

        internal List<int> ActionResults = new List<int>();
        internal Dictionary<Mutex, int> heldmutices = new Dictionary<Mutex, int>();

        internal const double EORZEA_MULTIPLIER = 3600D / 175D;

        private static MathParser mp = new MathParser();
		
		public Context()
		{
			namedgroups = new Dictionary<string, string>();
			numgroups = new List<string>();
        }

        public override string ToString()
        {
            return (trig != null ? trig.LogName : "(no trigger)") + " at " + triggered.ToString();
        }

        internal void PushActionResult(int i)
        {
            lock (ActionResults)
            {
                ActionResults.Add(i);
            }
        }

        internal int PeekActionResult(bool previous, int i)
        {
            lock (ActionResults)
            {
                if (previous == true)
                {
                    if (ActionResults.Count > 0)
                    {
                        return ActionResults[ActionResults.Count - 1];
                    }
                    return 0;
                }
                else
                {
                    if (i < 1 || i > ActionResults.Count)
                    {
                        return 0;
                    }
                    return ActionResults[i - 1];
                }
            }
        }

        public delegate void LoggerCallback(object o, string message);

        public double EvaluateNumericExpression(LoggerDelegate logger, object o, string expr)
        {
            string exp = ExpandVariables(logger, o, true, expr == null ? "" : expr);
            if (plug != null)
            {
                exp = plug.cfg.PerformSubstitution(exp, Configuration.Substitution.SubstitutionScopeEnum.NumericExpression);
            }
            lock (mp)
            {
                return mp.Parse(exp);
            }
        }

        public string EvaluateStringExpression(LoggerDelegate logger, object o, string expr)
        {            
            string exp = ExpandVariables(logger, o, false, expr == null ? "" : expr);
            if (plug != null)
            {
                exp = plug.cfg.PerformSubstitution(exp, Configuration.Substitution.SubstitutionScopeEnum.StringExpression);
            }
            return exp;
        }

        public delegate void LoggerDelegate(object o, string msg);

        private TimeSpan GetEorzeanTime()
        {
            long epochTicks = DateTime.UtcNow.Ticks - (new DateTime(1970, 1, 1).Ticks);
            long eorzeaTicks = (long)Math.Round(epochTicks * EORZEA_MULTIPLIER);
            return new DateTime(eorzeaTicks).TimeOfDay;
        }

        private VariableScalar GetScalarVariable(VariableStore vs, string varname, bool createNew)
        {
            if (vs.Scalar.ContainsKey(varname) == true)
            {
                return vs.Scalar[varname];
            }
            VariableScalar vl = new VariableScalar();
            if (createNew == true)
            {
                vs.Scalar[varname] = vl;
            }
            return vl;
        }

        private VariableList GetListVariable(VariableStore vs, string varname, bool createNew)
        {
            if (vs.List.ContainsKey(varname) == true)
            {
                return vs.List[varname];
            }
            VariableList vl = new VariableList();
            if (createNew == true)
            {
                vs.List[varname] = vl;
            }
            return vl;
        }

        private VariableTable GetTableVariable(VariableStore vs, string varname, bool createNew)
        {
            if (vs.Table.ContainsKey(varname) == true)
            {
                return vs.Table[varname];
            }
            VariableTable vl = new VariableTable();
            if (createNew == true)
            {
                vs.Table[varname] = vl;
            }
            return vl;
        }

        private static string[] SplitArguments(string args)
        {
            var parmChars = args.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ',')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string ExpandVariables(LoggerDelegate logger, object o, bool numeric, string expr)
        {            
            Match m, mx;
            string newexpr = expr;
            int i = 1;
            while (true)
            {
                m = rex.Match(newexpr);
                if (m.Success == false) m = rex2.Match(newexpr);
                if (m.Success == false)
                {
                    m = rexnum.Match(newexpr);
                    if (m.Success == false)
                    {
                        break;
                    }
                }
                string x = m.Groups["id"].Value;
                string val = "";
                bool found = false;
                if (testmode == true)
                {
                    if (x == "_since")
                    {
                        val = ((int)Math.Floor((DateTime.UtcNow - triggered).TotalSeconds)).ToString();                        
                    }
                    else if (x == "_sincems")
                    {
                        val = ((int)Math.Floor((DateTime.UtcNow - triggered).TotalMilliseconds)).ToString();                        
                    }
                    else
                    {
                        val = (numeric == true ? "1" : "test");
                    }
                    found = true;
                }
                else
                {
                    int gn;
                    if (Int32.TryParse(x, out gn) == true)
                    {
                        if (gn >= 0 && gn < numgroups.Count)
                        {
                            val = numgroups[gn];
                            if (plug != null)
                            {
                                val = plug.cfg.PerformSubstitution(val, Configuration.Substitution.SubstitutionScopeEnum.CaptureGroup);
                            }
                            found = true;
                        }
                    }
                    if (found == false)
                    {
                        if (namedgroups.ContainsKey(x) == true)
                        {
                            val = namedgroups[x];
                            if (plug != null)
                            {
                                val = plug.cfg.PerformSubstitution(val, Configuration.Substitution.SubstitutionScopeEnum.CaptureGroup);
                            }
                            found = true;
                        }
                    }
                    if (found == false)
                    {
                        if (x == "_since")
                        {
                            val = ((int)Math.Floor((DateTime.UtcNow - triggered).TotalSeconds)).ToString();
                            found = true;
                        }
                        else if (x == "_sincems")
                        {
                            val = ((int)Math.Floor((DateTime.UtcNow - triggered).TotalMilliseconds)).ToString();
                            found = true;
                        }
                        else if (x == "_systemtime")
                        {
                            val = ((long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds).ToString();
                            found = true;
                        }
                        else if (x == "_systemtimems")
                        {
                            val = ((long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds).ToString();
                            found = true;
                        }
                        else if (x == "_folder")
                        {
                            val = this.trig.Parent.FullPath;
                            found = true;
                        }
                        else if (x == "_fullpath")
                        {
                            val = this.trig.FullPath;
                            found = true;
                        }
                        else if (x == "_ffxivplayer")
                        {
                            VariableDictionary vc = PluginBridges.BridgeFFXIV.GetMyself();
                            if (vc != null)
                            {
                                val = vc.GetValue("name").ToString();
                            }
                            found = true;
                        }
                        else if (x == "_ffxivzoneid")
                        {
                            val = PluginBridges.BridgeFFXIV.ZoneID.ToString();
                            found = true;
                        }
                        else if (x == "_ffxivpartyorder")
                        {
                            val = plug.cfg.FfxivPartyOrdering + " " + plug.cfg.FfxivCustomPartyOrder;
                            found = true;
                        }
                        else if (x == "_ffxivprocid")
                        {
                            val = PluginBridges.BridgeFFXIV.GetProcessId().ToString();
                            found = true;
                        }
                        else if (x == "_ffxivprocname")
                        {
                            val = PluginBridges.BridgeFFXIV.GetProcessName();
                            found = true;
                        }
                        else if (x == "_ffxivbaseaddress")
                        {
                            val = PluginBridges.BridgeFFXIV.GetFFXIVBaseAddress(0);
                            found = true;
                        }
                        else if (x == "_incombat")
                        {
                            val = plug != null && plug.InCombatHook() ? "1" : "0";
                            found = true;
                        }
                        else if (x == "_duration")
                        {
                            try
                            {
                                if (plug != null && plug.InCombatHook() == true)
                                {
                                    val = ((int)Math.Floor(plug.EncounterDurationHook())).ToString();
                                }
                                else
                                {
                                    val = "0";
                                }
                            }
                            catch (Exception)
                            {
                                val = "0";
                            }
                            found = true;
                        }
                        else if (x == "_response")
                        {
                            val = contextResponse;
                            found = true;
                        }
                        else if (x == "_triggername")
                        {
                            val = trig != null ? trig.Name : "(null)";
                            found = true;
                        }
                        else if (x == "_triggerid")
                        {
                            val = trig != null ? trig.Id.ToString() : "(null)";
                            found = true;
                        }
                        else if (x.IndexOf("_actionhistory") == 0)
                        {
                            mx = rexlidx.Match(x);
                            if (mx.Success == true)
                            {
                                string idx = mx.Groups["index"].Value;
                                if (idx == "previous")
                                {
                                    val = PeekActionResult(true, 0).ToString();
                                }
                                else
                                {
                                    val = PeekActionResult(false, Int32.Parse(idx)).ToString();
                                }
                                found = true;
                            }
                        }
                        else if (x.IndexOf("_env") == 0)
                        {
                            mx = rexlidx.Match(x);
                            if (mx.Success == true)
                            {
                                string idx = mx.Groups["index"].Value;
                                val = System.Environment.GetEnvironmentVariable(idx);
                                found = true;
                            }
                        }
                        else if (x.IndexOf("_isfileexist") == 0)
                        {
                            mx = rexlidx.Match(x);
                            if (mx.Success == true)
                            {
                                string dir = mx.Groups["index"].Value;
                                val = System.IO.File.Exists(dir)?"1":"0";
                                found = true;
                            }
                        }
                        else if (x.IndexOf("_jsonresponse") == 0)
                        {
                            mx = rexlidx.Match(x);
                            if (mx.Success == true)
                            {
                                string pathspec = mx.Groups["index"].Value;
                                if (contextJsonParsed == false)
                                {
                                    JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                                    contextJsonResponse = jsonSerializer.Deserialize<dynamic>(contextResponse);
                                    contextJsonParsed = true;
                                }
                                string[] path = pathspec.Split("/".ToCharArray());
                                dynamic curdir = contextJsonResponse;
                                foreach (string px in path)
                                {
                                    curdir = curdir[px];
                                }
                                val = curdir.ToString();
                                found = true;
                            }
                        }
                        else if (x.IndexOf("_ffxivmemory") == 0)
                        {
                            mx = rexlidx3.Match(x);
                            if (mx.Success == true)
                            {
                                Int64 pointer;
                                Int64 offset;
                                int length = 0;
                                if (Int64.TryParse(mx.Groups["index1"].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out pointer) == true)
                                {
                                    if (Int64.TryParse(mx.Groups["index2"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out offset) == true)
                                    {

                                        int.TryParse(mx.Groups["index3"].Value, out length);
                                        byte[] buffer = new byte[length < 8 ? 8 : length];
                                        IntPtr ptr = (IntPtr)(pointer + offset);

                                        val = "";
                                        if (mx.Groups["index3"].Value == "X8")
                                        {
                                            PluginBridges.BridgeFFXIV.ReadFFXIVMemory((IntPtr)ptr, buffer, 4);
                                            val += BitConverter.ToUInt32(buffer, 0).ToString("X8");
                                        }
                                        else if (mx.Groups["index3"].Value == "X16")
                                        {
                                            PluginBridges.BridgeFFXIV.ReadFFXIVMemory((IntPtr)ptr, buffer, 8);
                                            val += BitConverter.ToUInt64(buffer, 0).ToString("X16");
                                        }
                                        else if (mx.Groups["index3"].Value == "X4")
                                        {
                                            PluginBridges.BridgeFFXIV.ReadFFXIVMemory((IntPtr)ptr, buffer, 2);
                                            val += BitConverter.ToUInt16(buffer, 0).ToString("X4");
                                        }
                                        else
                                        {
                                            PluginBridges.BridgeFFXIV.ReadFFXIVMemory((IntPtr)ptr, buffer, length);
                                            for (int iptr = 0; iptr < length; iptr += 4)
                                            {
                                                val += BitConverter.ToUInt32(buffer, iptr).ToString("X8") + " ";
                                            }
                                        }
                                        found = true;
                                    }
                                }

                            }
                        }
                        else if (x.IndexOf("_ffxivsignature") == 0)
                        {
                            mx = rexlidx.Match(x);
                            if (mx.Success == true)
                            {
                                Int64 pointer;
                                if (Int64.TryParse(mx.Groups["index"].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out pointer) == true)
                                {
                                    val = PluginBridges.BridgeFFXIV.GetFFXIVSignature64((IntPtr)pointer);
                                    found = true;
                                }

                            }
                        }
                        else if (x.IndexOf("_signatureshort") == 0)
                        {
                            mx = rexlidx3.Match(x);
                            if (mx.Success == true)
                            {
                                Int64 baseAddress;
                                UInt16 sig;
                                int len = 0;
                                if (UInt16.TryParse(mx.Groups["index1"].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sig) == true)
                                {
                                    if (Int64.TryParse(mx.Groups["index2"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out baseAddress) == true)
                                    {

                                        int.TryParse(mx.Groups["index3"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out len);
                                        //val = PluginBridges.BridgeFFXIV.GetFFXIVSignature16(sig,(IntPtr)baseAddress,len);
                                    }
                                }
                            }
                        }
                        else if (x.IndexOf("_signatureint") == 0)
                        {
                            mx = rexlidx3.Match(x);
                            if (mx.Success == true)
                            {
                                Int64 baseAddress;
                                UInt32 sig;
                                int len = 0;
                                if (UInt32.TryParse(mx.Groups["index1"].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sig) == true)
                                {
                                    if (Int64.TryParse(mx.Groups["index2"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out baseAddress) == true)
                                    {

                                        int.TryParse(mx.Groups["index3"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out len);
                                        val = PluginBridges.BridgeFFXIV.GetFFXIVSignature32(sig, (IntPtr)baseAddress, len);
                                    }
                                }
                            }
                        }
                        else if (x.IndexOf("_signaturelong") == 0)
                        {
                            mx = rexlidx3.Match(x);
                            if (mx.Success == true)
                            {
                                Int64 baseAddress;
                                UInt64 sig;
                                int len = 0;
                                if (UInt64.TryParse(mx.Groups["index1"].Value, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sig) == true)
                                {
                                    if (Int64.TryParse(mx.Groups["index2"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out baseAddress) == true)
                                    {

                                        int.TryParse(mx.Groups["index3"].Value, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out len);
                                        val = PluginBridges.BridgeFFXIV.GetFFXIVSignature64(sig, (IntPtr)baseAddress, len);
                                    }
                                }
                            }
                        }
                        else if (x.IndexOf("_pos") == 0)
                        {
                            mx = rexposstart.Match(x);
                            if (mx.Success == true)
                            {
                                double pos_x = 0;
                                double pos_y = 0;
                                double x0 = 0;
                                double y0 = 0;
                                string arg = mx.Groups["arg"].Value.TrimStart('(').TrimEnd(')').Replace(" ", "");
                                string name = "";
                                double.TryParse(arg.Split(',')[0], out pos_x);
                                double.TryParse(arg.Split(',')[1], out pos_y);
                                val = pos_x.ToString("0.000") + "," + pos_y.ToString("0.000");
                                string others = mx.Groups["others"].Value;
                                Match my = rexpos.Match(others);
                                while (my.Success == true)
                                {
                                    name = my.Groups["name"].Value.TrimStart('(').TrimEnd(')').Replace(" ", ""); ;
                                    arg = my.Groups["arg"].Value;
                                    others = my.Groups["others"].Value;
                                    //00  南逆时针为正
                                    //01   xy相反其余不变
                                    switch (name)
                                    {
                                        case "moveDir":
                                            {
                                                double dir; double.TryParse(arg.Split(',')[0], out dir);
                                                dir = dir * Math.PI / 180;
                                                double dis; double.TryParse(arg.Split(',')[1], out dis);
                                                pos_x = pos_x + Math.Sin(dir) * dis;
                                                pos_y = pos_y + Math.Cos(dir) * dis;
                                            };
                                            break;
                                        case "moveTo":
                                            {
                                                double target_x; double.TryParse(arg.Split(',')[0], out target_x);

                                                double target_y; double.TryParse(arg.Split(',')[1], out target_y);
                                                double dir = Math.Atan2(target_x - pos_x, target_y - pos_y);
                                                double dis; double.TryParse(arg.Split(',')[2], out dis);
                                                pos_x = pos_x + Math.Sin(dir) * dis;
                                                pos_y = pos_y + Math.Cos(dir) * dis;
                                            };
                                            break;
                                        case "moveXY":
                                            {
                                                double dx; double.TryParse(arg.Split(',')[0], out dx);
                                                double dy; double.TryParse(arg.Split(',')[1], out dy);
                                                pos_x = pos_x + dx;
                                                pos_y = pos_y + dy;
                                            }; break;
                                        case "flipX":
                                            {
                                                pos_x = -(pos_x - x0) + x0;
                                            }; break;
                                        case "flipY":
                                            {
                                                pos_y = -(pos_y - y0) + y0;
                                            }; break;
                                        case "rotate":
                                            {
                                                double dir; double.TryParse(arg, out dir);
                                                dir = dir * Math.PI / 180;
                                                var dis = Math.Sqrt((pos_x - x0) * (pos_x - x0) + (pos_y - y0) * (pos_y - y0));
                                                double dir0 = Math.Atan2(pos_x - x0, pos_y - y0);
                                                pos_x = x0 + Math.Sin(dir + dir0) * dis;
                                                pos_y = y0 + Math.Cos(dir + dir0) * dis;
                                            }; break;
                                        case "scaleX":
                                            {
                                                double scale; double.TryParse(arg, out scale);
                                                pos_x = x0 + (pos_x - x0) * scale;
                                            }; break;
                                        case "scaleY":
                                            {
                                                double scale; double.TryParse(arg, out scale);
                                                pos_y = y0 + (pos_y - y0) * scale;
                                            }; break;
                                        case "setCenter":
                                            {
                                                double dx; double.TryParse(arg.Split(',')[0], out x0);
                                                double dy; double.TryParse(arg.Split(',')[1], out y0);
                                            }; break;
                                        default: break;


                                    }

                                    my = rexpos.Match(others);
                                }
                                if (others == ".getX")
                                {
                                    val = pos_x.ToString("0.000");
                                }
                                else if (others == ".getY")
                                {
                                    val = pos_y.ToString("0.000");
                                }
                                else
                                {
                                    val = pos_x.ToString("0.000") + "," + pos_y.ToString("0.000");
                                }
                                found = true;

                            }
                        }
                        else if (x.IndexOf("_ref") == 0)
                        {

                            object obj = PluginBridges.BridgeFFXIV.ReflectPlugin(x);
                            JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
                            val = JsonConvert.SerializeObject(obj);
                            //val = jsonSerializer.Serialize(obj);

                        }                        // check if scalar variable exists
                        else if ((x.IndexOf("evar:") == 0) || (x.IndexOf("epvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("evar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(5);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(6);
                            }
                            lock (store.Scalar) // verified
                            {
                                if (store.Scalar.ContainsKey(varname) == true)
                                {
                                    val = "1";
                                }
                                else
                                {
                                    val = "0";
                                }
                                found = true;
                            }
                        }
                        else if ((x.IndexOf("ejvar:") == 0) || (x.IndexOf("epjvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("ejvar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(6);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(7);
                            }
                            lock (store.Scalar) // verified
                            {
                                var token = store.Json.Value.SelectToken(varname);
                                if(token!=null)
                                {
                                    val = "1";
                                }
                                else
                                {
                                    val = "0";
                                }
                                found = true;
                            }
                        }
                        // check if list variable exists
                        else if ((x.IndexOf("elvar:") == 0) || (x.IndexOf("eplvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("elvar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(6);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(7);
                            }
                            lock (store.List) // verified
                            {
                                if (store.List.ContainsKey(varname) == true)
                                {
                                    val = "1";
                                }
                                else
                                {
                                    val = "0";
                                }
                                found = true;
                            }
                        }
                        // check if table variable exists
                        else if ((x.IndexOf("etvar:") == 0) || (x.IndexOf("eptvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("etvar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(6);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(7);
                            }
                            lock (store.Table) // verified
                            {
                                if (store.Table.ContainsKey(varname) == true)
                                {
                                    val = "1";
                                }
                                else
                                {
                                    val = "0";
                                }
                                found = true;
                            }
                        }
                        // retrieve scalar variable value
                        else if ((x.IndexOf("var:") == 0) || (x.IndexOf("pvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("var:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(4);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(5);
                            }
                            lock (store.Scalar) // verified
                            {
                                VariableScalar vs = GetScalarVariable(store, varname, false);
                                val = vs.Value;
                                found = true;
                            }
                        }
                        else if ((x.IndexOf("jvar:") == 0) || (x.IndexOf("pjvar:") == 0) || (x.IndexOf("_jparty:") == 0) || (x.IndexOf("_jentity:") == 0))
                        {
                            Variables.VariableStore store = plug.sessionvars;
                            JObject root=null;
                            string varname;
                            if (x.IndexOf("jvar:") == 0)
                            {
                                store = plug.sessionvars;
                                root = store.Json.Value;
                                varname = x.Substring(5);
                            }
                            else if (x.IndexOf("pjvar:") == 0)
                            {
                                store = plug.cfg.PersistentVariables;
                                root = store.Json.Value;
                                varname = x.Substring(6);
                            }
                            else if (x.IndexOf("_jparty:") == 0)
                            {
                                store = plug.sessionvars;
                                root = store.Json.Value;
                                varname = x.Substring(8);
                                root["_ffxivparty"] = PluginBridges.BridgeFFXIV.JPartyMembers;
                                
                            }
                            else if (x.IndexOf("_jentity:") == 0)
                            {
                                store = plug.sessionvars;
                                root = store.Json.Value;
                                varname = x.Substring(9);
                                root["_ffxiventity"] = PluginBridges.BridgeFFXIV.JCombatants;
                                
                            }
                            else {
                                store = plug.sessionvars;
                                root = store.Json.Value;
                                varname = x.Substring(5);
                            }
                            string tokenname;
                            string method = "";
                            mx = jvarfunc.Match(varname);
                            if (mx.Success == true)
                            {
                                tokenname = mx.Groups["arg"].Value;
                                method = mx.Groups["name"].Value;
                                
                            }
                            else {
                                tokenname = varname;
                            }

                            lock (store.Json) // verified
                            {
                                var tokens = root.SelectTokens(tokenname);
                                val = "";
                                if (tokens.Count() >= 1)
                                {
                                    var count = 0;
                                    foreach (var token in tokens)
                                    {
                                        switch (method)
                                        {
                                            case "size":
                                                {
                                                    if (token.Type == JTokenType.Array)
                                                    {
                                                        val +=(token as JArray).Count;
                                                    }
                                                    else if (token.Type == JTokenType.Object)
                                                    {
                                                        val += (token as JObject).Count;
                                                    }
                                                    else
                                                    {
                                                        val += token.Children().Count();
                                                    }
                                                }
                                                break;
                                            case "typeof":
                                                {
                                                    val += token.Type.ToString();
                                                }
                                                break;
                                            case "pathof":
                                                {
                                                    val += token.Path;
                                                }
                                                break;
                                            case "indexof":
                                                {
                                                    if (token.Parent != null)
                                                    {
                                                        if (token.Parent.Type == JTokenType.Array)
                                                        {
                                                            val += (token.Parent as JArray).IndexOf(token);
                                                        }
                                                        else if (token.Parent.Type == JTokenType.Object)
                                                        {
                                                            val += (token.Parent as JObject).PropertyValues().ToList().IndexOf(token);
                                                        }
                                                        else if (token.Parent.Type == JTokenType.Property)
                                                        {
                                                            val += (token.Parent.Parent as JObject).PropertyValues().ToList().IndexOf(token.Parent);
                                                        }
                                                        else
                                                        {
                                                            val += "0";
                                                        }
                                                    }
                                                    else
                                                    { 
                                                        val += "0";
                                                    }
                                                }
                                                break;
                                            case "print":
                                                {
                                                    Regex r = new Regex(@"[\s\t]*?[\[\{\]\},]+");

                                                    val += r.Replace(token.ToString().Replace("\"","").Replace(":","\t:"),"");
                                                }
                                                break;
                                            default:
                                                val += JsonConvert.SerializeObject(token);
                                                break;
                                        }
                                        count++;
                                        if (count < tokens.Count())
                                        {
                                            val += ",";
                                        }
                                    }
                                }
                                found = true;
                            }

                        }
                        // retrieve list variable value
                        else if ((x.IndexOf("lvar:") == 0) || (x.IndexOf("plvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("lvar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(5);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(6);
                            }
                            mx = rexlprp.Match(varname);
                            if (mx.Success == true)
                            {
                                string gname = mx.Groups["name"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                switch (gprop)
                                {
                                    case "size":
                                        {
                                            lock (store.List)
                                            {
                                                VariableList vl = GetListVariable(store, gname, false);
                                                val = vl.Size().ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                    case "indexof":
                                        {
                                            string garg = mx.Groups["arg"].Value;
                                            lock (store.List)
                                            {
                                                VariableList vl = GetListVariable(store, gname, false);
                                                val = vl.IndexOf(garg).ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                    case "lastindexof":
                                        {
                                            string garg = mx.Groups["arg"].Value;
                                            lock (store.List)
                                            {
                                                VariableList vl = GetListVariable(store, gname, false);
                                                val = vl.LastIndexOf(garg).ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                mx = rexlidx.Match(varname);
                                if (mx.Success == true)
                                {
                                    string gname = mx.Groups["name"].Value;
                                    string gindex = mx.Groups["index"].Value;
                                    lock (store.List)
                                    {
                                        VariableList vl = GetListVariable(store, gname, false);
                                        if (gindex == "last")
                                        {
                                            gindex = vl.Size().ToString();
                                        }
                                        int iindex;
                                        if (Int32.TryParse(gindex, out iindex) == true)
                                        {
                                            val = vl.Peek(iindex).ToString();
                                        }
                                        found = true;
                                    }
                                    found = true;
                                }
                            }
                        }
                        // retrieve table variable value
                        else if ((x.IndexOf("tvar:") == 0) || (x.IndexOf("ptvar:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("tvar:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(5);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(6);
                            }
                            mx = rexlprp.Match(varname);
                            if (mx.Success == true)
                            {
                                string gname = mx.Groups["name"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                switch (gprop)
                                {
                                    case "w":
                                    case "width":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = vt.Width.ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                    case "h":
                                    case "height":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = vt.Height.ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                mx = rextidx.Match(varname);
                                if (mx.Success == true)
                                {
                                    string gname = mx.Groups["name"].Value;
                                    string gcol = mx.Groups["column"].Value;
                                    string grow = mx.Groups["row"].Value;
                                    lock (store.Table)
                                    {
                                        VariableTable vt = GetTableVariable(store, gname, false);
                                        if (gcol == "last")
                                        {
                                            gcol = vt.Width.ToString();
                                        }
                                        if (grow == "last")
                                        {
                                            grow = vt.Height.ToString();
                                        }
                                        int xindex, yindex;
                                        if (Int32.TryParse(gcol, out xindex) == true)
                                        {
                                            if (Int32.TryParse(grow, out yindex) == true)
                                            {
                                                val = vt.Peek(xindex, yindex).ToString();
                                            }
                                        }
                                        found = true;
                                    }
                                    found = true;
                                }
                            }
                        }
                        // row-based table lookup
                        else if ((x.IndexOf("tvarrl:") == 0) || (x.IndexOf("ptvarrl:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("tvarrl:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(7);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(8);
                            }
                            mx = rexlprp.Match(varname);
                            if (mx.Success == true)
                            {
                                string gname = mx.Groups["name"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                switch (gprop)
                                {
                                    case "w":
                                    case "width":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = (vt.Width > 0 ? vt.Width - 1 : 0).ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                    case "h":
                                    case "height":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = vt.Height.ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                mx = rextidx.Match(varname);
                                if (mx.Success == true)
                                {
                                    string gname = mx.Groups["name"].Value;
                                    string gheader = mx.Groups["column"].Value;
                                    string gindex = mx.Groups["row"].Value;
                                    lock (store.Table)
                                    {
                                        VariableTable vt = GetTableVariable(store, gname, false);
                                        if (gindex == "last")
                                        {
                                            gindex = (vt.Width > 0 ? vt.Width - 1 : 0).ToString();
                                        }
                                        int xindex;
                                        if (Int32.TryParse(gindex, out xindex) == true)
                                        {
                                            int yindex = vt.SeekRow(gheader);
                                            if (yindex > 0)
                                            {
                                                val = vt.Peek(xindex + 1, yindex).ToString();
                                            }
                                        }
                                        found = true;
                                    }
                                    found = true;
                                }
                            }
                        }
                        // column-based table lookup
                        else if ((x.IndexOf("tvarcl:") == 0) || (x.IndexOf("ptvarcl:") == 0))
                        {
                            Variables.VariableStore store;
                            string varname;
                            if (x.IndexOf("tvarcl:") == 0)
                            {
                                store = plug.sessionvars;
                                varname = x.Substring(7);
                            }
                            else
                            {
                                store = plug.cfg.PersistentVariables;
                                varname = x.Substring(8);
                            }
                            mx = rexlprp.Match(varname);
                            if (mx.Success == true)
                            {
                                string gname = mx.Groups["name"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                switch (gprop)
                                {
                                    case "w":
                                    case "width":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = vt.Width.ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                    case "h":
                                    case "height":
                                        {
                                            lock (store.Table)
                                            {
                                                VariableTable vt = GetTableVariable(store, gname, false);
                                                val = (vt.Height > 0 ? vt.Height - 1 : 0).ToString();
                                                found = true;
                                            }
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                mx = rextidx.Match(varname);
                                if (mx.Success == true)
                                {
                                    string gname = mx.Groups["name"].Value;
                                    string gheader = mx.Groups["column"].Value;
                                    string gindex = mx.Groups["row"].Value;
                                    lock (store.Table)
                                    {
                                        VariableTable vt = GetTableVariable(store, gname, false);
                                        if (gindex == "last")
                                        {
                                            gindex = (vt.Height > 0 ? vt.Height - 1 : 0).ToString();
                                        }
                                        int yindex;
                                        if (Int32.TryParse(gindex, out yindex) == true)
                                        {
                                            int xindex = vt.SeekColumn(gheader);
                                            if (xindex > 0)
                                            {
                                                val = vt.Peek(xindex, yindex + 1).ToString();
                                            }
                                        }
                                        found = true;
                                    }
                                    found = true;
                                }
                            }
                        }
                        else if (x.IndexOf("numeric:") == 0)
                        {
                            string numexpr = x.Substring(8);
                            val = I18n.ThingToString(EvaluateNumericExpression(logger, o, numexpr));
                            found = true;
                        }
                        else if (x.IndexOf("string:") == 0)
                        {
                            string numexpr = x.Substring(7);
                            val = EvaluateStringExpression(logger, o, numexpr);
                            found = true;
                        }
                        else if (x.IndexOf("func:") == 0)
                        {
                            val = "";
                            found = true;
                            string funcexpr = x.Substring(5);
                            int splitter = funcexpr.IndexOf(":");
                            if (splitter > 0)
                            {
                                string funcdef = funcexpr.Substring(0, splitter);
                                Match rxm = rexfunc.Match(funcdef);
                                if (rxm.Success == true)
                                {
                                    string funcname = rxm.Groups["name"].Value.ToLower();
                                    string funcarg = rxm.Groups["arg"].Value;
                                    string[] args = SplitArguments(funcarg);
                                    int argc = args.Count();
                                    string funcval = funcexpr.Substring(splitter + 1);
                                    switch (funcname)
                                    {
                                        case "toupper": // toupper()
                                            val = funcval.ToUpper();
                                            break;
                                        case "tolower": // tolower()
                                            val = funcval.ToLower();
                                            break;
                                        case "totitle":
                                            {
                                                TextInfo tInfo = Thread.CurrentThread.CurrentCulture.TextInfo;
                                                funcval = tInfo.ToTitleCase(funcval);
                                                val = funcval.ToString();
                                                break;
                                            }
                                        case "length": // length()
                                            val = funcval.Length.ToString();
                                            break;
                                        case "hex2dec": // hex2dec()
                                            val = "" + int.Parse(funcval, System.Globalization.NumberStyles.HexNumber);
                                            break;
                                        case "dec2hex": // dec2hex()
                                            val = int.Parse(funcval).ToString("X");
                                            break;
                                        case "padleft": // padleft(charcode,length)
                                            if (argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/padleftargerror", "Padleft function requires two arguments, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = funcval.PadLeft(Int32.Parse(args[1]), (char)Int32.Parse(args[0]));
                                            }
                                            break;
                                        case "padright": // padright(charcode,length)
                                            if (argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/padrightargerror", "Padright function requires two arguments, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = funcval.PadRight(Int32.Parse(args[1]), (char)Int32.Parse(args[0]));
                                            }
                                            break;
                                        case "job": // padright(charcode,length)
                                            if (argc != 1)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/padrightargerror", "Job function requires one argument, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = PluginBridges.BridgeFFXIV.TranslateJob(funcval, args[0]);
                                            }
                                            break;
                                        case "substring": // substring(startindex, length) or substring(startindex)
                                            if (argc != 1 && argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/substringargerror", "Substring function requires one or two arguments, {0} were given", argc));
                                            }
                                            else
                                            {
                                                switch (argc)
                                                {
                                                    case 1:
                                                        val = funcval.Substring(Int32.Parse(args[0]));
                                                        break;
                                                    case 2:
                                                        val = funcval.Substring(Int32.Parse(args[0]), Int32.Parse(args[1]));
                                                        break;
                                                }
                                            }
                                            break;
                                        case "indexof": // indexof(stringtosearch)
                                            if (argc != 1)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/indexofargerror", "Indexof function requires one argument, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = "" + funcval.IndexOf(args[0]);
                                            }
                                            break;
                                        case "replace": // indexof(stringtosearch)
                                            if (argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/replaceargerror", "Replace function requires two argument, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = "" + funcval.Replace(args[0],args[1]);
                                            }
                                            break;
                                        case "compare": // compare(stringtocompare) or compare(stringtocompare, ignorecase)
                                            if (argc != 1 && argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/compareargerror", "Compare function requires one or two arguments, {0} were given", argc));
                                            }
                                            else
                                            {
                                                switch (argc)
                                                {
                                                    case 1:
                                                        val = "" + String.Compare(funcval, args[0], true);
                                                        break;
                                                    case 2:
                                                        bool ignoreCase = bool.Parse(args[1]);
                                                        val = "" + String.Compare(funcval, args[0], ignoreCase);
                                                        break;
                                                }
                                            }
                                            break;
                                        case "lastindexof": // lastindexof(stringtosearch)
                                            if (argc != 1)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/lastindexofargerror", "Lastindexof function requires one argument, {0} were given", argc));
                                            }
                                            else
                                            {
                                                val = "" + funcval.LastIndexOf(args[0]);
                                            }
                                            break;
                                        case "trim": // trim() or trim(charcode,charcode,charcode,...)
                                            if (argc == 0 || args[0] == "")
                                            {
                                                val = funcval.Trim();
                                            }
                                            else
                                            {
                                                string xx = "";
                                                foreach (string xyz in args)
                                                {
                                                    xx += (char)Int32.Parse(xyz);
                                                }
                                                val = funcval.Trim(xx.ToCharArray());
                                            }
                                            break;
                                        case "trimleft": // trimleft() or trimleft(charcode,charcode,charcode,...)
                                            if (argc == 0 || args[0] == "")
                                            {
                                                val = funcval.TrimStart();
                                            }
                                            else
                                            {
                                                string xx = "";
                                                foreach (string xyz in args)
                                                {
                                                    xx += (char)Int32.Parse(xyz);
                                                }
                                                val = funcval.TrimStart(xx.ToCharArray());
                                            }
                                            break;
                                        case "trimright": // trimright() or trimright(charcode,charcode,charcode,...)
                                            if (argc == 0 || args[0] == "")
                                            {
                                                val = funcval.TrimEnd();
                                            }
                                            else
                                            {
                                                string xx = "";
                                                foreach (string xyz in args)
                                                {
                                                    xx += (char)Int32.Parse(xyz);
                                                }
                                                val = funcval.TrimEnd(xx.ToCharArray());
                                            }
                                            break;
                                        case "format": // format(type,formatstring)
                                            if (argc != 2)
                                            {
                                                throw new ArgumentException(I18n.Translate("internal/Context/formatargerror", "Format function requires two arguments, {0} were given", argc));
                                            }
                                            else
                                            {
                                                Type type = Type.GetType(args[0]);
                                                object converted = Convert.ChangeType(funcval, type, CultureInfo.InvariantCulture);
                                                val = String.Format("{0:" + args[1] + "}", converted);
                                            }
                                            break;
                                        case "hex2str":
                                            {

                                            }
                                            break;
                                        case "trimname":
                                            {
                                                string name = "";
                                                string server = "";
                                                PluginBridges.BridgeFFXIV.SplitNameServer(funcval, out name, out server);
                                                val = name;
                                            }
                                            break;
                                        case "trimserver":
                                            {
                                                string name = "";
                                                string server = "";
                                                PluginBridges.BridgeFFXIV.SplitNameServer(funcval, out name, out server);
                                                val = server;
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                        else if (x.IndexOf("_ffxivparty") == 0)
                        {

                            mx = rexnump.Match(x);
                            var mx2 = rexnumparg.Match(x);
                            if (mx.Success == true)
                            {
                                string gindex = mx.Groups["index"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                int iindex = 0;
                                Int64 honk;
                                VariableDictionary vc = null;
                                bool foundid = false;
                                if (Int64.TryParse(gindex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out honk) == true)
                                {
                                    vc = PluginBridges.BridgeFFXIV.GetIdPartyMember(gindex, out foundid);
                                }
                                if (foundid == false)
                                {
                                    if (Int32.TryParse(gindex, out iindex) == true)
                                    {
                                        vc = PluginBridges.BridgeFFXIV.GetPartyMember(iindex);
                                    }
                                    else
                                    {
                                        vc = PluginBridges.BridgeFFXIV.GetNamedPartyMember(gindex);
                                    }
                                }
                                if (vc != null)
                                {
                                    if (gprop == "job")
                                    {
                                        var mxx = rexnumparg.Match(x);
                                        if (mxx.Success)
                                        {
                                            string arg1 = mxx.Groups["arg"].Value;
                                            val = PluginBridges.BridgeFFXIV.TranslateJob(vc.GetValue(gprop).ToString(), arg1);
                                            mx = mxx;
                                        }
                                        else
                                        {
                                            val = vc.GetValue(gprop).ToString();
                                        }
                                    }
                                    else if (gprop == "pos")
                                    {
                                        val = vc.GetValue("x") + " , " + vc.GetValue("y");
                                    }
                                    else if (gprop == "posXYZ")
                                    {
                                        val = vc.GetValue("x") + " , " + vc.GetValue("y") + " , " + vc.GetValue("z");
                                    }
                                    else
                                    {
                                        val = vc.GetValue(gprop).ToString();
                                    }
                                }
                            }

                            found = true;
                        }
                        else if (x.IndexOf("_ffxiventity") == 0)
                        {
                            mx = rexnumpnumnum.Match(x);
                            if (mx.Success == true)
                            {
                                string gindex = mx.Groups["index"].Value;
                                string gprop = mx.Groups["prop"].Value;
                                Int64 honk;
                                VariableDictionary vc = null;
                                bool foundid = false;
                                if (Int64.TryParse(gindex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out honk) == true)
                                {
                                    vc = PluginBridges.BridgeFFXIV.GetIdEntity(gindex, out foundid);
                                }
                                if (foundid == false)
                                {
                                    vc = PluginBridges.BridgeFFXIV.GetNamedEntity(gindex);
                                }
                                if (vc != null)
                                {

                                    if (gprop == "memory")
                                    {
                                        int arg1, arg2;
                                        int.TryParse(mx.Groups["arg1"].Value, out arg1);
                                        int.TryParse(mx.Groups["arg2"].Value, out arg2);
                                        byte[] buffer = new byte[arg2];
                                        Int64 ptr = Convert.ToInt64(vc.GetValue("pointer").ToString(), 16);
                                        PluginBridges.BridgeFFXIV.ReadFFXIVMemory((IntPtr)(ptr + arg1), buffer, arg2);
                                        val = "";
                                        for (int iptr = 0; iptr < arg2; iptr += 4)
                                        {
                                            val += BitConverter.ToUInt32(buffer, iptr).ToString("X8") + " ";
                                        }
                                    }
                                    found = true;
                                }

                            }
                            else
                            {
                                mx = rexnump.Match(x);
                                if (mx.Success == true)
                                {
                                    string gindex = mx.Groups["index"].Value;
                                    string gprop = mx.Groups["prop"].Value;
                                    Int64 honk;
                                    VariableDictionary vc = null;
                                    bool foundid = false;
                                    if (Int64.TryParse(gindex, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out honk) == true)
                                    {
                                        vc = PluginBridges.BridgeFFXIV.GetIdEntity(gindex, out foundid);
                                    }
                                    if (foundid == false)
                                    {
                                        vc = PluginBridges.BridgeFFXIV.GetNamedEntity(gindex);
                                    }
                                    if (vc != null)
                                    {
                                        if (gprop == "job")
                                        {
                                            var mxx = rexnumparg.Match(x);
                                            if (mxx.Success)
                                            {
                                                string arg1 = mxx.Groups["arg"].Value;
                                                val = PluginBridges.BridgeFFXIV.TranslateJob(vc.GetValue(gprop).ToString(), arg1);
                                                mx = mxx;
                                            }
                                            else
                                            {
                                                val = vc.GetValue(gprop).ToString();
                                            }
                                        }
                                        else if (gprop == "pos")
                                        {
                                            val = vc.GetValue("x") + " , " + vc.GetValue("y");
                                        }
                                        else if (gprop == "posXYZ")
                                        {
                                            val = vc.GetValue("x") + " , " + vc.GetValue("y") + " , " + vc.GetValue("z");
                                        }
                                        else
                                        {
                                            val = vc.GetValue(gprop).ToString();
                                        }
                                    }
                                }
                                found = true;
                            }
                        }
                        else if (x == "_ffxivtime")
                        {
                            TimeSpan ez = GetEorzeanTime();
                            int mins = (int)Math.Floor(ez.TotalMinutes);
                            val = mins.ToString();
                            found = true;
                        }
                        else if (x == "_lastencounter")
                        {
                            val = plug != null ? plug.LastEncounterHook() : "";
                            found = true;
                        }
                        else if (x == "_activeencounter")
                        {
                            val = plug != null ? plug.ActiveEncounterHook() : "";
                            found = true;
                        }
                        else if (x.IndexOf("_textaura") == 0)
                        {
                            mx = rexnump.Match(x);
                            if (mx.Success == true)
                            {
                                string gindex = mx.Groups["index"].Value;
                                string gprop = mx.Groups["prop"].Value.ToLower();
                                val = "";
                                if (plug.sc != null)
                                {
                                    lock (plug.sc.textitems)
                                    {
                                        Scarborough.ScarboroughText item = plug.sc.GetText(gindex);
                                        if (item != null)
                                        {
                                            switch (gprop)
                                            {
                                                case "x":
                                                    val = item.Left.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "y":
                                                    val = item.Top.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "w":
                                                case "width":
                                                    val = item.Width.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "h":
                                                case "height":
                                                    val = item.Height.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "opacity":
                                                    val = item.Opacity.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "text":
                                                    val = item.Text;
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    lock (plug.textauras)
                                    {
                                        if (plug.textauras.ContainsKey(gindex) == true)
                                        {
                                            Forms.AuraContainerForm acf = plug.textauras[gindex];
                                            switch (gprop)
                                            {
                                                case "x":
                                                    val = acf.Left.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "y":
                                                    val = acf.Top.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "w":
                                                case "width":
                                                    val = acf.Width.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "h":
                                                case "height":
                                                    val = acf.Height.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "opacity":
                                                    val = acf.PresentableOpacity.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "text":
                                                    val = acf.CurrentText;
                                                    break;
                                            }
                                        }
                                    }
                                }
                                found = true;
                            }
                        }
                        else if (x.IndexOf("_imageaura") == 0)
                        {
                            mx = rexnump.Match(x);
                            if (mx.Success == true)
                            {
                                string gindex = mx.Groups["index"].Value;
                                string gprop = mx.Groups["prop"].Value.ToLower();
                                val = "";
                                if (plug.sc != null)
                                {
                                    lock (plug.sc.imageitems)
                                    {
                                        Scarborough.ScarboroughImage item = plug.sc.GetImage(gindex);
                                        if (item != null)
                                        {
                                            switch (gprop)
                                            {
                                                case "x":
                                                    val = item.Left.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "y":
                                                    val = item.Top.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "w":
                                                case "width":
                                                    val = item.Width.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "h":
                                                case "height":
                                                    val = item.Height.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "opacity":
                                                    val = item.Opacity.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    lock (plug.imageauras)
                                    {
                                        if (plug.imageauras.ContainsKey(gindex) == true)
                                        {
                                            Forms.AuraContainerForm acf = plug.imageauras[gindex];
                                            switch (gprop)
                                            {
                                                case "x":
                                                    val = acf.Left.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "y":
                                                    val = acf.Top.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "w":
                                                case "width":
                                                    val = acf.Width.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "h":
                                                case "height":
                                                    val = acf.Height.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                                case "opacity":
                                                    val = acf.PresentableOpacity.ToString(CultureInfo.InvariantCulture);
                                                    break;
                                            }
                                        }
                                    }
                                }
                                found = true;
                            }
                        }
                        else if (x == "_screenwidth")
                        {
                            System.Windows.Forms.Screen scr = System.Windows.Forms.Screen.PrimaryScreen;
                            val = scr.WorkingArea.Width.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_screenheight")
                        {
                            System.Windows.Forms.Screen scr = System.Windows.Forms.Screen.PrimaryScreen;
                            val = scr.WorkingArea.Height.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_screenminx")
                        {
                            val = plug.MinX.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_screenminy")
                        {
                            val = plug.MinY.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_screenmaxx")
                        {
                            val = plug.MaxX.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_screenmaxy")
                        {
                            val = plug.MaxY.ToString(CultureInfo.InvariantCulture);
                            found = true;
                        }
                        else if (x == "_mousex")
                        {
                            RealPlugin.WindowsUtils.POINT point;
                            if (RealPlugin.WindowsUtils.GetCursorPos(out point))
                            {
                                val = point.X.ToString(CultureInfo.InvariantCulture);
                                found = true;
                            }
                        }
                        else if (x == "_mousey")
                        {
                            RealPlugin.WindowsUtils.POINT point;
                            if (RealPlugin.WindowsUtils.GetCursorPos(out point))
                            {
                                val = point.Y.ToString(CultureInfo.InvariantCulture);
                                found = true;
                            }
                        }
                    }
                }
                newexpr = newexpr.Substring(0, m.Index) + val + newexpr.Substring(m.Index + m.Length);
                /*
                if (logger != null)
                {
                    logger(o, I18n.Translate("internal/Context/expansionstep", "Expansion step {0} from '{1}' to '{2}' ({3}) = '{4}'", i, expr, newexpr, val, found));
                }
                else if (plug != null)
                {
                    plug.FilteredAddToLog(Plugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Context/expansionstep", "Expansion step {0} from '{1}' to '{2}' ({3}) = '{4}'", i, expr, newexpr, val, found));
                }*/
                i++;
            };
            if (trig != null)
            {
                if (trig._DebugLevel == RealPlugin.DebugLevelEnum.Inherit)
                {
                    if (plug != null)
                    {
                        if (plug.cfg.DebugLevel < RealPlugin.DebugLevelEnum.Verbose)
                        {
                            return newexpr;
                        }
                    }
                }
                else
                {
                    if (trig._DebugLevel < RealPlugin.DebugLevelEnum.Verbose)
                    {
                        return newexpr;
                    }
                }
            }
            if (plug != null)
            {
                if (plug.cfg.LogVariableExpansions == false)
                {
                    return newexpr;
                }
            }
            if (newexpr.CompareTo(expr) != 0)
            {
                if (logger != null)
                {
                    logger(o, I18n.Translate("internal/Context/expansion", "Variable expansion from '{0}' to '{1}'", expr, newexpr));
                }
                else if (plug != null)
                {
                    plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Context/expansion", "Variable expansion from '{0}' to '{1}'", expr, newexpr));
                }
            }
            return newexpr;
        }

    }

}
