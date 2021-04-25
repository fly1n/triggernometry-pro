using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Triggernometry.Variables;
using System.Runtime.InteropServices;
using System.Text;

namespace Triggernometry.PluginBridges
{

    public static class BridgeFFXIV
    {

        private static string ActPluginName = "FFXIV_ACT_Plugin.dll";
        private static string ActPluginType = "FFXIV_ACT_Plugin";
        private static IntPtr hProcessFFXIV;

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
            vc.SetValue("pointer", cmx.Pointer.ToString("X12"));
        }

        public static object GetInstance()
        {            
            RealPlugin.PluginWrapper wrap = RealPlugin.InstanceHook(ActPluginName, ActPluginType);
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
                        if (cmx.ID == PlayerId || nump == 1)
                        {
                            if (ex >= PartyMembers.Count)
                            {
                                throw new InvalidOperationException(I18n.Translate("internal/ffxiv/partytoobig", "Party structure has more than {0} members", PartyMembers.Count));
                            }
                            phase = 4;
                            if (cmx.ID == PlayerId)
                            {
                                Myself = PartyMembers[ex];
                            }
                            phase = 5;
                            PopulateClumpFromCombatant(PartyMembers[ex], cmx, 1, nump == 2 ? 1 : 0, ex + 1);
                            phase = 6;
                            for (int i = 0; i < ex; i++)
                            {
                                if (PartyMembers[ex].CompareTo(PartyMembers[i]) == 0)
                                {
                                    ex--;
                                    break;
                                }
                            }
                            ex++;
                            if (ex >= PartyMembers.Count)
                            {
                                // full party found
                                break;
                            }
                        }   
                    }
                    phase = 7;
                    NumPartyMembers = ex;
                    if (PrevNumPartyMembers > NumPartyMembers)
                    {
                        for (int i = NumPartyMembers; i < PrevNumPartyMembers; i++)
                        {
                            ClearCombatant(PartyMembers[i]);
                        }
                    }
                    PrevNumPartyMembers = NumPartyMembers;
                    phase = 8;

