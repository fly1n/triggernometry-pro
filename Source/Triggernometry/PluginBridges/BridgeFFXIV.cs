using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Triggernometry.Variables;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using Advanced_Combat_Tracker;

namespace Triggernometry.PluginBridges
{

    public static class BridgeFFXIV
    {

        private static string ActPluginName = "FFXIV_ACT_Plugin.dll";
        private static string ActPluginType = "FFXIV_ACT_Plugin";
        public static uint ActPluginVersion;
        private static IntPtr hProcessFFXIV;
        public static Dictionary<string, object> ActPlugins = new Dictionary<string, object>();

        private static VariableDictionary NullCombatant = new VariableDictionary();

        internal delegate void LoggingDelegate(RealPlugin.DebugLevelEnum level, string text);
        internal static event LoggingDelegate OnLogEvent;

        public static Int64 LastCheck = 0;
        public static Int64 TickNum = 0;
        public static uint PlayerId = 0;

        public static uint ZoneID = 0;

        public static Configuration cfg;

        public static List<VariableDictionary> PartyMembers = new List<VariableDictionary>(new VariableDictionary[8] {
            new VariableDictionary(), new VariableDictionary(), new VariableDictionary(), new VariableDictionary(),
            new VariableDictionary(), new VariableDictionary(), new VariableDictionary(), new VariableDictionary()
        });
        public static int NumPartyMembers = 0;
        public static int PrevNumPartyMembers = 0;
        public static VariableDictionary Myself;
        public static List<string> OverridePartyOrder = new List<string>(new String[8]{
            "","","","","","","",""
        });
        public static List<VariableDictionary> OverridePartyMembers = new List<VariableDictionary>(new VariableDictionary[8] {
            new VariableDictionary(), new VariableDictionary(), new VariableDictionary(), new VariableDictionary(),
            new VariableDictionary(), new VariableDictionary(), new VariableDictionary(), new VariableDictionary()
        });

        internal static bool ckw = false;

        internal static string ConvertToHex(Int64 x)
        {
            return x.ToString("X8");
        }

        private delegate void NetworkReceiveDelegate(string connection, long epoch, byte[] message);

        public static void ClearCombatant(VariableDictionary vc)
        {
            vc.SetValue("name", "");
            vc.SetValue("currenthp", 0);
            vc.SetValue("currentmp", 0);
            vc.SetValue("currentgp", 0);
            vc.SetValue("currentcp", 0);
            vc.SetValue("maxhp", 0);
            vc.SetValue("maxmp", 0);
            vc.SetValue("maxgp", 0);
            vc.SetValue("maxcp", 0);
            vc.SetValue("level", 0);
            vc.SetValue("jobid", 0);
            vc.SetValue("job", "");
            vc.SetValue("role", "");
            vc.SetValue("x", 0);
            vc.SetValue("y", 0);
            vc.SetValue("z", 0);
            vc.SetValue("id", "");
            vc.SetValue("inparty", 0);
            vc.SetValue("inalliance", 0);
            vc.SetValue("order", 0);
            vc.SetValue("casttargetid", 0);
            vc.SetValue("targetid", 0);
            vc.SetValue("heading", 0);
            vc.SetValue("distance", 0);
            vc.SetValue("worldid", 0);
            vc.SetValue("worldname", "");
            vc.SetValue("currentworldid", 0);

        }

        public static void SetupNullCombatant()
        {
            ClearCombatant(NullCombatant);
        }