                    if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomSelfFirst)
                    {
                        //DebugPlayerSorting("a1", PartyMembers);
                        PartyMembers.Sort(SortPlayersSelf);
                        int ro = 1;
                        foreach (VariableDictionary vc in PartyMembers)
                        {
                            vc.SetValue("order", "" + ro);
                            ro++;
                        }
                        //DebugPlayerSorting("a2", PartyMembers);
                    }
                    else if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomFull)
                    {
                        //DebugPlayerSorting("b1", PartyMembers);
                        PartyMembers.Sort(SortPlayers);
                        int ro = 1;
                        foreach (VariableDictionary vc in PartyMembers)
                        {
                            vc.SetValue("order", "" + ro);
                            ro++;
                        }
                        //DebugPlayerSorting("b2", PartyMembers);
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
                        if (cmx.Name == name)
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
                if (vc.GetValue("name").ToString() == name)
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
        public static void CloseHandleFFXIV()
        {
            if (hProcessFFXIV != IntPtr.Zero)
            {
                CloseHandle(hProcessFFXIV);
            }
        }

        public static string[,] joblist = new string[,] {
                {"0",   "ð����",  "ð�U��",  "ð��",   "ð",    "adventurer",   "ADV",  "���äԤ�ʿ",    "ð",    "ð",    "None", "N",    "ð��",   "adventurer",   "None"},
                {"1",   "����ʦ",  "���g��",  "����",   "��",    "gladiator",    "GLA",  "���gʿ",  "��",    "��",    "Tank", "T",    "̹��",   "Tank", "ST"},
                {"2",   "�񶷼�",  "���Y��",  "��",   "��",    "pugilist", "PGL",  "���Lʿ",  "��",    "��",    "DPS",  "D",    "��ս",   "Melee",    "D1"},
                {"3",   "����ʦ",  "���g��",  "����",   "��",    "marauder", "MRD",  "���gʿ",  "��",    "��",    "Tank", "T",    "̹��",   "Tank", "MT"},
                {"4",   "ǹ��ʦ",  "���g��",  "ǹ��",   "��",    "lancer",   "LNC",  "���gʿ",  "��",    "��",    "DPS",  "D",    "��ս",   "Melee",    "D1"},
                {"5",   "������",  "������",  "����",   "��",    "archer",   "ARC",  "���gʿ",  "��",    "��",    "DPS",  "D",    "Զ��",   "Ranged",   "D3"},
                {"6",   "����ʦ",  "���g��",  "����",   "��",    "conjurer", "CNJ",  "���gʿ",  "��",    "��",    "Healer",   "H",    "����",   "Healer",   "H1"},
                {"7",   "����ʦ",  "���g��",  "����",   "��",    "thaumaturge",  "THM",  "���gʿ",  "��",    "��",    "DPS",  "D",    "��ϵ",   "Ranged",   "D4"},
                {"8",   "��ľ��",  "��ľ��",  "��ľ",   "ľ",    "carpenter",    "CRP",  "ľ����",  "ľ",    "ľ",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"9",   "������",  "��F��",  "����",   "��",    "blacksmith",   "BSM",  "�ұ��",  "�",    "�",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"10",  "���׽�",  "�T�׽�",  "����",   "��",    "armorer",  "ARM",  "�׃َ�",  "��",    "��",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"11",  "���",  "���",  "���",   "��",    "goldsmith",    "GSM",  "������",  "��",    "��",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"12",  "�Ƹｳ",  "�u�ｳ",  "�Ƹ�",   "��",    "leatherworker",    "LTW",  "�：����", "��",    "��",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"13",  "���½�",  "���½�",  "�÷�",   "��",    "weaver",   "WVR",  "�ÿp��",  "��",    "��",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"14",  "������ʿ", "倽��gʿ", "����", "��",    "alchemist",    "ALC",  "�b���g��", "�b",    "�b",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"15",  "���ʦ",  "���{��",  "���",   "��",    "culinarian",   "CUL",  "�{�펟",  "�{",    "�{",    "Crafter",  "C",    "����",   "Crafter",  "None"},
                {"16",  "�ɿ�",  "��V��",  "��",   "��",    "miner",    "MIN",  "���",  "��",    "��",    "Gatherer", "G",    "�ɼ�",   "Gatherer", "None"},
                {"17",  "԰�չ�",  "�@ˇ��",  "԰��",   "԰",    "botanist", "BTN",  "�@ܿ��",  "�@",    "�@",    "Gatherer", "G",    "�ɼ�",   "Gatherer", "None"},
                {"18",  "������",  "���~��",  "����",   "��",    "fisher",   "FSH",  "�O��",   "�O",    "�O",    "Gatherer", "G",    "�ɼ�",   "Gatherer", "None"},
                {"19",  "��ʿ",   "�Tʿ",   "��ʿ",   "��",      "paladin",  "PLD",  "�ʥ���",  "�ʥ���",  "��",    "Tank", "T",    "̹��",   "Tank", "ST"},
                {"20",  "��ɮ",   "��ɮ",   "��ɮ",   "ɮ",      "monk", "MNK",  "���",  "���",  "��",    "DPS",  "D",    "��ս",   "Melee",    "D1"},
                {"21",  "սʿ",   "��ʿ",   "սʿ",   "ս",      "warrior",  "WAR",  "��ʿ",   "��ʿ",   "��",    "Tank", "T",    "̹��",   "Tank", "MT"},
                {"22",  "����ʿ",  "���Tʿ",  "����",   "��",    "dragoon",  "DRG",  "�o�Tʿ",  "�o",    "�o",    "DPS",  "D",    "��ս",   "Melee",    "D2"},
                {"23",  "����ʫ��", "���[Ԋ��", "ʫ��",   "ʫ",  "bard", "BRD",  "���[Ԋ��", "Ԋ��",   "��",    "DPS",  "D",    "Զ��",   "Ranged",   "D3"},
                {"24",  "��ħ��ʦ", "��ħ����", "��ħ",   "��",  "white mage",   "WHM",  "��ħ��ʿ", "��",    "��",    "Healer",   "H",    "����",   "Healer",   "H1"},
                {"25",  "��ħ��ʦ", "��ħ����", "��ħ",   "��",  "black mage",   "BLM",  "�\ħ��ʿ", "�\",    "�\",    "DPS",  "D",    "��ϵ",   "Caster",   "D4"},
                {"26",  "����ʦ",  "���g��",  "����",   "��",    "arcanist", "ACN",  "���gʿ",  "��",    "��",    "DPS",  "D",    "��ϵ",   "Caster",   "D4"},
                {"27",  "�ٻ�ʦ",  "�ن���",  "�ٻ�",   "��",    "summoner", "SMN",  "�ن�ʿ",  "�ن�",   "��",    "DPS",  "D",    "��ϵ",   "Caster",   "D4"},
                {"28",  "ѧ��",   "�W��",   "ѧ��",   "ѧ",      "scholar",  "SCH",  "ѧ��",   "ѧ��",   "ѧ",    "Healer",   "H",    "����",   "Healer",   "H2"},
                {"29",  "˫��ʦ",  "�p����",  "˫��",   "˫",    "rogue",    "ROG",  "˫��ʿ",  "˫",    "˫",    "DPS",  "D",    "��ս",   "Melee",    "D2"},
                {"30",  "����",   "����",   "����",   "��",      "ninja",    "NIN",  "����",   "����",   "��",    "DPS",  "D",    "��ս",   "Melee",    "D2"},
                {"31",  "����ʿ",  "�C��ʿ",  "����",   "��",    "machinist",    "MCH",  "�C��ʿ",  "�C��",   "�C",    "DPS",  "D",    "Զ��",   "Ranged",   "D3"},
                {"32",  "������ʿ", "�����Tʿ", "����",   "��",  "dark knight",  "DRK",  "���\�Tʿ", "���\",   "��",    "Tank", "T",    "̹��",   "Tank", "MT"},
                {"33",  "ռ����ʿ", "ռ���gʿ", "ռ��",   "��",  "astrologian",  "AST",  "ռ���g��", "ռ��",   "ռ",    "Healer",   "H",    "����",   "Healer",   "H1"},
                {"34",  "��ʿ",   "��ʿ",   "��ʿ",   "��",      "samurai",  "SAM",  "��",    "��",    "��",    "DPS",  "D",    "��ս",   "Melee",    "D1"},
                {"35",  "��ħ��ʦ", "��ħ����", "��ħ",   "��",  "red mage", "RDM",  "��ħ��ʿ", "��",    "��",    "DPS",  "D",    "��ϵ",   "Caster",   "D4"},
                {"36",  "��ħ��ʦ", "��ħ����", "��ħ",   "��",  "blue mage",    "BLU",  "��ħ��ʿ", "��",    "��",    "DPS",  "D",    "��ħ",   "Blue", "None"},
                {"37",  "��ǹսʿ", "�^����ʿ", "ǹ��",   "ǹ",  "gunbreaker",   "GNB",  "����֥쥤���`",  "����֥쥤",    "��",    "Tank", "T",    "̹��",   "Tank", "ST"},
                {"38",  "����",   "����",   "����",   "��",      "dancer",   "DNC",  "ӻ����",  "ӻ��",   "ӻ",    "DPS",  "D",    "Զ��",   "Ranged",   "D3"},
                {"39",  "����",   "�t��",   "����",   "��",      "sage", "SAG",  "�t��",   "�t��",   "�t",    "Healer",   "H",    "����",   "Healer",   "H2"},
         };
        public static List<string> serverlist = new List<string>() {

            "��������","������Ⱦ","�������","����","��ѿ��","����֮��","��ӰȺ��","��ŵ����","����֮��","�������","����ջ��","�׽����","������","��ȭ��","����ͤ","����ԭ","��纣��","��è����","����","����ׯ԰","Ħ����","��ˮջ��",
            "Asura","Belias","Chaos","Hecatoncheir","Moomba","Pandaemonium","Shinryu","Unicorn","Yojimbo","Zeromus","Twintania","Brynhildr","Famfrit","Lich","Mateus","Shemhazai","Omega","Jenova","Zalera","Zodiark",
            "Alexander","Anima","Carbuncle","Fenrir","Hades","Ixion","Kujata","Typhon","Ultima","Valefor","Exodus","Faerie","Lamia","Phoenix","Siren","Garuda","Ifrit","Ramuh","Titan","Diabolos","Gilgamesh","Leviathan","Midgardsormr","Odin","Shiva",
            "Atomos","Bahamut","Chocobo","Moogle","Tonberry","Adamantoise","Coeurl","Malboro","Tiamat","Ultros","Behemoth","Cactuar","Cerberus","Goblin","Mandragora","Louisoix","Syldra","Spriggan","Aegis","Balmung","Durandal","Excalibur","Gungnir","Hyperion","Masamune","Ragnarok","Ridill","Sargatanas"

        };
    }

}