        public static void SubscribeToNetworkEvents(RealPlugin p)
        {
            try
            {
                object plug = GetInstance();
                if (plug == null)
                {
                    throw new ArgumentException("No plugin instance available");
                }
                PropertyInfo pi = plug.GetType().GetProperty("DataSubscription", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi == null)
                {
                    throw new ArgumentException("No DataSubscription found");
                }
                dynamic subs = pi.GetValue(plug);
                if (subs == null)
                {
                    throw new ArgumentException("DataSubscription not initialized");
                }
                EventInfo ei = subs.GetType().GetEvent("ParsedLogLine", BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
                if (ei == null)
                {
                    throw new ArgumentException("No ParsedLogLine found");
                }
                MethodInfo mix = p.GetType().GetMethod("NetworkLogLineReceiver");
                Type deltype = ei.EventHandlerType;
                Delegate handler = Delegate.CreateDelegate(deltype, p, mix);
                ei.AddEventHandler(subs, handler);
                LogMessage(RealPlugin.DebugLevelEnum.Info, I18n.Translate("internal/ffxiv/networksubok", "Subscribed to FFXIV network events"));
                ei = subs.GetType().GetEvent("ZoneChanged", BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
                if (ei != null)
                {
                    mix = p.GetType().GetMethod("ZoneChangeDelegate");
                    deltype = ei.EventHandlerType;
                    handler = Delegate.CreateDelegate(deltype, p, mix);
                    ei.AddEventHandler(subs, handler);                    
                }
                else
                {
                    LogMessage(RealPlugin.DebugLevelEnum.Error, "No ZoneChanged found");
                }
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/networksubexception", "Could not subscribe to FFXIV network events due to an exception: {0}", ex.Message));
            }
        }

        public static void UnsubscribeFromNetworkEvents(RealPlugin p)
        {
            try
            {
                object plug = GetInstance();
                if (plug == null)
                {
                    throw new ArgumentException("No plugin instance available");
                }
                PropertyInfo pi = plug.GetType().GetProperty("DataSubscription", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (pi == null)
                {
                    throw new ArgumentException("No DataSubscription found");
                }
                dynamic subs = pi.GetValue(plug);
                if (subs == null)
                {
                    throw new ArgumentException("DataSubscription not initialized");
                }
                EventInfo ei = subs.GetType().GetEvent("ParsedLogLine", BindingFlags.GetField | BindingFlags.Public | BindingFlags.Instance);
                if (subs == null)
                {
                    throw new ArgumentException("No ParsedLogLine found");
                }
                MethodInfo mix = p.GetType().GetMethod("NetworkLogLineReceiver");
                Type deltype = ei.EventHandlerType;
                Delegate handler = Delegate.CreateDelegate(deltype, p, mix);
                ei.RemoveEventHandler(subs, handler);
                LogMessage(RealPlugin.DebugLevelEnum.Info, I18n.Translate("internal/ffxiv/networkunsubok", "Unsubscribed from FFXIV network events"));
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/networkunsubexception", "Could not unsubscribe from FFXIV network events due to an exception: {0}", ex.Message));
            }
        }

        private static void LogMessage(RealPlugin.DebugLevelEnum level, string message)
        {
            if (OnLogEvent != null)
            {
                OnLogEvent(level, message);
            }
        }

        public static string TranslateJob(string id)
        {
            switch (id)
            {
                // JOBS
                case "33": return "AST";
                case "23": return "BRD";
                case "25": return "BLM";
                case "36": return "BLU";
                case "38": return "DNC";
                case "32": return "DRK";
                case "22": return "DRG";
                case "37": return "GNB";
                case "31": return "MCH";
                case "20": return "MNK";
                case "30": return "NIN";
                case "19": return "PLD";
                case "35": return "RDM";
                case "34": return "SAM";
                case "28": return "SCH";
                case "27": return "SMN";
                case "21": return "WAR";
                case "24": return "WHM";
                // CRAFTERS
                case "14": return "ALC";
                case "10": return "ARM";
                case "9": return "BSM";
                case "8": return "CRP";
                case "15": return "CUL";
                case "11": return "GSM";
                case "12": return "LTW";
                case "13": return "WVR";
                // GATHERERS
                case "17": return "BTN";
                case "18": return "FSH";
                case "16": return "MIN";
                // CLASSES
                case "26": return "ACN";
                case "5": return "ARC";
                case "6": return "CNJ";
                case "1": return "GLA";
                case "4": return "LNC";
                case "3": return "MRD";
                case "2": return "PUG";
                case "29": return "ROG";
                case "7": return "THM";
            }
            return "";
        }

        public static string TranslateRole(string id)
        {
            switch (id)
            {
                // JOBS
                case "33": return "Healer";
                case "23": return "DPS";
                case "25": return "DPS";
                case "36": return "DPS";
                case "38": return "DPS";
                case "32": return "Tank";
                case "22": return "DPS";
                case "37": return "Tank";
                case "31": return "DPS";
                case "20": return "DPS";
                case "30": return "DPS";
                case "19": return "Tank";
                case "35": return "DPS";
                case "34": return "DPS";
                case "28": return "Healer";
                case "27": return "DPS";
                case "21": return "Tank";
                case "24": return "Healer";
                // CRAFTERS
                case "14": return "Crafter";
                case "10": return "Crafter";
                case "9": return "Crafter";
                case "8": return "Crafter";
                case "15": return "Crafter";
                case "11": return "Crafter";
                case "12": return "Crafter";
                case "13": return "Crafter";
                // GATHERERS
                case "17": return "Gatherer";
                case "18": return "Gatherer";
                case "16": return "Gatherer";
                // CLASSES
                case "26": return "Healer";
                case "5": return "DPS";
                case "6": return "Healer";
                case "1": return "Tank";
                case "4": return "DPS";
                case "3": return "Tank";
                case "2": return "DPS";
                case "29": return "DPS";
                case "7": return "DPS";
            }
            return "";
        }

        public static string TranslateJob(string strIn,string strType)
        {
            int[] dictindex = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 9 };
            int returnIndex = 5;
            switch (strType)
            {
                case "ID":returnIndex = 0;break;
                case "CN": returnIndex = 1; break;
                case "TCN": returnIndex = 2; break;
                case "CN2": returnIndex = 3; break;
                case "CN1": returnIndex = 4; break;

                case "EN": returnIndex = 5; break;
                case "EN3": returnIndex = 6; break;
                case "JP": returnIndex = 7; break;
                case "JP2": returnIndex = 8; break;
                case "JP1": returnIndex = 9; break;

                case "RoleEN": returnIndex = 10; break;
                case "RoleEN1": returnIndex = 11; break;
                case "RoleCN": returnIndex = 12; break;
                case "SubRoleEN": returnIndex = 13; break;
                case "MacroPos": returnIndex = 14; break;

                default: break;
            }
            foreach(int j in dictindex)
            {
                for (int i = 0; i < joblist.GetLength(0); i++)
                {
                    if (strIn == joblist[i,j]) return joblist[i, returnIndex];
                }
            }
            return "";
        }
        public static void PopulateClumpFromCombatant(VariableDictionary vc, dynamic cmx, int inParty, int inAlliance, int orderNum)
        {
            if (cmx.ID == 0) return;
            vc.SetValue("id", ConvertToHex(cmx.ID));
            vc.SetValue("ownerid", ConvertToHex(cmx.OwnerID));
            vc.SetValue("type", cmx.type);
            vc.SetValue("jobid", cmx.Job);
            vc.SetValue("level", cmx.Level);
            if (cmx.Name != null)
            {
                vc.SetValue("name", cmx.Name);
            }
            else {
                vc.SetValue("name", "");
            }
            vc.SetValue("currenthp", cmx.CurrentHP);
            vc.SetValue("maxhp", cmx.MaxHP);
            vc.SetValue("currentmp", cmx.CurrentMP);
            vc.SetValue("maxmp", cmx.MaxMP);
            vc.SetValue("currentcp", cmx.CurrentCP);
            vc.SetValue("maxcp", cmx.MaxCP);
            vc.SetValue("currentgp", cmx.CurrentGP);
            vc.SetValue("maxgp", cmx.MaxGP);
            vc.SetValue("iscasting", Convert.ToInt32(cmx.IsCasting));
            vc.SetValue("castbuffid", ConvertToHex(cmx.CastBuffID));
            if (cmx.IsCasting == true)
            {
                vc.SetValue("casttargetid", ConvertToHex(cmx.CastTargetID));
            }
            else
            {
                vc.SetValue("casttargetid", 0);
            }

            vc.SetValue("castdurationcurrent", cmx.CastDurationCurrent);
            vc.SetValue("castdurationmax", cmx.CastDurationMax);
            vc.SetValue("x", cmx.PosX);
            vc.SetValue("y", cmx.PosY);
            vc.SetValue("z", cmx.PosZ);
            vc.SetValue("heading", cmx.Heading);
            vc.SetValue("currentworldid", cmx.CurrentWorldID);
            vc.SetValue("worldid", cmx.WorldID);
            vc.SetValue("worldname", cmx.WorldName);
            vc.SetValue("bnpcnameid", ConvertToHex(cmx.BNpcNameID));
            vc.SetValue("bnpcid", ConvertToHex(cmx.BNpcID));
            if (cmx.TargetID > 0)
            {
                vc.SetValue("targetid", ConvertToHex(cmx.TargetID));
            }
            else
            {
                vc.SetValue("targetid", 0);
            }
            vc.SetValue("job", TranslateJob(cmx.Job.ToString()));
            vc.SetValue("role", TranslateRole(cmx.Job.ToString()));
            vc.SetValue("inparty", inParty);
            vc.SetValue("inalliance", inAlliance);
            vc.SetValue("order", orderNum);
            vc.SetValue("distance", cmx.EffectiveDistance);
            if(BridgeFFXIV.ActPluginVersion<2604)
                vc.SetValue("pointer", cmx.Pointer.ToString("X12"));
            else
                vc.SetValue("pointer", cmx.Address.ToString("X12"));
        }
        public static object GetPluginObject(string ActPluginName, string ActPluginStatus)
        {
            if (!ActPlugins.ContainsKey(ActPluginName))
            {
                RealPlugin.PluginWrapper wrap= RealPlugin.PluginObjectHook(ActPluginName, ActPluginStatus);
                Object obj = wrap.pluginObj;
                ActPlugins.Add(ActPluginName, obj);
                return obj;
            }
            else
            {
                return ActPlugins[ActPluginName];
            }
        }
        public static object GetInstance()
        {            
            RealPlugin.PluginWrapper wrap = RealPlugin.InstanceHook(ActPluginName, ActPluginType);
            uint.TryParse(wrap.fileversion.Replace(".",""),out ActPluginVersion);
            switch (wrap.state)
            {
                case 0:
                    {
                        if (ckw == false)
                        {
                            LogMessage(RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/ffxiv/missingactplugin", "FFXIV ACT plugin with filename ({0}) or type ({1}) could not be located, some functions may not work as expected", ActPluginName, ActPluginType));
                            ckw = true;
                        }
                        return null;
                    }
                case 1:
                    {
                        return wrap.pluginObj;
                    }
                case 2:
                    {
                        LogMessage(RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/ffxiv/oldactplugin", "FFXIV ACT plugin version is lower ({0}) than expected ({1}), some functions may not work as expected", wrap.fileversion, wrap.expectedversion));
                        return wrap.pluginObj;
                    }
            }
            return null;
        }

        private static PropertyInfo GetDataRepository(object plug)
        {
            return plug.GetType().GetProperty("DataRepository", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        private static PropertyInfo GetDataSubscription(object plug)
        {
            return plug.GetType().GetProperty("DataSubscription", BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }



        private class CombatantData
        {

            public object Lock { get; set; }
            public dynamic Combatants { get; set; }

        }

        private static CombatantData GetCombatants(object plug, PropertyInfo reprop)
        {
            if (reprop == null)
            {
                // use _Memory
                FieldInfo fi = plug.GetType().GetField("_Memory", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic mmry = fi.GetValue(plug);
                if (mmry == null)
                {
                    return null;
                }
                fi = mmry.GetType().GetField("_config", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic cnfg = fi.GetValue(mmry);
                if (cnfg == null)
                {
                    return null;
                }
                fi = cnfg.GetType().GetField("ScanCombatants", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic cmbt = fi.GetValue(cnfg);
                if (cmbt == null)
                {
                    return null;
                }
                fi = cmbt.GetType().GetField("_CurrentPlayerID", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                PlayerId = fi.GetValue(cmbt);
                CombatantData cd = new CombatantData();
                fi = cmbt.GetType().GetField("_Combatants", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                cd.Combatants = fi.GetValue(cmbt);
                fi = cmbt.GetType().GetField("_CombatantsLock", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                cd.Lock = fi.GetValue(cmbt);
                return cd;
            }
            else
            {
                // use DataRepository
                MethodInfo mi = reprop.GetGetMethod();
                object o = mi.Invoke(plug, null);
                mi = o.GetType().GetMethod("GetCurrentPlayerID", BindingFlags.Instance | BindingFlags.Public);
                PlayerId = (uint)mi.Invoke(o, null);
                mi = o.GetType().GetMethod("GetCombatantList", BindingFlags.Instance | BindingFlags.Public);
                CombatantData cd = new CombatantData();
                cd.Combatants = mi.Invoke(o, null);
                cd.Lock = cd;
                return cd;
            }
        }

        public static void UpdateState()
        {
            int phase = 0;
            try
            {
                Int64 old = Interlocked.Read(ref LastCheck);
                Int64 now = DateTime.Now.Ticks;
                if (((now - old) / TimeSpan.TicksPerMillisecond) < 100)
                {
                    return;
                }
                Interlocked.Exchange(ref LastCheck, now);
                object plug = null;
                phase = 99;
                plug = GetInstance();
                if (plug == null)
                {
                    return;
                }
                phase = 1;
                PropertyInfo pi = GetDataRepository(plug);
                phase = 2;
                CombatantData cd = GetCombatants(plug, pi);
                phase = 3;
                lock (cd.Lock)
                {
                    int ex = 0;
                    List<VariableDictionary> newCombatants = new List<VariableDictionary>();
                    foreach (dynamic cmx in cd.Combatants)
                    {
                        int nump;
                        try
                        {
                            nump = (int)cmx.PartyType;
                        }
                        catch (Exception)
                        {
                            nump = 0;
                        }
                        //override party list
                        if (cmx.Name != "")
                        {
                            int override_index = OverridePartyOrder.IndexOf(cmx.Name);
                            if (override_index >= 0)
                            {

                                PopulateClumpFromCombatant(OverridePartyMembers[override_index], cmx, 1, nump == 2 ? 1 : 0, override_index + 1);
                                continue;
                            }
                        }

                        if (cmx.ID == PlayerId || nump == 1)
                        {
                            /*
                            if (ex >= PartyMembers.Count)
                            {
                                throw new InvalidOperationException(I18n.Translate("internal/ffxiv/partytoobig", "Party structure has more than {0} members", PartyMembers.Count));
                            }
                            */
                            phase = 4;

                            phase = 5;
                            VariableDictionary vd = new VariableDictionary();
                            PopulateClumpFromCombatant(vd, cmx, 1, nump == 2 ? 1 : 0, newCombatants.Count+1);
                            newCombatants.Add(vd);
                            if (cmx.ID == PlayerId)
                            {
                                if (Myself == null) Myself = new VariableDictionary();
                                Myself.CopyFrom(vd);
                            }
                            phase = 6;
                            for (int i = 0; i < newCombatants.Count-1; i++)
                            {
                                phase = 16;
                                phase = 17;
                                if (newCombatants[newCombatants.Count-1].CompareTo(newCombatants[i]) == 0)
                                {
                                    phase = 18;
                                    //newCombatants.RemoveAt(newCombatants.Count - 1);
                                    phase = 19;
                                    break;
                                }
                            }
                            phase = 20;
                            if (newCombatants.Count >= PartyMembers.Count)
                            {
                                // full party found
                                break;
                            }
                        }   
                    }
                    //fill empty party members
                    /*
                    while (ex < PartyMembers.Count)
                    {
                        if (OverridePartyOrder[ex] =="") BridgeFFXIV.ClearCombatant(PartyMembers[ex]);
                        ex++;
                    }
                    */
                    phase = 7;
                    NumPartyMembers = newCombatants.Count;
                    /*
                    if (PrevNumPartyMembers > NumPartyMembers)
                    {
                        for (int i = NumPartyMembers; i < PrevNumPartyMembers; i++)
                        {
                            ClearCombatant(PartyMembers[i]);
                        }
                    }
                    */
                    
                    phase = 8;

                    if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomSelfFirst)
                    {
                        newCombatants.Sort(SortPlayersSelf);
                    }
                    else if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomFull)
                    {
                        newCombatants.Sort(SortPlayers);
                    }
                    for (
                        int i = 0; i < PartyMembers.Count; i++)
                    {
                        if (newCombatants.Count <= i)
                        {
                            newCombatants.Add(new VariableDictionary());
                        }
                        if (OverridePartyOrder[i] != "")
                        {
                            newCombatants.Insert(i, OverridePartyMembers[i]);
                        }
                        PartyMembers[i].CopyFrom(newCombatants[i]);
                        
                    }
                    NumPartyMembers = 8;
                    int ro = 1;
                    foreach (VariableDictionary vc in PartyMembers)
                    {
                        vc.SetValue("order", "" + ro);
                        ro++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/updateexception", "Exception in FFXIV state update: {0} at stage {1}", ex.Message, phase));
            }
        }

        /*private static void DebugPlayerSorting(string header, IEnumerable<VariableClump> vc)
        {
            int ro = 1;
            foreach (VariableClump a in vc)
            {
                System.Diagnostics.Debug.WriteLine(header + ": " + ro + " -- " + a.GetValue("name") + ", " + a.GetValue("job") + " --> " + a.GetValue("order") + " / " + cfg.GetPartyOrderValue(a.GetValue("jobid")));
                ro++;
            }
        }*/
        public static void SplitNameServer(string name_server, out string out_name, out string out_server) {
            out_name = name_server;
            out_server = "";
            //try to match player in partylist
            for (int pid = 1; pid < PluginBridges.BridgeFFXIV.NumPartyMembers + 1; pid++)
            {
                VariableDictionary p = PluginBridges.BridgeFFXIV.GetPartyMember(pid);
                var pname = p.GetValue("name").ToString();
                if (pname == "") continue;
                if (name_server.Contains(pname))
                {
                    var sname = name_server.Replace(pname, "");
                    if (serverlist.Contains(sname))
                    {
                        out_name = pname;
                        out_server = sname;
                        break;
                    }
                }
            }
            //if not matched, trim the first server name found.
            foreach (var sname in PluginBridges.BridgeFFXIV.serverlist)
            {
                if (name_server.EndsWith(sname))
                {
                    out_name = name_server.Substring(0, name_server.Length - sname.Length);
                    out_server = sname;
                    break;
                }
            }


        }
        public static int SortPlayersSelf(VariableDictionary a, VariableDictionary b)
        {
            if (a == Myself && b != Myself)
            {
                //System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (ME) < " + b.GetValue("name"));
                return -1;
            }
            if (b == Myself && a != Myself)
            {
                //System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " > " + b.GetValue("name") + " (ME)");
                return 1;
            }
            return SortPlayers(a, b);
        }

        public static int SortPlayers(VariableDictionary a, VariableDictionary b)
        {
            string name_a = a.GetValue("name").ToString();
            string name_b = b.GetValue("name").ToString();
            int av = -1;
            int bv = -1;
            if (name_a.Length > 0 && name_b.Length > 0)
            {
                try
                {
                    av = OverridePartyOrder.IndexOf(name_a);
                    bv = OverridePartyOrder.IndexOf(name_b);
                }catch (Exception e)
                {
                    av = -1;
                    bv = -1;
                }
            }
            if ((av >= 0) && (bv >= 0))
            {
                if (av < bv)
                {
                    return -1;
                }
                if (av > bv)
                {
                    return 1;
                }
            }
            av = cfg.GetPartyOrderValue(a.GetValue("jobid").ToString());
            bv = cfg.GetPartyOrderValue(b.GetValue("jobid").ToString());
            if (av < bv)
            {
                //System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") < " + b.GetValue("name") + " (" + bv + ")");
                return -1;
            }
            if (av > bv)
            {
                //System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") > " + b.GetValue("name") + " (" + bv + ")");
                return 1;
            }
            //System.Diagnostics.Debug.WriteLine(a.GetValue("name") + " (" + av + ") -(" + a.GetValue("name").CompareTo(b.GetValue("name")) + ")- " + b.GetValue("name") + " (" + bv + ")");
            // https://github.com/paissaheavyindustries/Triggernometry/issues/9
            return b.GetValue("id").CompareTo(a.GetValue("id"));
        }

        public static VariableDictionary GetNamedEntity(string name)
        {            
            try
            {
                object plug = null;
                plug = GetInstance();
                if (plug == null)
                {
                    return NullCombatant;
                }
                PropertyInfo pi = GetDataRepository(plug);
                CombatantData cd = GetCombatants(plug, pi);
                lock (cd.Lock)
                {
                    foreach (dynamic cmx in cd.Combatants)
                    {
                        if (cmx.Name == null) continue;
                        if (cmx.Name.ToLower() == name.ToLower())
                        {
                            int nump = 0;
                            try
                            {
                                nump = (int)cmx.PartyType;
                            }
                            catch (Exception)
                            {
                            }
                            VariableDictionary vc = new VariableDictionary();
                            PopulateClumpFromCombatant(vc, cmx, nump == 1 ? 1 : 0, nump == 2 ? 1 : 0, 0);
                            return vc;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/namedexception", "Exception in FFXIV named entity retrieve: {0}", ex.Message));
            }
            return NullCombatant;
        }

        public static VariableDictionary GetIdEntity(string id, out bool found)
        {
            
            found = false;
            try
            {
                object plug = null;
                plug = GetInstance();
                if (plug == null)
                {
                    return NullCombatant;
                }
                PropertyInfo pi = GetDataRepository(plug);
                CombatantData cd = GetCombatants(plug, pi);
                lock (cd.Lock)
                {
                    foreach (dynamic cmx in cd.Combatants)
                    {
                        if (String.Compare(ConvertToHex(cmx.ID), id, true) == 0)
                        {
                            int nump = 0;
                            try
                            {
                                nump = (int)cmx.PartyType;
                            }
                            catch (Exception)
                            {
                            }
                            VariableDictionary vc = new VariableDictionary();
                            PopulateClumpFromCombatant(vc, cmx, nump == 1 ? 1 : 0, nump == 2 ? 1 : 0, 0);
                            found = true;
                            return vc;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/idexception", "Exception in FFXIV ID entity retrieve: {0}", ex.Message));
            }
            return NullCombatant;
        }

        public static VariableDictionary GetPartyMember(int index)
        {
            UpdateState();
            if (index < 1 || index > NumPartyMembers)
            {
                return NullCombatant;
            }
            return PartyMembers[index - 1];
        }

        public static VariableDictionary GetMyself()
        {
            UpdateState();
            return Myself;
        }

        public static VariableDictionary GetNamedPartyMember(string name)
        {
            UpdateState();
            foreach (VariableDictionary vc in PartyMembers)
            {
                if (vc.GetValue("name").ToString().ToLower() == name.ToLower())
                {
                    return vc;
                }
            }
            return NullCombatant;
        }

        public static VariableDictionary GetIdPartyMember(string id, out bool found)
        {
            found = false;
            UpdateState();
            foreach (VariableDictionary vc in PartyMembers)
            {
                if (String.Compare(vc.GetValue("id").ToString(), id, true) == 0)
                {
                    found = true;
                    return vc;
                }
            }
            return NullCombatant;
        }
        public static void SetOverridePartyOrder(string name, int order) {
            if (order > 8) return;
            if (order <= 0) return;
            OverridePartyOrder[order - 1] = name;
        }

        public static Process GetProcess()
        {
            try
            {
                object plug = GetInstance();
                if (plug == null)
                {
                    return null;
                }
                PropertyInfo pi = GetDataRepository(plug);
                if (pi == null)
                {
                    return null;
                }
                MethodInfo mi = pi.GetGetMethod();
                object o = mi.Invoke(plug, null);
                mi = o.GetType().GetMethod("GetCurrentFFXIVProcess", BindingFlags.Instance | BindingFlags.Public);
                return (Process)mi.Invoke(o, null);
            }
            catch (Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/procexception", "Exception in FFXIV process retrieve: {0}", ex.Message));
            }
            return null;
        }

        public static int GetProcessId()
        {
            Process p = GetProcess();
            if (p == null)
            {
                return 0;
            }            
            return p.Id;
        }

        public static string GetProcessName()
        {
            Process p = GetProcess();
            if (p == null)
            {
                return "";
            }
            return p.ProcessName;
        }
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetModuleInformation(IntPtr hProcess, IntPtr hModule, out ModuleInfo lpmodinfo, uint cb);

        [DllImport("psapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int EnumProcessModules(IntPtr hProcess, [Out] IntPtr lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern int GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, StringBuilder lpFilename, int nSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct ModuleInfo
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        private const ProcessAccessFlags ProcessFlags =
            ProcessAccessFlags.VirtualMemoryRead
            | ProcessAccessFlags.VirtualMemoryWrite
            | ProcessAccessFlags.VirtualMemoryOperation
            | ProcessAccessFlags.QueryInformation;
        public static void ReadFFXIVMemory(IntPtr ptr, byte[] buffer, int length)
        {
            try
            {
                var new_pid = GetProcessId();
                if (hProcessFFXIV == IntPtr.Zero)
                {
                    hProcessFFXIV = OpenProcess(ProcessFlags, false, new_pid);

                }

                IntPtr read;
                ReadProcessMemory(hProcessFFXIV, ptr, buffer, length, out read);

            }
            catch (Exception e)
            {
                hProcessFFXIV = IntPtr.Zero;
            }

        }
        public static string GetFFXIVBaseAddress(int id)
        {
            Process p = GetProcess();
            try {
                string baseAddress = p.Modules[id].BaseAddress.ToString("X16") ;
                return baseAddress;
            }
            catch (Exception e)
            {
                return "";
            }
            return "";
        }
        public static void CloseHandleFFXIV()
        {
            if (hProcessFFXIV != IntPtr.Zero)
            {
                CloseHandle(hProcessFFXIV);
            }
        }
        public static string GetFFXIVSignature64(IntPtr addr)
        {
            string outstr = "";
            Process p = GetProcess();
            try
            {
                IntPtr baseAddress = p.Modules[0].BaseAddress;
                Int64 addr_offset = ((Int64)addr) - ((Int64)baseAddress);
                var len = p.Modules[0].ModuleMemorySize;
                var buffer = new byte[len];
                ReadFFXIVMemory(baseAddress, buffer, len);
                for (int i = 0; i+3< len; i++)
                {
                    if(i + BitConverter.ToUInt32(buffer,i) +4 == addr_offset)
                    {
                        outstr += (baseAddress+i).ToString("X16")+"("+ (i).ToString("X8") + ")" +" ";
                    }
                }
            }
            catch (Exception e)
            {
                return "";
            }
            return outstr;
        }
        public static string GetFFXIVSignature(string sigstr, string sigtype, string addresstype)
        {
            byte[] sig_buffer = null;
            try
            {
                switch (sigtype)
                {
                    case "byte":
                        {
                            byte sig=byte.Parse(sigstr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sig_buffer = new byte[1] { sig };
                        }
                        break;
                    case "uint16":
                        {
                            UInt16 sig = UInt16.Parse(sigstr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sig_buffer = BitConverter.GetBytes(sig);
                        }
                        break;
                    case "uint32":
                        {
                            UInt32 sig=UInt32.Parse(sigstr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sig_buffer = BitConverter.GetBytes(sig);
                        }
                        break;
                    case "uint64":
                        {
                            UInt64 sig=UInt64.Parse(sigstr, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            sig_buffer = BitConverter.GetBytes(sig);
                        }
                        break;
                    case "array":
                        {
                            sig_buffer = new byte[sigstr.Length / 2];
                            for (int i = 0; i < sigstr.Length; i += 2)
                            {
                                sig_buffer[i / 2]=byte.Parse(sigstr.Substring(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                    default: break;
                }
            }catch(Exception ex)
            {
                LogMessage(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/ffxiv/findsignatureexception", "Invalid FFXIV signature: {0}", ex.Message));
                return "";
            }
            if (sig_buffer == null) return "";


            string outstr = "";
            Process p = GetProcess();
            //try
            //{
            byte[] buffer;
            IntPtr baseAddress=IntPtr.Zero;
            UInt64 len=0;
            switch (addresstype)
            {
                case "2": { baseAddress = IntPtr.Zero; len = (UInt64) p.MainModule.BaseAddress + (UInt64)p.MainModule.ModuleMemorySize; } break;
                case "1": { baseAddress = IntPtr.Zero; len = (UInt64) p.MainModule.BaseAddress; }break;
                case "0": { baseAddress = p.MainModule.BaseAddress; len = (UInt64) p.MainModule.ModuleMemorySize; } break;
                default:break;

            }
            buffer = new byte[len];
            //ReadFFXIVMemory(baseAddress, buffer, len);
            //for (int i = 0; i + 4 < len; i++)
            //    if (BitConverter.ToUInt16(buffer, i) == sig)
            //        outstr += (baseAddress + i).ToString("X16") + "(" + (i).ToString("X8") + ")" + " ";
            //}
            //catch (Exception e)
            //{
            //    return "";
            //}
            return outstr;
        }
        public static string GetFFXIVSignature32(UInt32 sig, IntPtr baseAddress, int len)
        {
            string outstr = "";
            Process p = GetProcess();
            //try
            //{
                var buffer = new byte[len];
                ReadFFXIVMemory(baseAddress, buffer, len);
                for (int i = 0; i+8  < len; i++)
                    if (BitConverter.ToUInt32(buffer, i) == sig)
                        outstr += (baseAddress + i).ToString("X16") + "(" + (i).ToString("X8") + ")" + " ";
            //}
            //catch (Exception e)
            //{
            //    return "";
            //}
            return outstr;
        }
        public static string GetFFXIVSignature64(UInt64 sig, IntPtr baseAddress, int len)
        {
            string outstr = "";
            Process p = GetProcess();
            //try
            //{
                var buffer = new byte[len];
                ReadFFXIVMemory(baseAddress, buffer, len);
                for (int i = 0; i + 16 < len; i++)
                    if (BitConverter.ToUInt64(buffer, i) == sig)
                        outstr += (baseAddress + i).ToString("X16") + "(" + (i).ToString("X8") + ")" + " ";
            //}
            //catch (Exception e)
            //{
            //    return "";
            //}
            return outstr;
        }
        private static dynamic setPropertyWithJson(dynamic prop, dynamic obj,string json)
        {
            dynamic value = prop.GetValue(obj);
            Type type;
            try
            {
                type = prop.PropertyType;
            }
            catch (Exception e)
            {
                type = prop.FieldType;
            }

            try
            {
                dynamic new_value2 = Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
                prop.SetValue(obj, new_value2);
            }
            catch (Exception e)
            {
                if (type.Name == "Server_MessageType")
                {
                    MethodInfo mi = obj.GetMethod("op_Implicit", new Type[] { typeof(UInt16) });
                    object obj2 = type.Assembly.CreateInstance(type.FullName);

                    ushort num2 = ushort.Parse(json);
                    dynamic newMessageType = mi.Invoke(obj2, new object[] { num2 });
                    prop.SetValue(obj2, newMessageType);
                    //value.InternalValue = UInt16.Parse(json);
                    //PropertyInfo pi =value.GetType().GetProperty("InternalValue", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                    //pi.SetValue(value, UInt16.Parse(json));
                    //prop.SetValue(obj, value);
                }
            }
            return 0;
        }
        public static object ReflectPlugin(string command,string new_value="")
        {
            Regex rex1= new Regex(@"(?<method>[^\[]+)\[(?<name>.+?)\](?<others>.*)");
            Regex rex2= new Regex(@"\.(?<method>[^\[]+)\[(?<name>.+?)\](?<others>.*)");

            var mx = rex1.Match(command);
            if (mx.Success == false) return null;
            //string reflectMethod = mx.Groups["method"].Value;
            string reflectMethod = mx.Groups["method"].Value;
            string pluginName = mx.Groups["name"].Value;
            string pluginStatus = pluginName.Split(',')[1];
            pluginName = pluginName.Split(',')[0];
            object currentObj = GetPluginObject(pluginName, pluginStatus);
            if (currentObj == null) return null;

            string others = mx.Groups["others"].Value;
            Match my = rex2.Match(others);

            dynamic currentValue = null;
            string currentMethod = "f";
            while (my.Success == true)
            {
                currentMethod = my.Groups["method"].Value;
                var sp=my.Groups["name"].Value.Split(',');
                string name;
                string type="";
                if (sp.Length > 1)
                {
                    name = sp[1];
                    type = sp[0];
                }
                else {
                    name = sp[0];
                }

                others = my.Groups["others"].Value;
                bool isValue = false;
                bool isSet = false;
                if (currentMethod.EndsWith("v"))
                {
                    isValue = true;
                    currentMethod=currentMethod.TrimEnd('v');
                }else if (currentMethod.EndsWith("s"))
                {
                    isSet = true;
                    currentMethod = currentMethod.TrimEnd('s');
                }
                switch (currentMethod)
                {
                    case "af":
                        {
                            Assembly ass = currentObj.GetType().Assembly;
                            FieldInfo fi = ass.GetType(type).GetField(name, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = isValue? fi.GetValue(ass.GetType(type)) : fi;
                            if (isSet)setPropertyWithJson(fi, ass.GetType(type) , new_value);
                        }
                        break;
                    case "ap":
                        {
                            Assembly ass = currentObj.GetType().Assembly;
                            PropertyInfo pi = ass.GetType(type).GetProperty(name, BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = isValue ? pi.GetValue(ass.GetType(type)) : pi;
                            if (isSet) setPropertyWithJson(pi, ass.GetType(type), new_value);
                        }
                        break;
                    case "am":
                        {
                            Assembly ass = currentObj.GetType().Assembly;
                            MethodInfo mi = ass.GetType(type).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = mi;
                            mi.Invoke(currentObj, new object[0]);
                        }
                        break;
                    case "ac":
                        {
                            Assembly ass = currentObj.GetType().Assembly;
                            ConstructorInfo ci= ass.GetType(type).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)[0];
                            ci.Invoke(ass.GetType(type),new object[0]);
                        }
                        break;
                    case "f":
                        {
                            FieldInfo fi = currentObj.GetType().GetField(name, BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = isValue ? fi.GetValue(currentObj): fi;
                            if (isSet) setPropertyWithJson(fi, currentObj, new_value);
                        }
                        break;
                    case "p":
                        {
                            PropertyInfo pi = currentObj.GetType().GetProperty(name, BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = isValue ? pi.GetValue(currentObj): pi;
                            if (isSet) setPropertyWithJson(pi, currentObj, new_value);
                        }
                        break;
                    case "m":
                        {
                            MethodInfo mi = currentObj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = mi;
                            mi.Invoke(currentObj,new object[0]);
                        }
                        break;
                    case "e":
                        {
                            EventInfo ei = currentObj.GetType().GetEvent(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                            currentValue = ei;
                        }
                        break;
                    default:break;
                }
                if (currentValue == null) break;
                my = rex2.Match(others);
                currentObj = currentValue;
            }

            return currentObj;

        }
        public static void SetFFXIVSignature(string typeName,uint offset)
        {
            FFXIV_ACT_Plugin.Memory.SignatureType sigType = (FFXIV_ACT_Plugin.Memory.SignatureType)Enum.Parse(typeof(FFXIV_ACT_Plugin.Memory.SignatureType), typeName);
            var plug = PluginBridges.BridgeFFXIV.GetInstance();
            FieldInfo fi = plug.GetType().GetField("_dataCollection", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            dynamic dataCollection = fi.GetValue(plug);
            fi = dataCollection.GetType().GetField("_monitor", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            dynamic signatureManager;
            if (fi == null)
            {
                fi = plug.GetType().GetField("_actUIMods", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic actUIMods = fi.GetValue(plug);
                fi = actUIMods.GetType().GetField("_settingsPropertyPage", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic settingsPropertyPage = fi.GetValue(actUIMods);
                fi = settingsPropertyPage.GetType().GetField("_signatureManager", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                signatureManager = fi.GetValue(settingsPropertyPage);
            }
            else
            {
                dynamic monitor = fi.GetValue(dataCollection);
                fi = monitor.GetType().GetField("_diagnosisHelper", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                dynamic diagnosisHelper = fi.GetValue(monitor);
                fi = diagnosisHelper.GetType().GetField("_signatureManager", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
                signatureManager = fi.GetValue(diagnosisHelper);
            }            fi = signatureManager.GetType().GetField("_signatures", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
            //var signatures = (Dictionary<FFXIV_ACT_Plugin.Memory.SignatureType, uint>)fi.GetValue(signatureManager);
            dynamic signatures = fi.GetValue(signatureManager);
            //var signatures_parsed = (Dictionary<SignatureType, uint>)signatures;
            //Type t = signatures.GetType();
            signatures[sigType] = offset;
            //signatures = (t)signatures_parsed;
            fi.SetValue(signatureManager, signatures);
        }
        public static string[,] joblist = new string[,] {
                {"0",   "冒险者",  "冒險者",  "冒险",   "冒",    "adventurer",   "ADV",  "すっぴん士",    "冒",    "冒",    "None", "N",    "冒险",   "adventurer",   "None"},
                {"1",   "剑术师",  "劍術師",  "剑术",   "剑",    "gladiator",    "GLA",  "剣術士",  "剣",    "剣",    "Tank", "T",    "坦克",   "Tank", "ST"},
                {"2",   "格斗家",  "格鬥家",  "格斗",   "格",    "pugilist", "PGL",  "格闘士",  "格",    "格",    "DPS",  "D",    "近战",   "Melee",    "D1"},
                {"3",   "斧术师",  "斧術師",  "斧术",   "斧",    "marauder", "MRD",  "斧術士",  "斧",    "斧",    "Tank", "T",    "坦克",   "Tank", "MT"},
                {"4",   "枪术师",  "槍術師",  "枪术",   "无",    "lancer",   "LNC",  "槍術士",  "槍",    "槍",    "DPS",  "D",    "近战",   "Melee",    "D1"},
                {"5",   "弓箭手",  "弓箭手",  "弓手",   "弓",    "archer",   "ARC",  "弓術士",  "弓",    "弓",    "DPS",  "D",    "远敏",   "Ranged",   "D3"},
                {"6",   "幻术师",  "幻術師",  "幻术",   "幻",    "conjurer", "CNJ",  "幻術士",  "幻",    "幻",    "Healer",   "H",    "治疗",   "Healer",   "H1"},
                {"7",   "咒术师",  "咒術師",  "咒术",   "咒",    "thaumaturge",  "THM",  "呪術士",  "呪",    "呪",    "DPS",  "D",    "法系",   "Ranged",   "D4"},
                {"8",   "刻木匠",  "刻木匠",  "刻木",   "木",    "carpenter",    "CRP",  "木工師",  "木",    "木",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"9",   "锻铁匠",  "鍛鐵匠",  "锻铁",   "锻",    "blacksmith",   "BSM",  "鍛冶師",  "鍛",    "鍛",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"10",  "铸甲匠",  "鑄甲匠",  "铸甲",   "甲",    "armorer",  "ARM",  "甲冑師",  "甲",    "甲",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"11",  "雕金匠",  "雕金匠",  "雕金",   "雕",    "goldsmith",    "GSM",  "彫金師",  "彫",    "彫",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"12",  "制革匠",  "製革匠",  "制革",   "革",    "leatherworker",    "LTW",  "革細工師", "革",    "革",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"13",  "裁衣匠",  "裁衣匠",  "裁缝",   "裁",    "weaver",   "WVR",  "裁縫師",  "裁",    "裁",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"14",  "炼金术士", "鍊金術士", "炼金", "炼",    "alchemist",    "ALC",  "錬金術師", "錬",    "錬",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"15",  "烹调师",  "烹調師",  "烹调",   "烹",    "culinarian",   "CUL",  "調理師",  "調",    "調",    "Crafter",  "C",    "制作",   "Crafter",  "None"},
                {"16",  "采矿工",  "採礦工",  "矿工",   "矿",    "miner",    "MIN",  "採掘師",  "採",    "採",    "Gatherer", "G",    "采集",   "Gatherer", "None"},
                {"17",  "园艺工",  "園藝工",  "园艺",   "园",    "botanist", "BTN",  "園芸師",  "園",    "園",    "Gatherer", "G",    "采集",   "Gatherer", "None"},
                {"18",  "捕鱼人",  "捕魚人",  "钓鱼",   "鱼",    "fisher",   "FSH",  "漁師",   "漁",    "漁",    "Gatherer", "G",    "采集",   "Gatherer", "None"},
                {"19",  "骑士",   "騎士",   "骑士",   "骑",      "paladin",  "PLD",  "ナイト",  "ナイト",  "ナ",    "Tank", "T",    "坦克",   "Tank", "ST"},
                {"20",  "武僧",   "武僧",   "武僧",   "僧",      "monk", "MNK",  "モンク",  "モンク",  "モ",    "DPS",  "D",    "近战",   "Melee",    "D1"},
                {"21",  "战士",   "戰士",   "战士",   "战",      "warrior",  "WAR",  "戦士",   "戦士",   "戦",    "Tank", "T",    "坦克",   "Tank", "MT"},
                {"22",  "龙骑士",  "龍騎士",  "龙骑",   "龙",    "dragoon",  "DRG",  "竜騎士",  "竜",    "竜",    "DPS",  "D",    "近战",   "Melee",    "D2"},
                {"23",  "吟游诗人", "吟遊詩人", "诗人",   "诗",  "bard", "BRD",  "吟遊詩人", "詩人",   "吟",    "DPS",  "D",    "远敏",   "Ranged",   "D3"},
                {"24",  "白魔法师", "白魔法師", "白魔",   "白",  "white mage",   "WHM",  "白魔道士", "白",    "白",    "Healer",   "H",    "治疗",   "Healer",   "H1"},
                {"25",  "黑魔法师", "黑魔法師", "黑魔",   "黑",  "black mage",   "BLM",  "黒魔道士", "黒",    "黒",    "DPS",  "D",    "法系",   "Caster",   "D4"},
                {"26",  "秘术师",  "秘術師",  "秘术",   "秘",    "arcanist", "ACN",  "巴術士",  "巴",    "巴",    "DPS",  "D",    "法系",   "Caster",   "D4"},
                {"27",  "召唤师",  "召喚師",  "召唤",   "召",    "summoner", "SMN",  "召喚士",  "召喚",   "召",    "DPS",  "D",    "法系",   "Caster",   "D4"},
                {"28",  "学者",   "學者",   "学者",   "学",      "scholar",  "SCH",  "学者",   "学者",   "学",    "Healer",   "H",    "治疗",   "Healer",   "H2"},
                {"29",  "双剑师",  "雙劍師",  "双剑",   "双",    "rogue",    "ROG",  "双剣士",  "双",    "双",    "DPS",  "D",    "近战",   "Melee",    "D2"},
                {"30",  "忍者",   "忍者",   "忍者",   "忍",      "ninja",    "NIN",  "忍者",   "忍者",   "忍",    "DPS",  "D",    "近战",   "Melee",    "D2"},
                {"31",  "机工士",  "機工士",  "机工",   "机",    "machinist",    "MCH",  "機工士",  "機工",   "機",    "DPS",  "D",    "远敏",   "Ranged",   "D3"},
                {"32",  "暗黑骑士", "暗黑騎士", "暗骑",   "暗",  "dark knight",  "DRK",  "暗黒騎士", "暗黒",   "暗",    "Tank", "T",    "坦克",   "Tank", "MT"},
                {"33",  "占星术士", "占星術士", "占星",   "星",  "astrologian",  "AST",  "占星術師", "占星",   "占",    "Healer",   "H",    "治疗",   "Healer",   "H1"},
                {"34",  "武士",   "武士",   "武士",   "侍",      "samurai",  "SAM",  "侍",    "侍",    "侍",    "DPS",  "D",    "近战",   "Melee",    "D1"},
                {"35",  "赤魔法师", "赤魔法師", "赤魔",   "赤",  "red mage", "RDM",  "赤魔道士", "赤",    "赤",    "DPS",  "D",    "法系",   "Caster",   "D4"},
                {"36",  "青魔法师", "青魔法師", "青魔",   "青",  "blue mage",    "BLU",  "青魔道士", "青",    "青",    "DPS",  "D",    "青魔",   "Blue", "None"},
                {"37",  "绝枪战士", "絕槍戰士", "枪刃",   "枪",  "gunbreaker",   "GNB",  "ガンブレイカー",  "ガンブレイ",    "ガ",    "Tank", "T",    "坦克",   "Tank", "ST"},
                {"38",  "舞者",   "舞者",   "舞者",   "舞",      "dancer",   "DNC",  "踊り子",  "踊り",   "踊",    "DPS",  "D",    "远敏",   "Ranged",   "D3"},
                {"39",  "贤者",   "賢者",   "贤者",   "贤",      "sage", "SAG",  "賢者",   "賢者",   "賢",    "Healer",   "H",    "治疗",   "Healer",   "H2"},
                {"40",  "钐镰师",   "釤鐮師",   "钐镰",   "镰",  "reaper", "RPR",  "釤鐮",   "釤鐮",   "鐮",    "DPS",   "D",    "近战",   "Melee",   "D1"},
         };
        public static List<string> serverlist = new List<string>() {

            "晨曦王座","沃仙曦染","宇宙和音","红玉海","萌芽池","神意之地","幻影群岛","拉诺西亚","拂晓之间","龙巢神殿","旅人栈桥","白金幻象","白银乡","神拳痕","潮风亭","琥珀原","柔风海湾","海猫茶屋","延夏","静语庄园","摩杜纳","紫水栈桥",
            "Asura","Belias","Chaos","Hecatoncheir","Moomba","Pandaemonium","Shinryu","Unicorn","Yojimbo","Zeromus","Twintania","Brynhildr","Famfrit","Lich","Mateus","Shemhazai","Omega","Jenova","Zalera","Zodiark",
            "Alexander","Anima","Carbuncle","Fenrir","Hades","Ixion","Kujata","Typhon","Ultima","Valefor","Exodus","Faerie","Lamia","Phoenix","Siren","Garuda","Ifrit","Ramuh","Titan","Diabolos","Gilgamesh","Leviathan","Midgardsormr","Odin","Shiva",
            "Atomos","Bahamut","Chocobo","Moogle","Tonberry","Adamantoise","Coeurl","Malboro","Tiamat","Ultros","Behemoth","Cactuar","Cerberus","Goblin","Mandragora","Louisoix","Syldra","Spriggan","Aegis","Balmung","Durandal","Excalibur","Gungnir","Hyperion","Masamune","Ragnarok","Ridill","Sargatanas"

        };
        public enum SignatureType
        {
            ChatLog = 16,
            MobArray = 32,
            ZoneID = 48,
            ServerTime = 64,
            PartyList = 80,
            Player = 96,
            MapID = 112
        }
    }

}
