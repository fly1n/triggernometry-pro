﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Web.Script.Serialization;
using WMPLib;
using System.Threading;
using System.Windows.Forms;
using System.CodeDom.Compiler;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Triggernometry.Variables;
using CsvHelper;
using Advanced_Combat_Tracker;
using System.Reflection;
using Newtonsoft.Json;

namespace Triggernometry
{

    /*
	Confirm the triggers to import.
	
	At least one of triggers you are trying to import includes one of more actions that are set to launch an external process.
	These triggers may be dangerous and as such they are not included in the import by default.
	To import these triggers, you will have to confirm them manually one by one.
	
	*/
    public partial class Action
    {

        #region General properties

        internal Action NextAction { get; set; } = null;

        internal Guid Id { get; set; }

        internal bool _Enabled { get; set; } = true;
        [XmlAttribute]
        public string Enabled
        {
            get
            {
                if (_Enabled == true)
                {
                    return null;
                }
                return _Enabled.ToString();
            }
            set
            {
                _Enabled = Boolean.Parse(value);
            }
        }

        [XmlAttribute]
        public ActionTypeEnum ActionType { get; set; } = ActionTypeEnum.SystemBeep;

        internal string _ExecutionDelayExpression { get; set; } = "0";
        [XmlAttribute]
        public string ExecutionDelayExpression
        {
            get
            {
                if ( _ExecutionDelayExpression != "0" && _ExecutionDelayExpression != "")
                {
                    if (_DontExecute) return "0";
                    else return _ExecutionDelayExpression;
                }
                else
                {
                    return "0";
                }
            }
            set
            {
                _ExecutionDelayExpression = value;
            }
        }

        internal bool _Asynchronous { get; set; } = true;
        [XmlAttribute]
        public string Asynchronous
        {
            get
            {
                if (_Asynchronous == true)
                {
                    return null;
                }
                return _Asynchronous.ToString();
            }
            set
            {
                _Asynchronous = Boolean.Parse(value);
            }
        }

        internal RealPlugin.DebugLevelEnum _DebugLevel { get; set; } = RealPlugin.DebugLevelEnum.Inherit;
        [XmlAttribute]
        public string DebugLevel
        {
            get
            {
                if (_DebugLevel != RealPlugin.DebugLevelEnum.Inherit)
                {
                    return _DebugLevel.ToString();
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _DebugLevel = (RealPlugin.DebugLevelEnum)Enum.Parse(typeof(RealPlugin.DebugLevelEnum), value);
            }
        }

        internal bool _RefireInterrupt { get; set; } = false;
        [XmlAttribute]
        public string RefireInterrupt
        {
            get
            {
                if (_RefireInterrupt == false)
                {
                    return null;
                }
                return _RefireInterrupt.ToString();
            }
            set
            {
                _RefireInterrupt = Boolean.Parse(value);
            }
        }

        internal bool _RefireRequeue { get; set; } = true;
        [XmlAttribute]
        public string RefireRequeue
        {
            get
            {
                if (_RefireRequeue == true)
                {
                    return null;
                }
                return _RefireRequeue.ToString();
            }
            set
            {
                _RefireRequeue = Boolean.Parse(value);
            }
        }

        [XmlAttribute]
        public int OrderNumber;

        internal string _Description { get; set; } = "";
        [XmlAttribute]
        public string Description
        {
            get
            {
                if (_Description == "")
                {
                    return null;
                }
                return _Description;
            }
            set
            {
                _Description = value;
            }
        }

        internal bool _DescriptionOverride { get; set; } = false;
        [XmlAttribute]
        public string DescriptionOverride
        {
            get
            {
                if (_DescriptionOverride == false)
                {
                    return null;
                }
                return _DescriptionOverride.ToString();
            }
            set
            {
                _DescriptionOverride = Boolean.Parse(value);
            }
        }

        public ConditionGroup Condition { get; set; }

        #endregion
 
        #region Old condition to new condition converter
        private EventList<Condition> _Conditions;
        public EventList<Condition> Conditions
        {
            get
            {
                return _Conditions;
            }
            set
            {
                _Conditions = value;
                if (_Conditions != null)
                {
                    _Conditions.ItemAdded += _Conditions_ItemAdded;
                }
            }
        }

        private void _Conditions_ItemAdded(object sender, EventListArgs<Condition> e)
        {
            if (Condition == null)
            {
                Condition = new ConditionGroup();
                Condition.Grouping = ConditionGroup.CndGroupingEnum.And;
                Condition.Enabled = true;
            }
            Condition cx = e.Item;
            Condition.AddChild(cx.ConvertToConditionSingle());
            _Conditions.Remove(e.Item);
        }

        public class EventListArgs<T> : EventArgs
        {
            public EventListArgs(T item, int index)
            {
                Item = item;
                Index = index;
            }

            public T Item { get; }
            public int Index { get; }
        }

        public class EventList<T> : IList<T>
        {
            private readonly List<T> _list;

            public EventList()
            {
                _list = new List<T>();
            }

            public EventList(IEnumerable<T> collection)
            {
                _list = new List<T>(collection);
            }

            public EventList(int capacity)
            {
                _list = new List<T>(capacity);
            }

            public event EventHandler<EventListArgs<T>> ItemAdded;
            public event EventHandler<EventListArgs<T>> ItemRemoved;

            private void RaiseEvent(EventHandler<EventListArgs<T>> eventHandler, T item, int index)
            {
                var eh = eventHandler;
                eh?.Invoke(this, new EventListArgs<T>(item, index));
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                var index = _list.Count;
                _list.Add(item);
                RaiseEvent(ItemAdded, item, index);
            }

            public void Clear()
            {
                for (var index = 0; index < _list.Count; index++)
                {
                    var item = _list[index];
                    RaiseEvent(ItemRemoved, item, index);
                }

                _list.Clear();
            }

            public bool Contains(T item)
            {
                return _list.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _list.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                var index = _list.IndexOf(item);

                if (_list.Remove(item))
                {
                    RaiseEvent(ItemRemoved, item, index);
                    return true;
                }

                return false;
            }

            public int Count => _list.Count;
            public bool IsReadOnly => false;

            public int IndexOf(T item)
            {
                return _list.IndexOf(item);
            }

            public void Insert(int index, T item)
            {
                _list.Insert(index, item);
                RaiseEvent(ItemRemoved, item, index);
            }

            public void RemoveAt(int index)
            {
                var item = _list[index];
                _list.RemoveAt(index);
                RaiseEvent(ItemRemoved, item, index);
            }

            public T this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = value; }
            }
        }
        #endregion

        public void ActionContextLogger(object o, string msg)
        {
            AddToLog((Context)o, RealPlugin.DebugLevelEnum.Verbose, msg);
        }

        private string Capitalize(string str)
        {
            if (str == null)
            {
                return null;
            }
            if (str.Length > 1)
            {
                return char.ToUpper(str[0]) + str.Substring(1);
            }
            return str.ToUpper();
        }

        internal bool ObsConnector(Context ctx)
        {
            RealPlugin p = ctx.plug;
            lock (p._obs)
            {
                if (p._obs.IsConnected == true)
                {
                    return true;
                }
                try
                {
                    p._obs.Connect();
                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Info, I18n.Translate("internal/Action/obsconnectok", "OBS WebSocket connected successfully"));
                    return true;
                }
                catch (Exception ex)
                {
                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/obsconnecterror", "Error connecting to OBS WebSocket: {0}", ex.Message));
                }
            }
            return false;
        }

        internal string GetDescription(Context ctx)
        {
            string temp = "";
            if (_DescriptionOverride == true)
            {
                return _Description;
            }
            if (ExecutionDelayExpression.Length > 0 && ExecutionDelayExpression != "0")
            {
                temp += I18n.Translate("internal/Action/descafterdelay", "after ({0}) ms", ExecutionDelayExpression);
                temp += ", ";
            }
            if (Condition != null && Condition.Enabled == true)
            {
                temp += I18n.Translate("internal/Action/descassumingcondition", "assuming condition is met");
                temp += ", ";
            }
            switch (ActionType)
            { 
                case ActionTypeEnum.Trigger:
                    {
                        Trigger t = ctx.plug.GetTriggerById(_TriggerId, ctx.trig != null ? ctx.trig.Repo : null);
                        if (t != null)
                        {
                            switch (_TriggerOp)
                            {
                                case TriggerOpEnum.CancelTrigger:
                                    temp += I18n.Translate("internal/Action/desctrigcancel", "cancel all actions queued from trigger ({0})", t.Name);
                                    break;
                                case TriggerOpEnum.CancelAllTrigger:
                                    temp += I18n.Translate("internal/Action/desctrigcancelall", "cancel all actions queued from all triggers");
                                    break;
                                case TriggerOpEnum.FireTrigger:
                                    
                                    temp += I18n.Translate("internal/Action/desctrigfire", "fire trigger ({0})", t.Name);
                                    List<string> ex = new List<string>();
                                    if (_TriggerForceType == TriggerForceTypeEnum.SkipAll)
                                    {
                                        ex.Add(I18n.Translate("internal/Action/desctrigignoreall", "all restrictions"));
                                    }
                                    else
                                    {
                                        if ((_TriggerForceType & TriggerForceTypeEnum.SkipRegexp) != 0)
                                        {
                                            ex.Add(I18n.Translate("internal/Action/desctrigignoreregex", "regular expression"));
                                        }
                                        else
                                        {
                                            temp += " " + I18n.Translate("internal/Action/desctrigfireusing", "with event text ({0}) and zone ({1})", _TriggerText, _TriggerZone);
                                        }
                                        if ((_TriggerForceType & TriggerForceTypeEnum.SkipConditions) != 0)
                                        {
                                            ex.Add(I18n.Translate("internal/Action/desctrigignoreconditions", "conditions"));
                                        }
                                        if ((_TriggerForceType & TriggerForceTypeEnum.SkipRefire) != 0)
                                        {
                                            ex.Add(I18n.Translate("internal/Action/desctrigignorerefire", "refire delay"));
                                        }
                                        if ((_TriggerForceType & TriggerForceTypeEnum.SkipParent) != 0)
                                        {
                                            ex.Add(I18n.Translate("internal/Action/desctrigignoreparent", "parent folder settings"));
                                        }
                                        if ((_TriggerForceType & TriggerForceTypeEnum.SkipActive) != 0)
                                        {
                                            ex.Add(I18n.Translate("internal/Action/desctrigignorestate", "enabled/disabled status"));
                                        }
                                    }
                                    if (ex.Count > 1)
                                    {
                                        ex[ex.Count - 1] = I18n.Translate("internal/Action/desctrigignoreand", "and") + " " + ex[ex.Count - 1];
                                    }
                                    if (ex.Count > 0)
                                    {
                                        temp += ", " + I18n.Translate("internal/Action/desctrigignoring", "ignoring") + " " + String.Join(", ", ex);
                                    }
                                    break;
                                case TriggerOpEnum.DisableTrigger:
                                    temp += I18n.Translate("internal/Action/desctrigdisable", "disable trigger ({0})", t.Name);
                                    break;
                                case TriggerOpEnum.EnableTrigger:
                                    temp += I18n.Translate("internal/Action/desctrigenable", "enable trigger ({0})", t.Name);
                                    break;
                                case TriggerOpEnum.CopyTrigger:
                                    temp += I18n.Translate("internal/Action/desctrigcopy", "copy trigger ({0}) into new trigger with name: ({1}) and regular expression: ({2})", t.Name, _TriggerText, _TriggerZone);
                                    break;
                            }
                        }
                        else
                        {
                            if (_TriggerOp == TriggerOpEnum.CancelAllTrigger)
                            {
                                temp += I18n.Translate("internal/Action/desctrigcancelall", "cancel all actions queued from all triggers");
                            }
                            else
                            {
                                temp += I18n.Translate("internal/Action/desctriginvalidref", "trigger action with an invalid trigger reference ({0})", _TriggerId);
                            }
                        }
                    }
                    break;
                case ActionTypeEnum.Folder:
                    {
                        Folder f = ctx.plug.GetFolderById(_FolderId, ctx.trig != null ? ctx.trig.Repo : null);
                        if (f != null)
                        {
                            switch (_FolderOp)
                            {
                                case FolderOpEnum.DisableFolder:
                                    temp += I18n.Translate("internal/Action/descdisablefolder", "disable folder ({0})", f.Name);
                                    break;
                                case FolderOpEnum.EnableFolder:
                                    temp += I18n.Translate("internal/Action/descenablefolder", "enable folder ({0})", f.Name);
                                    break;
                            }
                        }
                        else
                        {
                            temp += I18n.Translate("internal/Action/descinvalidfolderref", "folder action with an invalid folder reference ({0})", _FolderId);
                        }
                    }
                    break;
                case ActionTypeEnum.KeyPress:
                    if (_KeypressType == KeypressTypeEnum.SendKeys)
                    {
                        temp += I18n.Translate("internal/Action/desckeypresses", "send keypresses ({0}) to the active window", _KeyPressExpression);
                    }
                    else
                    {
                        temp += I18n.Translate("internal/Action/desckeypress", "send keycode ({0}) to window ({1})", _KeyPressCode, _KeyPressWindow);
                    }
                    break;
                case ActionTypeEnum.LaunchProcess:
                    {
                        string tempt = "";
                        switch (_LaunchProcessWindowStyle)
                        {
                            case System.Diagnostics.ProcessWindowStyle.Hidden:
                                tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Hidden from view]", _LaunchProcessWindowStyle.ToString());
                                break;
                            case System.Diagnostics.ProcessWindowStyle.Maximized:
                                tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Maximized to fullscreen]", _LaunchProcessWindowStyle.ToString());
                                break;
                            case System.Diagnostics.ProcessWindowStyle.Minimized:
                                tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Minimized to taskbar]", _LaunchProcessWindowStyle.ToString());
                                break;
                            case System.Diagnostics.ProcessWindowStyle.Normal:
                                tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Normal]", _LaunchProcessWindowStyle.ToString());
                                break;
                            default:
                                tempt = _LaunchProcessWindowStyle.ToString();
                                break;
                        }
                        temp += I18n.Translate("internal/Action/desclaunchprocess", "launch process ({0}) as ({1}) using command line parameters ({2})",
                            LaunchProcessPathExpression,
                            tempt,
                            LaunchProcessCmdlineExpression
                        );
                    }
                    break;
                case ActionTypeEnum.PlaySound:
                    temp += I18n.Translate("internal/Action/descplaysound", "play sound file ({0}) at volume ({1}) %", _PlaySoundFileExpression, _PlaySoundVolumeExpression);
                    break;
                case ActionTypeEnum.SystemBeep:
                    temp += I18n.Translate("internal/Action/descbeep", "beep at ({0}) hz for ({1}) ms", _SystemBeepFreqExpression, _SystemBeepLengthExpression);
                    break;
                case ActionTypeEnum.UseTTS:
                    temp += I18n.Translate("internal/Action/desctts", "say ({0}) at volume ({1}) %, using speed ({2})", _UseTTSTextExpression, _UseTTSVolumeExpression, _UseTTSRateExpression);
                    break;
                case ActionTypeEnum.ExecuteScript:
                    temp += I18n.Translate("internal/Action/descexecscript", "execute ({0}) script", _ExecScriptType.ToString());
                    break;
                case ActionTypeEnum.MessageBox:
                    temp += I18n.Translate("internal/Action/descmsgbox", "show a message box saying ({0}) with icon ({1})", _MessageBoxText, _MessageBoxIconType.ToString());
                    break;
                case ActionTypeEnum.Mutex:
                    switch (_MutexOpType)
                    {
                        case MutexOpEnum.Release:
                            temp += I18n.Translate("internal/Action/mutexrelease", "release mutex ({0})", _MutexName);
                            break;
                        case MutexOpEnum.Acquire:
                            temp += I18n.Translate("internal/Action/mutexacquire", "acquire mutex ({0})", _MutexName);
                            break;
                    }
                    break;
                case ActionTypeEnum.ListVariable:
                    switch (_ListVariableOp)
                    {
                        case ListVariableOpEnum.Unset:
                            temp += I18n.Translate("internal/Action/desclistunset", "unset list variable ({0})", _ListVariableName);
                            break;
                        case ListVariableOpEnum.Push:
                            switch (_ListVariableExpressionType)
                            {
                                case ListVariableExpTypeEnum.Numeric:
                                    temp += I18n.Translate("internal/Action/desclistpushnumeric", "push the value from numeric expression ({0}) to the end of list variable ({1})", _ListVariableExpression, _ListVariableName);
                                    break;
                                case ListVariableExpTypeEnum.String:
                                    temp += I18n.Translate("internal/Action/desclistpushstring", "push the value from string expression ({0}) to the end of list variable ({1})", _ListVariableExpression, _ListVariableName);
                                    break;
                            }
                            break;
                        case ListVariableOpEnum.Insert:
                            switch (_ListVariableExpressionType)
                            {
                                case ListVariableExpTypeEnum.Numeric:
                                    temp += I18n.Translate("internal/Action/desclistinsertnumeric", "insert the value from numeric expression ({0}) to index ({1}) on list variable ({2})", _ListVariableExpression, _ListVariableIndex, _ListVariableName);
                                    break;
                                case ListVariableExpTypeEnum.String:
                                    temp += I18n.Translate("internal/Action/desclistinsertstring", "insert the value from string expression ({0}) to index ({1}) on list variable ({2})", _ListVariableExpression, _ListVariableIndex, _ListVariableName);
                                    break;
                            }
                            break;
                        case ListVariableOpEnum.Set:
                            switch (_ListVariableExpressionType)
                            {
                                case ListVariableExpTypeEnum.Numeric:
                                    temp += I18n.Translate("internal/Action/desclistsetnumeric", "set the value from numeric expression ({0}) to index ({1}) on list variable ({2})", _ListVariableExpression, _ListVariableIndex, _ListVariableName);
                                    break;
                                case ListVariableExpTypeEnum.String:
                                    temp += I18n.Translate("internal/Action/desclistsetstring", "set the value from string expression ({0}) to index ({1}) on list variable ({2})", _ListVariableExpression, _ListVariableIndex, _ListVariableName);
                                    break;
                            }
                            break;
                        case ListVariableOpEnum.Remove:
                            temp += I18n.Translate("internal/Action/desclistremoveindex", "remove the value at index ({0}) on list variable ({1})", _ListVariableIndex, _ListVariableName);
                            break;
                        case ListVariableOpEnum.PopLast:
                            temp += I18n.Translate("internal/Action/desclistpoplast", "pop the last value in list variable ({0}) into scalar variable ({1})", _ListVariableName, _ListVariableTarget);
                            break;
                        case ListVariableOpEnum.PopFirst:
                            temp += I18n.Translate("internal/Action/desclistpopfirst", "pop the first value in list variable ({0}) into scalar variable ({1})", _ListVariableName, _ListVariableTarget);
                            break;
                        case ListVariableOpEnum.SortAlphaAsc:
                            temp += I18n.Translate("internal/Action/desclistsortasc", "sort list variable ({0}) in an alphabetically ascending order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.SortAlphaDesc:
                            temp += I18n.Translate("internal/Action/desclistsortdesc", "sort list variable ({0}) in an alphabetically descending order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.SortNumericAsc:
                            temp += I18n.Translate("internal/Action/desclistsortasc", "sort list variable ({0}) in an numeric ascending order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.SortNumericDesc:
                            temp += I18n.Translate("internal/Action/desclistsortdesc", "sort list variable ({0}) in an numeric descending order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.SortFfxivPartyAsc:
                            temp += I18n.Translate("internal/Action/desclistsortffxivasc", "sort list variable ({0}) in ascending order according to FFXIV party job order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.SortFfxivPartyDesc:
                            temp += I18n.Translate("internal/Action/desclistsortffxivdesc", "sort list variable ({0}) in descending order according to FFXIV party job order", _ListVariableName);
                            break;
                        case ListVariableOpEnum.Copy:
                            temp += I18n.Translate("internal/Action/desclistcopy", "copy list variable ({0}) to list variable ({1})", _ListVariableName, _ListVariableTarget);
                            break;
                        case ListVariableOpEnum.Filter:
                            temp += I18n.Translate("internal/Action/desclistfilter", "use list variable ({0}) as a filter to list variable ({1})", _ListVariableName, _ListVariableTarget);
                            break;
                        case ListVariableOpEnum.ReverseFilter:
                            temp += I18n.Translate("internal/Action/desclistreversefilter", "use list variable ({0}) as a reverse filter to list variable ({1})", _ListVariableName, _ListVariableTarget);
                            break;
                        case ListVariableOpEnum.InsertList:
                            temp += I18n.Translate("internal/Action/desclistinsertlist", "insert list variable ({0}) into list variable ({1}) at index ({2})", _ListVariableName, _ListVariableTarget, _ListVariableIndex);
                            break;
                        case ListVariableOpEnum.Join:
                            temp += I18n.Translate("internal/Action/desclistjoin", "join all values in list variable ({0}) to scalar variable ({1}) using ({2}) as separator", _ListVariableName, _ListVariableTarget, _ListVariableExpression);
                            break;
                        case ListVariableOpEnum.Split:
                            temp += I18n.Translate("internal/Action/desclistsplit", "split scalar variable ({0}) into list variable ({1}) using ({2}) as separator", _ListVariableName, _ListVariableTarget, _ListVariableExpression);
                            break;
                        case ListVariableOpEnum.UnsetAll:
                            temp += I18n.Translate("internal/Action/desclistunsetall", "unset all list variables");
                            break;
                        case ListVariableOpEnum.UnsetRegex:
                            temp += I18n.Translate("internal/Action/desclistunsetregex", "unset list variables matching regular expression ({0})", _ListVariableName);
                            break;
                    }
                    break;
                case ActionTypeEnum.GenericJson:
                    if (_JsonCacheRequest == true)
                    {
                        if (_JsonFiringExpression != null && _JsonFiringExpression.Trim().Length > 0)
                        {
                            temp += I18n.Translate("internal/Action/descjsonsendrelaycache", "send JSON payload to endpoint ({0}), caching the response, and relaying response for further processing", _JsonEndpointExpression);
                        }
                        else
                        {
                            temp += I18n.Translate("internal/Action/descjsonsendcache", "send JSON payload to endpoint ({0}) and cache the response", _JsonEndpointExpression);
                        }
                    }
                    else
                    {
                        if (_JsonFiringExpression != null && _JsonFiringExpression.Trim().Length > 0)
                        {
                            temp += I18n.Translate("internal/Action/descjsonsendrelay", "send JSON payload to endpoint ({0}), relaying response for further processing", _JsonEndpointExpression);
                        }
                        else
                        {
                            temp += I18n.Translate("internal/Action/descjsonsend", "send JSON payload to endpoint ({0})", _JsonEndpointExpression);
                        }
                    }
                    break;
                case ActionTypeEnum.ObsControl:
                    switch (_OBSControlType)
                    {
                        case ObsControlTypeEnum.StartStreaming:
                            temp += I18n.Translate("internal/Action/descobsstartstream", "start streaming on OBS");
                            break;
                        case ObsControlTypeEnum.StopStreaming:
                            temp += I18n.Translate("internal/Action/descobsstopstream", "stop streaming on OBS");
                            break;
                        case ObsControlTypeEnum.ToggleStreaming:
                            temp += I18n.Translate("internal/Action/descobstogglestream", "start/stop streaming on OBS (toggle)");
                            break;
                        case ObsControlTypeEnum.StartRecording:
                            temp += I18n.Translate("internal/Action/descobsstartrecord", "start recording on OBS");
                            break;
                        case ObsControlTypeEnum.StopRecording:
                            temp += I18n.Translate("internal/Action/descobsstoprecord", "stop recording on OBS");
                            break;
                        case ObsControlTypeEnum.ToggleRecording:
                            temp += I18n.Translate("internal/Action/descobstogglerecord", "start/stop recording on OBS (toggle)");
                            break;
                        case ObsControlTypeEnum.StartReplayBuffer:
                            temp += I18n.Translate("internal/Action/descobsstartreplay", "start OBS replay buffer");
                            break;
                        case ObsControlTypeEnum.StopReplayBuffer:
                            temp += I18n.Translate("internal/Action/descobsstopreplay", "stop OBS replay buffer");
                            break;
                        case ObsControlTypeEnum.ToggleReplayBuffer:
                            temp += I18n.Translate("internal/Action/descobstogglereplay", "start/stop OBS replay buffer (toggle)");
                            break;
                        case ObsControlTypeEnum.SaveReplayBuffer:
                            temp += I18n.Translate("internal/Action/descobssavereplay", "save OBS replay buffer");
                            break;
                        case ObsControlTypeEnum.SetScene:
                            temp += I18n.Translate("internal/Action/descobssetscene", "set current OBS scene to ({0})", _OBSSceneName);
                            break;
                        case ObsControlTypeEnum.ShowSource:
                            if (_OBSSceneName != null && _OBSSceneName != "")
                            {
                                temp += I18n.Translate("internal/Action/descobsshowsource", "show source ({0}) on OBS scene ({1})", _OBSSourceName, _OBSSceneName);
                            }
                            else
                            {
                                temp += I18n.Translate("internal/Action/descobsshowsourcecurrent", "show source ({0}) on current OBS scene", _OBSSourceName);
                            }
                            break;
                        case ObsControlTypeEnum.HideSource:
                            if (_OBSSceneName != null && _OBSSceneName != "")
                            {
                                temp += I18n.Translate("internal/Action/descobshidesource", "hide source ({0}) on OBS scene ({1})", _OBSSourceName, _OBSSceneName);
                            }
                            else
                            {
                                temp += I18n.Translate("internal/Action/descobshidesourcecurrent", "hide source ({0}) on current OBS scene", _OBSSourceName);
                            }
                            break;
                        case ObsControlTypeEnum.JSONPayload:
                            temp += I18n.Translate("internal/Action/descobsjsonpayload", "Send custom JSON payload to OBS");
                            break;
                    }
                    break; 
                case ActionTypeEnum.Variable:
                    switch (_VariableOp)
                    {
                        case VariableOpEnum.SetNumeric:
                            temp += I18n.Translate("internal/Action/descscalarnumeric", "set scalar variable ({0}) value with numeric expression ({1})", _VariableName, _VariableExpression);
                            break;
                        case VariableOpEnum.SetString:
                            temp += I18n.Translate("internal/Action/descscalarstring", "set scalar variable ({0}) value with string expression ({1})", _VariableName, _VariableExpression);
                            break;
                        case VariableOpEnum.Unset:
                            temp += I18n.Translate("internal/Action/descscalarunset", "unset scalar variable ({0})", _VariableName);
                            break;
                        case VariableOpEnum.UnsetAll:
                            temp += I18n.Translate("internal/Action/descscalarunsetall", "unset all scalar variables");
                            break;
                        case VariableOpEnum.UnsetRegex:
                            temp += I18n.Translate("internal/Action/descscalarunsetregex", "unset scalar variables matching regular expression ({0})", _VariableName);
                            break;
                    }
                    break;
                case ActionTypeEnum.TableVariable:
                    switch (_TableVariableOp)
                    {
                        case TableVariableOpEnum.Set:
                            if (_TableVariableExpressionType == TableVariableExpTypeEnum.Numeric)
                            {
                                temp += I18n.Translate("internal/Action/desctablenumeric", "set table variable ({0}) value at ({1},{2}) with numeric expression ({3})", _TableVariableName, _TableVariableX, _TableVariableY, _TableVariableExpression);
                            }
                            else
                            {
                                temp += I18n.Translate("internal/Action/desctablestring", "set table variable ({0}) value at ({1},{2}) with string expression ({3})", _TableVariableName, _TableVariableX, _TableVariableY, _TableVariableExpression);
                            }
                            break;
                        case TableVariableOpEnum.Resize:
                            temp += I18n.Translate("internal/Action/desctableresize", "resize table variable ({0}) to ({1},{2})", _TableVariableName, _TableVariableX, _TableVariableY);
                            break;
                        case TableVariableOpEnum.Unset:
                            temp += I18n.Translate("internal/Action/desctableunset", "unset table variable ({0})", _TableVariableName);
                            break;
                        case TableVariableOpEnum.UnsetAll:
                            temp += I18n.Translate("internal/Action/desctableunsetall", "unset all table variables");
                            break;
                        case TableVariableOpEnum.UnsetRegex:
                            temp += I18n.Translate("internal/Action/desctableunsetregex", "unset table variables matching regular expression ({0})", _TableVariableName);
                            break;
                        case TableVariableOpEnum.Copy:
                            temp += I18n.Translate("internal/Action/desctablecopy", "copy table variable ({0}) to table variable ({1})", _TableVariableName, _TableVariableTarget);
                            break;
                        case TableVariableOpEnum.Append:
                            temp += I18n.Translate("internal/Action/desctableappend", "append table variable ({0}) to table variable ({1})", _TableVariableName, _TableVariableTarget);
                            break;
                    }
                    break;
                case ActionTypeEnum.Aura:
                    switch (_AuraOp)
                    {
                        case AuraOpEnum.ActivateAura:
                            temp += I18n.Translate("internal/Action/descimgauraact", "activate image aura ({0}) with image ({1})", _AuraName, _AuraImage);
                            break;
                        case AuraOpEnum.DeactivateAura:
                            temp += I18n.Translate("internal/Action/descimgauradeact", "deactivate image aura ({0})", _AuraName);
                            break;
                        case AuraOpEnum.DeactivateAllAura:
                            temp += I18n.Translate("internal/Action/descimgauradeactall", "deactivate all image auras");
                            break;
                        case AuraOpEnum.DeactivateAuraRegex:
                            temp += I18n.Translate("internal/Action/descimgauradeactrex", "deactivate image auras matching regular expression ({0})", _AuraName);
                            break;
                    }
                    break;
                case ActionTypeEnum.TextAura:
                    switch (_TextAuraOp)
                    {
                        case AuraOpEnum.ActivateAura:
                            temp += I18n.Translate("internal/Action/desctextauraact", "activate text aura ({0}) with expression ({1})", _TextAuraName, _TextAuraExpression);
                            break;
                        case AuraOpEnum.DeactivateAura:
                            temp += I18n.Translate("internal/Action/desctextauradeact", "deactivate text aura ({0})", _TextAuraName);
                            break;
                        case AuraOpEnum.DeactivateAllAura:
                            temp += I18n.Translate("internal/Action/desctextauradeactall", "deactivate all text auras");
                            break;
                        case AuraOpEnum.DeactivateAuraRegex:
                            temp += I18n.Translate("internal/Action/desctextauradeactrex", "deactivate text auras matching regular expression ({0})", _TextAuraName);
                            break;
                    }
                    break;
                case ActionTypeEnum.DiscordWebhook:
                    {
                        if (_DiscordTts == true)
                        {
                            temp += I18n.Translate("internal/Action/descdiscordttsmsg", "send TTS message ({0}) to Discord webhook ({1})", _DiscordWebhookMessage, _DiscordWebhookURL);
                        }
                        else
                        {
                            temp += I18n.Translate("internal/Action/descdiscordmsg", "send message ({0}) to Discord webhook ({1})", _DiscordWebhookMessage, _DiscordWebhookURL);
                        }
                    }
                    break;
                case ActionTypeEnum.EndEncounter:
                    {
                        switch (_EncounterOp)
                        {
                            case EncounterOpEnum.EndEncounter:
                                temp += I18n.Translate("internal/Action/descendencounter", "end encounter");
                                break;
                            case EncounterOpEnum.StartEncounter:
                                temp += I18n.Translate("internal/Action/descstartencounter", "start encounter");
                                break;
                        }
                    }
                    break;
                case ActionTypeEnum.LogMessage:
                    {
                        if (_LogProcess == true)
                        {
                            temp += I18n.Translate("internal/Action/descprocessmessage", "process message ({0}) as log line", _LogMessageText);
                        }
                        else
                        {
                            temp += I18n.Translate("internal/Action/desclogmessage", "log message ({0})", _LogMessageText);
                        }                        
                    }
                    break;
                case ActionTypeEnum.WindowMessage:
                    temp += I18n.Translate("internal/Action/descwmsg", "send message ({0}) wparam ({1}) lparam ({2}) to window ({3})", _WmsgCode, _WmsgWparam, _WmsgLparam, _WmsgTitle);
                    break;
                case ActionTypeEnum.DiskFile:
                    {
                        switch (_DiskFileOp)
                        {
                            case DiskFileOpEnum.ReadIntoListVariable:
                                if (_DiskFileCache == true)
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadlistvarcache", "read file ({0}) lines into list variable ({1}), caching the file on disk", _DiskFileOpName, _DiskFileOpVar);
                                }
                                else
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadlistvar", "read file ({0}) lines into list variable ({1})", _DiskFileOpName, _DiskFileOpVar);
                                }
                                break;
                            case DiskFileOpEnum.ReadIntoVariable:
                                if (_DiskFileCache == true)
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadvarcache", "read file ({0}) into scalar variable ({1}), caching the file on disk", _DiskFileOpName, _DiskFileOpVar);
                                }
                                else
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadvar", "read file ({0}) into scalar variable ({1})", _DiskFileOpName, _DiskFileOpVar);
                                }
                                break;
                            case DiskFileOpEnum.ReadCSVIntoTableVariable:
                                if (_DiskFileCache == true)
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadcsvtablecache", "read CSV file ({0}) into table variable ({1}), caching the file on disk", _DiskFileOpName, _DiskFileOpVar);
                                }
                                else
                                {
                                    temp += I18n.Translate("internal/Action/descfilereadcsvtable", "read CSV file ({0}) into table variable ({1})", _DiskFileOpName, _DiskFileOpVar);
                                }
                                break;
                            case DiskFileOpEnum.AppendNewLineIntoFile:
                                temp += I18n.Translate("internal/Action/descfilewriteline", "append new line ({1}) into file ({0})", _DiskFileOpName, _DiskFileOpVar);
                                break;
                            case DiskFileOpEnum.ClearAndRewriteLineIntoFile:
                                temp += I18n.Translate("internal/Action/descfilerewriteline", "clear and rewrite line ({1}) into file ({0})", _DiskFileOpName, _DiskFileOpVar);
                                break;
                            case DiskFileOpEnum.ClearAndRewriteListVariableIntoFile:
                                temp += I18n.Translate("internal/Action/descfilerewritelist", "clear and rewrite list variable ({1}) into file ({0})", _DiskFileOpName, _DiskFileOpVar);
                                break;
                            case DiskFileOpEnum.ClearAndRewriteTableVariableIntoFile:
                                temp += I18n.Translate("internal/Action/descfilerewritetable", "clear and rewrite table variable ({1}) into file ({0})", _DiskFileOpName, _DiskFileOpVar);
                                break;
                        }
                    }
                    break;
                case ActionTypeEnum.Placeholder:
                    temp += I18n.Translate("internal/Action/descplaceholder", "Placeholder");
                    break;
                case ActionTypeEnum.NamedCallback:
                    temp += I18n.Translate("internal/Action/descnamedcallback", "Invoke named callback ({0}) with parameter ({1})", _NamedCallbackName, _NamedCallbackParam);
                    break;
                case ActionTypeEnum.Mouse:
                    {
                        string coorddesc = "";
                        switch (_MouseCoordType)
                        {
                            case MouseCoordEnum.Absolute:
                                coorddesc = I18n.Translate("internal/Action/descmousecoordabsolute", "to absolute coordinates");
                                break;
                            case MouseCoordEnum.Relative:
                                coorddesc = I18n.Translate("internal/Action/descmousecoordrelative", "by relative coordinates");
                                break;
                        }
                        switch (_MouseOpType)
                            {
                                case MouseOpEnum.Move:
                                    temp += I18n.Translate("internal/Action/descmousemove", "Move mouse {0} X: {1} Y: {2}", coorddesc, _MouseX, _MouseY);
                                    break;
                                case MouseOpEnum.LeftClick:
                                    temp += I18n.Translate("internal/Action/descmouselmb", "Move mouse {0} X: {1} Y: {2} and left click", coorddesc, _MouseX, _MouseY);
                                    break;
                                case MouseOpEnum.MiddleClick:
                                    temp += I18n.Translate("internal/Action/descmousemmb", "Move mouse {0} X: {1} Y: {2} and middle click", coorddesc, _MouseX, _MouseY);
                                    break;
                                case MouseOpEnum.RightClick:
                                    temp += I18n.Translate("internal/Action/descmousermb", "Move mouse {0} X: {1} Y: {2} and right click", coorddesc, _MouseX, _MouseY);
                                    break;
                            }
                    }
                    break;
                case ActionTypeEnum.PartyOrder:
                    temp += I18n.Translate("internal/Action/overridepartyorder", "Override party order for player {0} to {1}",_PartyOrderPlayerName ,_PartyOrderPartyOrder );
                    break;
                case ActionTypeEnum.DeveloperAction:
                    temp += I18n.Translate("internal/Action/developeraction", "Do developer action key: {0} value: {1}", _DevActionKey, _DevActionValue);
                    break;
                default:
                    temp += I18n.Translate("internal/Action/descunknown", "unknown action type");
                    break;
            }
            return Capitalize(temp);
        }

        internal List<WindowsMediaPlayer> players;

        public Action()
        {
            Id = Guid.NewGuid();
            Conditions = new EventList<Condition>();
            players = new List<WindowsMediaPlayer>();
        }

        private RealPlugin.DebugLevelEnum GetDebugLevel(Context ctx)
        {
            if (_DebugLevel == RealPlugin.DebugLevelEnum.Inherit)
            {
                if (ctx.trig != null)
                {
                    return ctx.trig.GetDebugLevel(ctx.plug);
                }
                else
                {
                    return RealPlugin.DebugLevelEnum.Verbose;
                }
            }
            else
            {
                return _DebugLevel;
            }
        }

        internal void AddToLog(Context ctx, RealPlugin.DebugLevelEnum level, string message)
        {
            RealPlugin.DebugLevelEnum dx = GetDebugLevel(ctx);
            if (level > dx)
            {
                return;
            }
            var myName = "";
            var myAction = 0;
            var myActionType = "";
            if (ctx.trig != null) { 
                myName = ctx.trig.Name;
            }
            if (ctx.ActionResults!=null)
            {
                try
                {
                    myAction = ctx.ActionResults.Count;
                    myActionType = ctx.trig.Actions[myAction].ActionType.ToString();
                }catch(Exception e)
                {

                }
            }
            ctx.plug.UnfilteredAddToLog(level, "["+myName+":AC_"+myAction.ToString("00")+":"+myActionType+"]"+message);
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
            VariableTable vt = new VariableTable();
            if (createNew == true)
            {
                vs.Table[varname] = vt;
            }
            return vt;
        }

        private string GetListExpressionValue(Context ctx, ListVariableExpTypeEnum typ, string expr)
        {
            switch (typ)
            {
                case ListVariableExpTypeEnum.Numeric:
                    return I18n.ThingToString(ctx.EvaluateNumericExpression(ActionContextLogger, ctx, expr));
                case ListVariableExpTypeEnum.String:
                    return ctx.EvaluateStringExpression(ActionContextLogger, ctx, expr);
            }
            return "";
        }

        private void ExecutionImplementation(RealPlugin.QueuedAction qa, Context ctx)
		{
			try
			{

                if (_DontExecute == true)
                {
                    if (_SchedulingActionOp != SchedulingActionOpEnum.Nothing)
                    {
                        Trigger t = ctx.plug.GetTriggerByName(ctx.EvaluateStringExpression(ActionContextLogger, ctx, _SchedulingTriggerName), ctx.trig != null ? ctx.trig.Repo : null);

                        switch (_SchedulingActionOp)
                        {

                            case SchedulingActionOpEnum.Clear:
                                {
                                    t.Actions.Clear();
                                }; break;
                            case SchedulingActionOpEnum.Insert:
                            case SchedulingActionOpEnum.Override:
                            case SchedulingActionOpEnum.Push:
                                {
                                    Action newAction = new Action();
                                    this.InheritSettingsTo(newAction, ctx);
                                    newAction._SchedulingActionOp = SchedulingActionOpEnum.Nothing;
                                    newAction._DontExecute = false;
                                    newAction.SchedulingTriggerName = "";
                                    newAction._SchedulingActionIndex = "";
                                    int index = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _SchedulingActionIndex);
                                    switch (_SchedulingActionOp)
                                    {
                                        case SchedulingActionOpEnum.Insert:
                                            {
                                                if (index >= t.Actions.Count)
                                                    t.Actions.Add(newAction);
                                                else
                                                    t.Actions.Insert(index, newAction);
                                            }
                                            break;
                                        case SchedulingActionOpEnum.Override:
                                            {
                                                if (index >= t.Actions.Count)
                                                    t.Actions.Add(newAction);
                                                else
                                                    t.Actions[index] = newAction;
                                            }
                                            break;
                                        case SchedulingActionOpEnum.Push: t.Actions.Add(newAction); break;
                                        default: break;
                                    }
                                }; break;
                            default: break;

                        }
                    }
                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/actionnotfired", "Action #{0} on trigger '{1}' not fired, it dont need to execute", OrderNumber, (ctx.trig != null ? ctx.trig.LogName : "(null)")));
                    ctx.PushActionResult(0);
                    goto ContinueChain;
                }
                if ((ctx.force & Action.TriggerForceTypeEnum.SkipConditions) == 0 && ctx.testmode == false)
                {
                    if (Condition != null && Condition.Enabled == true)
                    {
                        if (Condition.CheckCondition(ctx, ActionContextLogger, ctx) == false)
                        {
                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/actionnotfired", "Action #{0} on trigger '{1}' not fired, condition not met", OrderNumber, (ctx.trig != null ? ctx.trig.LogName : "(null)")));
                            ctx.PushActionResult(0);
                            goto ContinueChain;
                        }
                    }
                }


                ctx.PushActionResult(1);
                AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/executingaction", "Executing action '{0}' in thread {1}", GetDescription(ctx), System.Threading.Thread.CurrentThread.ManagedThreadId));
				switch (ActionType)
				{
                    #region Implementation - Beep
                    case ActionTypeEnum.SystemBeep:
						{
							double freq = ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _SystemBeepFreqExpression);
                            if (freq < 37.0)
                            {                                
                                freq = 37.0;
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/beepfreqlo", "Beep frequency below limit, capping to {0}", freq));
                            }
                            if (freq > 32767.0)
                            {
                                freq = 32767.0;
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/beepfreqhi", "Beep frequency above limit, capping to {0} ", freq));
                            }
                            double len = ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _SystemBeepLengthExpression);
                            if (len < 0.0)
                            {
                                len = 0.0;
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/beeplengthlo", "Beep length below limit, capping to {0}", len));
                            }
                            Console.Beep((int)Math.Ceiling(freq), (int)Math.Ceiling(len));
						}
						break;
                    #endregion
                    #region Implementation - Discord webhook
                    case ActionTypeEnum.DiscordWebhook:
                        {
                            string msg = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DiscordWebhookMessage);
                            string url = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DiscordWebhookURL);
                            if (_DiscordTts == true)
                            {
                                if (msg.Length > 1970)
                                {
                                    msg = msg.Substring(0, 1970);
                                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/warndiscordtrunc", "Discord message too long, capping to {0}", msg.Length));
                                }
                                var wh = new JavaScriptSerializer().Serialize(new { content = msg, tts = true });
                                SendJson(ctx, HTTPMethodEnum.POST, url, wh, null, true);
                            }
                            else
                            {
                                if (msg.Length > 1980)
                                {
                                    msg = msg.Substring(0, 1980);
                                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/warndiscordtrunc", "Discord message too long, capping to {0}", msg.Length));
                                }
                                var wh = new JavaScriptSerializer().Serialize(new { content = msg });
                                SendJson(ctx, HTTPMethodEnum.POST, url, wh, null, true);
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Disk operation
                    case ActionTypeEnum.DiskFile:
                        {
                            string filename = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DiskFileOpName);
                            string varname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DiskFileOpVar);
                            VariableStore vs = (_DiskPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                            if (_DiskFileOp == DiskFileOpEnum.ReadCSVIntoTableVariable || _DiskFileOp == DiskFileOpEnum.ReadIntoListVariable || _DiskFileOp == DiskFileOpEnum.ReadIntoVariable)
                            {                                
                                Uri u = new Uri(filename);
                                if (u.IsFile == false)
                                {
                                    string fn = Path.Combine(ctx.plug.path, "TriggernometryFileCache");
                                    if (Directory.Exists(fn) == false)
                                    {
                                        Directory.CreateDirectory(fn);
                                    }
                                    string ext = Path.GetExtension(u.LocalPath);
                                    fn = Path.Combine(fn, ctx.plug.GenerateHash(u.AbsoluteUri) + Path.GetExtension(u.LocalPath));
                                    bool fromcache = false;
                                    if (File.Exists(fn) == true && _DiskFileCache == true)
                                    {
                                        FileInfo fi = new FileInfo(fn);
                                        DateTime dt = DateTime.Now.AddMinutes(0 - ctx.plug.cfg.CacheFileExpiry);
                                        if (fi.LastWriteTime > dt)
                                        {
                                            filename = fn;
                                            fromcache = true;
                                        }
                                    }
                                    if (fromcache == false)
                                    {
                                        using (WebClient wc = new WebClient())
                                        {
                                            wc.Headers["User-Agent"] = "Triggernometry File Retriever";
                                            byte[] data = wc.DownloadData(u.AbsoluteUri);
                                            File.WriteAllBytes(fn, data);
                                            filename = fn;
                                        }
                                    }
                                }
                            }
                            switch (_DiskFileOp)
                            {
                                case DiskFileOpEnum.ReadCSVIntoTableVariable:
                                    {
                                        List<string[]> data = new List<string[]>();
                                        int datawidth = 0;
                                        using (StreamReader sr = new StreamReader(filename))
                                        {
                                            using (CsvReader csv = new CsvReader(sr))
                                            {
                                                string[] x;
                                                while ((x = csv.Parser.Read()) != null)
                                                {
                                                    if (x.Length > datawidth)
                                                    {
                                                        datawidth = x.Length;
                                                    }
                                                    data.Add(x);
                                                }
                                            }
                                        }
                                        VariableTable vt = GetTableVariable(vs, varname, true);
                                        if (data.Count > 0 && datawidth > 0)
                                        {
                                            string vtchanger;
                                            if (ctx.trig != null)
                                            {
                                                vtchanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                            }
                                            else
                                            {
                                                vtchanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                            }
                                            vt.Resize(datawidth, data.Count);
                                            int y = 1;
                                            foreach (string[] row in data)
                                            {
                                                for (int x = 0; x < row.Length; x++)
                                                {
                                                    vt.Set(x + 1, y, row[x], vtchanger);
                                                }
                                                y++;
                                            }
                                        }
                                    }
                                    break;
                                case DiskFileOpEnum.ReadIntoListVariable:
                                    {
                                        string[] data = File.ReadAllLines(filename);
                                        lock (vs.List) // verified
                                        {
                                            if (vs.List.ContainsKey(varname) == false)
                                            {
                                                vs.List[varname] = new VariableList();
                                            }
                                            VariableList x = vs.List[varname];
                                            foreach (string dat in data)
                                            {
                                                x.Push(new VariableScalar() { Value = dat }, "");
                                            }
                                            if (ctx.trig != null)
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                            }
                                            else
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                            }
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/filelistset", "List variable ({0}) value read from file ({1})", varname, filename));
                                    }
                                    break;
                                case DiskFileOpEnum.ReadIntoVariable:
                                    {
                                        string data = File.ReadAllText(filename);
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(varname) == false)
                                            {
                                                vs.Scalar[varname] = new VariableScalar();
                                            }
                                            VariableScalar x = vs.Scalar[varname];
                                            x.Value = data;
                                            if (ctx.trig != null)
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                            }
                                            else
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                            }
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/filescalarset", "Scalar variable ({0}) value read from file ({1})", varname, filename));
                                    }
                                    break;
                                case DiskFileOpEnum.AppendNewLineIntoFile:
                                    {
                                        string data = varname + "\n";
                                        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(data);
                                        using (FileStream fsWrite = new FileStream(filename, FileMode.Append))
                                        {
                                            fsWrite.Write(myByte, 0, myByte.Length);
                                        };
                                    }
                                    break;
                                case DiskFileOpEnum.ClearAndRewriteLineIntoFile:
                                    {
                                        string data = varname;
                                        if (data.Length > 0)
                                        {
                                            data += "\n";
                                        }
                                        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(data);
                                        using (FileStream fsWrite = new FileStream(filename, FileMode.Create))
                                        {
                                            fsWrite.Write(myByte, 0, myByte.Length);
                                        };
                                    }
                                    break;
                                case DiskFileOpEnum.ClearAndRewriteListVariableIntoFile:
                                    {
                                        string data = "";
                                        VariableList vl = GetListVariable(vs, varname, false);
                                        for (int i_row = 0; i_row < vl.Values.Count; i_row++)
                                        {
                                            data += vl.Values[i_row] + "\n";
                                        }
                                        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(data);
                                        using (FileStream fsWrite = new FileStream(filename, FileMode.Create))
                                        {
                                            fsWrite.Write(myByte, 0, myByte.Length);
                                        };
                                    }
                                    break;
                                case DiskFileOpEnum.ClearAndRewriteTableVariableIntoFile:
                                    {
                                        string data = "";
                                        VariableTable vt = GetTableVariable(vs, varname, false);
                                        for(int i_row = 0; i_row < vt.Rows.Count; i_row++)
                                        {
                                            for(int i_col = 0; i_col < vt.Rows[i_row].Values.Count; i_col++)
                                            {
                                                data += vt.Rows[i_row].Values[i_col];
                                                if(i_col<vt.Rows[i_row].Values.Count-1) data += ",";
                                            }
                                            data += "\n";
                                        }
                                        byte[] myByte = System.Text.Encoding.UTF8.GetBytes(data);
                                        using (FileStream fsWrite = new FileStream(filename, FileMode.Create))
                                        {
                                            fsWrite.Write(myByte, 0, myByte.Length);
                                        };
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - End encounter
                    case ActionTypeEnum.EndEncounter:
                        {
                            switch (_EncounterOp)
                            {
                                case EncounterOpEnum.EndEncounter:
                                    ctx.plug.EndCombatHook();
                                    break;
                                case EncounterOpEnum.StartEncounter:
                                    string swingType = _EncounterSwingType.ToString();
                                    string ability = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _EncounterAbilityType);
                                    string actor = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _EncounterActorName);
                                    string target = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _EncounterTargetName);
                                    int damage = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _EncounterDamage);
                                    string damageType = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _EncounterDamageType);
                                    ctx.plug.StartCombatHook(swingType,ability, actor, target, damage, damageType);
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Execute script
                    case ActionTypeEnum.ExecuteScript:
                        {
                            CodeDomProvider pr = null;
                            switch (_ExecScriptType)
                            {
                                case ScriptTypeEnum.CSharp:
                                    pr = CodeDomProvider.CreateProvider("CSharp");
                                    break;
                                case ScriptTypeEnum.VBScript:
                                    pr = CodeDomProvider.CreateProvider("VisualBasic");
                                    break;
                            }
                            using (pr)
                            {
                                CompilerParameters cp = new CompilerParameters();
                                cp.GenerateExecutable = true;
                                cp.GenerateInMemory = true;
                                cp.TreatWarningsAsErrors = false;
                                string assy = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ExecScriptAssembliesExpression);
                                foreach (string sass in assy.Split(",".ToArray()))
                                {
                                    string saf = sass.Trim();
                                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/addingassembly", "Adding assembly {0}", saf));
                                    cp.ReferencedAssemblies.Add(saf);
                                }
                                List<string> temp = new List<string>();
                                temp.Add(ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ExecScriptExpression));
                                CompilerResults cr = pr.CompileAssemblyFromSource(cp, temp.ToArray());
                                if (cr.Errors.Count > 0)
                                {
                                    if (ctx.testmode == true)
                                    {
                                        string erm = "";
                                        if (cr.Errors.Count > 1)
                                        {
                                            erm = I18n.Translate("internal/Action/scripterrorplural", "{0} errors occurred while compiling the script.", cr.Errors.Count);
                                        }
                                        else
                                        {
                                            erm = I18n.Translate("internal/Action/scripterrorsingular", "An error occurred while compiling the script.");
                                        }
                                        erm += " ";
                                        if (cr.Errors.Count > 5)
                                        {
                                            erm += I18n.Translate("internal/Action/fivescripterrorsare", "The first five errors are:");
                                        }
                                        else
                                        {
                                            if (cr.Errors.Count == 1)
                                            {
                                                erm += I18n.Translate("internal/Action/scripterroris", "The error is:");
                                            }
                                            else
                                            {
                                                erm += I18n.Translate("internal/Action/scripterrorsare", "The errors are:");
                                            }
                                        }
                                        int num = 0;
                                        foreach (CompilerError ce in cr.Errors)
                                        {
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/scripterror", "Script error: {0} @ line {1}", ce.ErrorText, ce.Line));
                                            erm += Environment.NewLine + Environment.NewLine + I18n.Translate("internal/Action/shortscripterror", "{0} @ line {1}", ce.ErrorText, ce.Line);
                                            num++;
                                            if (num >= 5)
                                            {
                                                break;
                                            }
                                        }
                                        MessageBox.Show(erm, I18n.Translate("internal/Action/scriptexecerror", "Script execution error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                    else
                                    {
                                        int num = 0;
                                        foreach (CompilerError ce in cr.Errors)
                                        {
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/scripterror", "Script error: {0} @ line {1}", ce.ErrorText, ce.Line));
                                            num++;
                                            if (num >= 5)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }
                                cr.CompiledAssembly.EntryPoint.Invoke(null, null);
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Folder operation
                    case ActionTypeEnum.Folder:
                        {
                            Folder f = ctx.plug.GetFolderById(_FolderId, ctx.trig != null ? ctx.trig.Repo : null);
                            if (f != null)
                            {
                                switch (_FolderOp)
                                {
                                    case FolderOpEnum.DisableFolder:
                                        {
                                            f.Enabled = false;
                                            TreeNode tn;
                                            if (ctx.trig == null || ctx.trig.Repo == null)
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[0], f);
                                            }
                                            else
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[1], f);
                                            }
                                            if (tn != null)
                                            {
                                                tn.Checked = false;
                                            }
                                            else
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/notreenodefolderwithid", "Didn't find a tree node for folder ({0}) with id ({1})", f.Name, f.Id));
                                            }
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/disabledfolderwithid", "Disabled folder ({0}) with id ({1})", f.Name, f.Id));
                                        }
                                        break;
                                    case FolderOpEnum.EnableFolder:
                                        {
                                            f.Enabled = true;
                                            TreeNode tn;
                                            if (ctx.trig == null || ctx.trig.Repo == null)
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[0], f);
                                            }
                                            else
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[1], f);
                                            }
                                            if (tn != null)
                                            {
                                                tn.Checked = true;
                                            }
                                            else
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/notreenodefolderwithid", "Didn't find a tree node for folder ({0}) with id ({1})", f.Name, f.Id));
                                            }
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/enabledfolderwithid", "Enabled folder ({0}) with id ({1})", f.Name, f.Id));
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/nofolderwithid", "Didn't find a folder with id ({0})", _FolderId));
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Image aura
                    case ActionTypeEnum.Aura:
                        {
                            ctx.plug.ImageAuraManagement(ctx, this);
                        }
                        break;
                    #endregion
                    #region Implementation - JSON
                    case ActionTypeEnum.GenericJson:
                        {
                            string response = "";
                            string endpoint = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _JsonEndpointExpression);
                            string payload = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _JsonPayloadExpression);
                            string headers = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _JsonHeaderExpression).Trim();
                            List<string> headerslist = new List<string>();
                            if (headers.Length > 0)
                            {
                                headerslist.AddRange(headers.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
                            }
                            if (_JsonCacheRequest == true)
                            {
                                string endpointh = ctx.plug.GenerateHash(endpoint);
                                string payloadh = ctx.plug.GenerateHash(payload);
                                string headersh = ctx.plug.GenerateHash(headers);
                                string fh = ctx.plug.GenerateHash(endpointh + payloadh + headers);
                                string fn = Path.Combine(ctx.plug.path, "TriggernometryJsonCache");
                                if (Directory.Exists(fn) == false)
                                {
                                    Directory.CreateDirectory(fn);
                                }
                                fn = Path.Combine(fn, fh + ".json");
                                bool fromcache = false;
                                if (File.Exists(fn) == true)
                                {
                                    FileInfo fi = new FileInfo(fn);
                                    DateTime dt = DateTime.Now.AddMinutes(0 - ctx.plug.cfg.CacheJsonExpiry);
                                    if (fi.LastWriteTime > dt)
                                    {
                                        response = File.ReadAllText(fn);
                                        fromcache = true;
                                    }
                                }
                                if (fromcache == false)
                                {
                                    response = SendJson(ctx, _JsonOperationType, endpoint, payload, headerslist, false);
                                    File.WriteAllText(fn, response);
                                }
                            }
                            else
                            {
                                response = SendJson(ctx, _JsonOperationType, endpoint, payload, headerslist, false);
                            }
                            if (_JsonFiringExpression != null && _JsonFiringExpression.Trim().Length > 0)
                            {
                                string firing = "";
                                lock (ctx.contextResponse)
                                {
                                    ctx.contextResponse = response;
                                    firing = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _JsonFiringExpression);
                                }
                                if (firing.Length > 0)
                                {
                                    ctx.plug.LogLineQueuer(firing, "", LogEvent.SourceEnum.Log);
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Keypress
                    case ActionTypeEnum.KeyPress:
                        {
                            if (_KeypressType == KeypressTypeEnum.SendKeys)
                            {
                                if (ctx.testmode == true)
                                {
                                    Thread.Sleep(2000);
                                }
                                string ks = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _KeyPressExpression);
                                SendKeys.SendWait(ks);
                            }
                            else
                            {
                                string window = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _KeyPressWindow);
                                int keycode = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _KeyPressCode);
                                RealPlugin.WindowsUtils.SendKeycode(window, (ushort)keycode);
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Launch process
                    case ActionTypeEnum.LaunchProcess:
                        {
                            System.Diagnostics.Process p = new System.Diagnostics.Process();
                            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                            psi.Arguments = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LaunchProcessCmdlineExpression);
                            psi.WindowStyle = _LaunchProcessWindowStyle;
                            psi.WorkingDirectory = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LaunchProcessWorkingDirExpression);
                            psi.FileName = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LaunchProcessPathExpression);
                            p.StartInfo = psi;
                            p.Start();
                            if (_Asynchronous == false)
                            {
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/waitingprocexit", "Waiting for process to exit"));
                                p.WaitForExit();
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - List variable
                    case ActionTypeEnum.ListVariable:
                        {
                            string sourcename = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableName);
                            string changer;
                            if (ctx.trig != null)
                            {
                                changer = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                            }
                            else
                            {
                                changer = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                            }
                            switch (_ListVariableOp)
                            {
                                case ListVariableOpEnum.Unset:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List) // verified
                                        { 
                                            if (vs.List.ContainsKey(sourcename) == true)
                                            {
                                                vs.List.Remove(sourcename);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listunset", "List variable ({0}) unset", sourcename));
                                        
                                    }
                                    break;
                                case ListVariableOpEnum.Push:
                                    {
                                        string value = GetListExpressionValue(ctx, _ListVariableExpressionType, _ListVariableExpression);
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, true);
                                            vl.Push(new VariableScalar() { Value = value }, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listpush", "Value ({0}) pushed to the end of list variable ({1})", value, sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.Insert:
                                    {
                                        string value = GetListExpressionValue(ctx, _ListVariableExpressionType, _ListVariableExpression);
                                        int index = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _ListVariableIndex);
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, true);
                                            vl.Insert(index, value, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listindexinsert", "Value ({0}) inserted to index ({1}) of list variable ({2})", value, index, sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.Set:
                                    {
                                        string value = GetListExpressionValue(ctx, _ListVariableExpressionType, _ListVariableExpression);
                                        int index = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _ListVariableIndex);
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, true);
                                            vl.Set(index, value, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listindexset", "Value ({0}) set to index ({1}) of list variable ({2})", value, index, sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.Remove:
                                    {
                                        int index = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _ListVariableIndex);
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.Remove(index, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listindexunset", "Value removed from index ({0}) of list variable ({1})", index, sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.PopLast:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        string newval = "";
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            newval = vl.StackPop(changer).ToString();
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(targetname) == false)
                                            {
                                                vs.Scalar[targetname] = new VariableScalar();
                                            }
                                            VariableScalar x = vs.Scalar[targetname];
                                            x.Value = newval;
                                            x.LastChanger = changer;
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listpopend", "Value ({0}) popped from the end of list variable ({1}) into scalar variable ({2})", newval, sourcename, targetname));
                                    }
                                    break;
                                case ListVariableOpEnum.PopFirst:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        string newval = "";
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            newval = vl.QueuePop(changer).ToString();
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(targetname) == false)
                                            {
                                                vs.Scalar[targetname] = new VariableScalar();
                                            }
                                            VariableScalar x = vs.Scalar[targetname];
                                            x.Value = newval;
                                            x.LastChanger = changer;
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listpopbegin", "Value ({0}) popped from the beginning of list variable ({1}) into scalar variable ({2})", newval, sourcename, targetname));
                                    }
                                    break;
                                case ListVariableOpEnum.SortAlphaAsc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortAlphaAsc(changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortasc", "List variable ({0}) sorted in ascending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.SortAlphaDesc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortAlphaDesc(changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortdesc", "List variable ({0}) sorted in descending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.SortNumericAsc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortNumericAsc(changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortasc", "List variable ({0}) sorted in ascending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.SortNumericDesc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortNumericDesc(changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortdesc", "List variable ({0}) sorted in descending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.SortFfxivPartyAsc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortFfxivPartyAsc(ctx.plug.cfg, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortffxivasc", "List variable ({0}) sorted in FFXIV party ascending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.SortFfxivPartyDesc:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            vl.SortFfxivPartyDesc(ctx.plug.cfg, changer);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listsortffxivdesc", "List variable ({0}) sorted in FFXIV party descending order", sourcename));
                                    }
                                    break;
                                case ListVariableOpEnum.Copy:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        VariableList vl = null;
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            vl = GetListVariable(vs, sourcename, false);
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList newvl = new VariableList();
                                            foreach (Variable x in vl.Values)
                                            {
                                                newvl.Push(x.Duplicate(), changer);
                                            }
                                            vs.List[targetname] = newvl;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listcopy", "List variable ({0}) copied to list variable ({1})", sourcename, targetname));
                                    }
                                    break;
                                case ListVariableOpEnum.Filter:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        VariableList vl = null;
                                        VariableList v2 = null;
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            vl = GetListVariable(vs, sourcename, false);
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            if (!vs.List.ContainsKey(targetname))
                                            {
                                                VariableList newvl = new VariableList();
                                                foreach (Variable x in vl.Values)
                                                {
                                                    newvl.Push(x.Duplicate(), changer);
                                                }
                                                vs.List[targetname] = newvl;
                                            }
                                            else
                                            {
                                                v2 = GetListVariable(vs, targetname, false);
                                                int i = 0;
                                                while (i<v2.Size())
                                                {

                                                    if (vl.IndexOf(v2.Values[i]) <= 0)
                                                    {
                                                        v2.Remove(i+1, changer);
                                                        i--;
                                                    }

                                                    i++;
                                                }
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listcopy", "List variable ({0}) used as a filter to list variable ({1})", sourcename, targetname));
                                    }
                                    break;
                                case ListVariableOpEnum.ReverseFilter:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        VariableList vl = null;
                                        VariableList v2 = null;
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            vl = GetListVariable(vs, sourcename, false);
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            v2 = GetListVariable(vs, targetname, false);
                                            foreach (Variable x in vl.Values)
                                            {
                                                if (v2.IndexOf(x) > 0)
                                                {
                                                    v2.Remove(v2.IndexOf(x), changer);
                                                }

                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listcopy", "List variable ({0}) used as a reverse filter to list variable ({1})", sourcename, targetname));
                                    }
                                    break;
                                case ListVariableOpEnum.InsertList:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        int index = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _ListVariableIndex);
                                        int rindex = index;
                                        VariableList vl = null;
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            vl = GetListVariable(vs, sourcename, false);
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList newvl = GetListVariable(vs, targetname, true);
                                            foreach (Variable x in vl.Values)
                                            {
                                                newvl.Insert(rindex, x.Duplicate(), changer);
                                                rindex++;
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listinsertlist", "List variable ({0}) inserted to list variable ({1}) at index ({2})", sourcename, targetname, index));
                                    }
                                    break;
                                case ListVariableOpEnum.Join:
                                    {
                                        string separator = GetListExpressionValue(ctx, _ListVariableExpressionType, _ListVariableExpression);
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        string newval = "";
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList vl = GetListVariable(vs, sourcename, false);
                                            newval = vl.Join(separator);
                                        }
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(targetname) == false)
                                            {
                                                vs.Scalar[targetname] = new VariableScalar();
                                            }
                                            VariableScalar x = vs.Scalar[targetname];
                                            x.Value = newval;
                                            x.LastChanger = changer;
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/listscalarjoin", "List variable ({0}) joined to scalar variable ({1}) with separator ({2})", sourcename, targetname, separator));
                                    }
                                    break;
                                case ListVariableOpEnum.Split: // todo
                                    {
                                        string separator = GetListExpressionValue(ctx, _ListVariableExpressionType, _ListVariableExpression);
                                        string newname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _ListVariableTarget);
                                        string splitval = "";
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(sourcename) == true)
                                            {
                                                splitval = vs.Scalar[sourcename].Value;
                                            }
                                        }
                                        string[] vals = splitval.Split(new string[] { separator }, StringSplitOptions.None);
                                        vs = (_ListTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List)
                                        {
                                            VariableList newvl = new VariableList();
                                            foreach (string x in vals)
                                            {
                                                newvl.Push(new VariableScalar() { Value = x }, changer);
                                            }
                                            vs.List[newname] = newvl;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/scalarlistsplit", "Scalar variable ({0}) split into list variable ({1}) with separator ({2})", sourcename, newname, separator));
                                    }
                                    break;
                                case ListVariableOpEnum.UnsetAll:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.List) // verified
                                        {
                                            vs.List.Clear();
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/alllistunset", "All list variables unset"));
                                    }
                                    break;
                                case ListVariableOpEnum.UnsetRegex:
                                    {
                                        VariableStore vs = (_ListSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        Regex rx = new Regex(_ListVariableName);
                                        List<string> toRem = new List<string>();
                                        lock (vs.List) // verified
                                        {
                                            foreach (KeyValuePair<string, VariableList> kp in vs.List)
                                            {
                                                if (rx.IsMatch(kp.Key) == true)
                                                {
                                                    toRem.Add(kp.Key);
                                                }
                                            }
                                            foreach (string vn in toRem)
                                            {
                                                vs.List.Remove(vn);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/regexlistunset", "All list variables matching ({0}) unset", _ListVariableName));
                                        break;
                                    }
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Log message
                    case ActionTypeEnum.LogMessage:
                        {
                            if (_LogProcess == true)
                            {
                                ctx.plug.LogLineQueuer(ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LogMessageText), "", LogEvent.SourceEnum.Log);
                            }
                            if (_LogReparse == true)
                            {
                                ACT_Reparse(ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LogMessageText));
                            }

                            RealPlugin.DebugLevelEnum dl = RealPlugin.DebugLevelEnum.Error;
                            switch (_LogLevel)
                            {
                                case LogMessageEnum.Error: dl = RealPlugin.DebugLevelEnum.Error; break;
                                case LogMessageEnum.Info: dl = RealPlugin.DebugLevelEnum.Info; break;
                                case LogMessageEnum.Verbose: dl = RealPlugin.DebugLevelEnum.Verbose; break;
                                case LogMessageEnum.Warning: dl = RealPlugin.DebugLevelEnum.Warning; break;
                            }
                            AddToLog(ctx, dl, ctx.EvaluateStringExpression(ActionContextLogger, ctx, _LogMessageText));

                        }
                        break;
                    #endregion
                    #region Implementation - Message box
                    case ActionTypeEnum.MessageBox:
                        {
                            MessageBox.Show(ctx.EvaluateStringExpression(ActionContextLogger, ctx, _MessageBoxText), "", MessageBoxButtons.OK, (System.Windows.Forms.MessageBoxIcon)_MessageBoxIconType);
                        }
                        break;
                    #endregion
                    #region Implementation - Mutex
                    case ActionTypeEnum.Mutex:
                        {
                            string mn = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _MutexName);
                            switch (_MutexOpType)
                            {
                                case MutexOpEnum.Acquire:
                                    {
                                        RealPlugin.MutexInformation mi = ctx.plug.GetMutex(mn);
                                        mi.Acquire(ctx);
                                    }
                                    break;
                                case MutexOpEnum.Release:
                                    {
                                        RealPlugin.MutexInformation mi = ctx.plug.GetMutex(mn);
                                        mi.Release(ctx);
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - OBS
                    case ActionTypeEnum.ObsControl:
                        if (ctx.plug._obs != null)
                        {
                            lock (ctx.plug._obs)
                            {
                                if (ObsConnector(ctx) == true)
                                {
                                    try
                                    {
                                        switch (_OBSControlType)
                                        {
                                            case ObsControlTypeEnum.StartStreaming:
                                                ctx.plug._obs.StartStreaming();
                                                break;
                                            case ObsControlTypeEnum.StopStreaming:
                                                ctx.plug._obs.StopStreaming();
                                                break;
                                            case ObsControlTypeEnum.ToggleStreaming:
                                                ctx.plug._obs.ToggleStreaming();
                                                break;
                                            case ObsControlTypeEnum.StartRecording:
                                                ctx.plug._obs.StartRecording();
                                                break;
                                            case ObsControlTypeEnum.StopRecording:
                                                ctx.plug._obs.StopRecording();
                                                break;
                                            case ObsControlTypeEnum.ToggleRecording:
                                                ctx.plug._obs.ToggleRecording();
                                                break;
                                            case ObsControlTypeEnum.StartReplayBuffer:
                                                ctx.plug._obs.StartReplayBuffer();
                                                break;
                                            case ObsControlTypeEnum.StopReplayBuffer:
                                                ctx.plug._obs.StopReplayBuffer();
                                                break;
                                            case ObsControlTypeEnum.ToggleReplayBuffer:
                                                ctx.plug._obs.ToggleReplayBuffer();
                                                break;
                                            case ObsControlTypeEnum.SaveReplayBuffer:
                                                ctx.plug._obs.SaveReplayBuffer();
                                                break;
                                            case ObsControlTypeEnum.SetScene:
                                                {
                                                    string scn = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSSceneName);
                                                    ctx.plug._obs.SetCurrentScene(scn);
                                                }
                                                break;
                                            case ObsControlTypeEnum.ShowSource:
                                                {
                                                    string scn = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSSceneName);
                                                    string src = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSSourceName);
                                                    ctx.plug._obs.ShowSource(scn, src);
                                                }
                                                break;
                                            case ObsControlTypeEnum.HideSource:
                                                {
                                                    string scn = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSSceneName);
                                                    string src = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSSourceName);
                                                    ctx.plug._obs.HideSource(scn, src);
                                                }
                                                break;
                                            case ObsControlTypeEnum.JSONPayload:
                                                {
                                                    string json = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _OBSJSONPayload);
                                                    ctx.plug._obs.JSONPayload(json);
                                                }
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/obscontrolexception", "Can't execute OBS control action due to exception: " + ex.Message));
                                    }
                                }
                                else
                                {
                                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/obscontrolerror", "Can't execute OBS control action due to error"));
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Play sound
                    case ActionTypeEnum.PlaySound:
						{
                            ctx.soundhook(ctx, this);
                        }
                        break;
                    #endregion
                    #region Implementation - Placeholder
                    case ActionTypeEnum.Placeholder:
                        break;
                    #endregion
                    #region Implementation - Play speech
                    case ActionTypeEnum.UseTTS:
						{
                            ctx.ttshook(ctx, this);
						}
						break;
                    #endregion
                    #region Implementation - Scalar variable
                    case ActionTypeEnum.Variable:
                        {
                            string varname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _VariableName);
                            string newval;
                            VariableStore vs = (_VariablePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                            switch (_VariableOp)
                            {
                                case VariableOpEnum.UnsetAll:
                                    {
                                        lock (vs.Scalar) // verified
                                        {
                                            vs.Scalar.Clear();
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/allscalarunset", "All scalar variables unset"));
                                        break;
                                    }
                                case VariableOpEnum.UnsetRegex:
                                    {
                                        Regex rx = new Regex(_VariableName);
                                        List<string> toRem = new List<string>();
                                        lock (vs.Scalar) // verified
                                        {
                                            foreach (KeyValuePair<string, VariableScalar> kp in vs.Scalar)
                                            {
                                                if (rx.IsMatch(kp.Key) == true)
                                                {
                                                    toRem.Add(kp.Key);
                                                }
                                            }
                                            foreach (string vn in toRem)
                                            {
                                                vs.Scalar.Remove(vn);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/regexscalarunset", "All scalar variables matching ({0}) unset", _VariableName));
                                        break;
                                    }
                                case VariableOpEnum.Unset:
                                    {
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(varname) == true)
                                            {
                                                vs.Scalar.Remove(varname);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/scalarunset", "Scalar variable ({0}) unset", varname));
                                        break;
                                    }
                                case VariableOpEnum.SetString:
                                case VariableOpEnum.SetNumeric:
                                    {
                                        if (_VariableOp == VariableOpEnum.SetString)
                                        {
                                            newval = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _VariableExpression);
                                        }
                                        else
                                        {
                                            newval = I18n.ThingToString(ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _VariableExpression));
                                        }
                                        lock (vs.Scalar) // verified
                                        {
                                            if (vs.Scalar.ContainsKey(varname) == false)
                                            {
                                                vs.Scalar[varname] = new VariableScalar();
                                            }
                                            VariableScalar x = vs.Scalar[varname];
                                            x.Value = newval;
                                            if (ctx.trig != null)
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                            }
                                            else
                                            {
                                                x.LastChanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                            }
                                            x.LastChanged = DateTime.Now;
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/scalarset", "Scalar variable ({0}) value set to ({1})", varname, newval));
                                        break;
                                    }
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Table variable
                    case ActionTypeEnum.TableVariable:
                        {
                            string sourcename = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TableVariableName);
                            string newval;
                            switch (_TableVariableOp)
                            {
                                case TableVariableOpEnum.UnsetAll:
                                    {
                                        VariableStore vs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Table) // verified
                                        {
                                            vs.Table.Clear();
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/alltableunset", "All table variables unset"));
                                        break;
                                    }
                                case TableVariableOpEnum.UnsetRegex:
                                    {
                                        Regex rx = new Regex(_TableVariableName);
                                        List<string> toRem = new List<string>();
                                        VariableStore vs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Table) // verified
                                        {
                                            foreach (KeyValuePair<string, VariableTable> kp in vs.Table)
                                            {
                                                if (rx.IsMatch(kp.Key) == true)
                                                {
                                                    toRem.Add(kp.Key);
                                                }
                                            }
                                            foreach (string vn in toRem)
                                            {
                                                vs.Table.Remove(vn);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/regextableunset", "All table variables matching ({0}) unset", _TableVariableName));
                                        break;
                                    }
                                case TableVariableOpEnum.Resize:
                                    {
                                        int w = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _TableVariableX);
                                        int h = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _TableVariableY);
                                        VariableStore vs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Table) // verified
                                        {
                                            VariableTable vt = null;
                                            if (vs.Table.ContainsKey(sourcename) == true)
                                            {
                                                vt = vs.Table[sourcename];
                                            }
                                            else
                                            {
                                                vt = new VariableTable();
                                            }
                                            vt.Resize(w, h);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/tableresized", "Table variable ({0}) resized to ({1},{2})", sourcename, w, h));
                                        break;
                                    }
                                case TableVariableOpEnum.Unset:
                                    {
                                        VariableStore vs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Table) // verified
                                        {
                                            if (vs.Table.ContainsKey(sourcename) == true)
                                            {
                                                vs.Table.Remove(sourcename);
                                            }
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/tableunset", "Table variable ({0}) unset", sourcename));
                                        break;
                                    }
                                case TableVariableOpEnum.Copy:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TableVariableTarget);
                                        VariableStore svs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        VariableStore tvs = (_TableTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        VariableTable vt = null;
                                        lock (svs.Table) // verified
                                        {
                                            if (svs.Table.ContainsKey(sourcename) == true)
                                            {
                                                vt = (VariableTable)svs.Table[sourcename].Duplicate();
                                            }
                                        }
                                        if (vt != null)
                                        {
                                            lock (tvs.Table)
                                            {
                                                tvs.Table[targetname] = vt;
                                            }
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/tablecopied", "Table variable ({0}) copied to ({1})", sourcename, targetname));
                                        }
                                        else
                                        {
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/tablecopynotexist", "Table variable ({0}) couldn't be copied to ({1}) since it doesn't exist", sourcename, targetname));
                                        }
                                        break;
                                    }
                                case TableVariableOpEnum.Append:
                                    {
                                        string targetname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TableVariableTarget);
                                        VariableStore svs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        VariableStore tvs = (_TableTargetPersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        VariableTable vt = null;
                                        lock (svs.Table) // verified
                                        {
                                            if (svs.Table.ContainsKey(sourcename) == true)
                                            {
                                                vt = (VariableTable)svs.Table[sourcename].Duplicate();
                                            }
                                        }
                                        if (vt != null)
                                        {
                                            VariableTable tgt = null;
                                            lock (tvs.Table)
                                            {
                                                if (tvs.Table.ContainsKey(targetname) == true)
                                                {
                                                    tgt = tvs.Table[targetname];
                                                    string vtchanger;
                                                    if (ctx.trig != null)
                                                    {
                                                        vtchanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                                    }
                                                    else
                                                    {
                                                        vtchanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                                    }
                                                    tgt.Append(vt, vtchanger);
                                                }
                                                else
                                                {
                                                    tvs.Table[targetname] = vt;
                                                }
                                            }
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/tableappended", "Table variable ({0}) appended to ({1})", sourcename, targetname));
                                        }
                                        else
                                        {
                                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/tableappendnotexist", "Table variable ({0}) couldn't be appended to ({1}) since it doesn't exist", sourcename, targetname));
                                        }
                                        break;
                                    }
                                case TableVariableOpEnum.Set:
                                    {
                                        int x = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _TableVariableX);
                                        int y = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _TableVariableY);
                                        if (_TableVariableExpressionType == TableVariableExpTypeEnum.String)
                                        {
                                            newval = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TableVariableExpression);
                                        }
                                        else
                                        {
                                            newval = I18n.ThingToString(ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _TableVariableExpression));
                                        }
                                        VariableStore vs = (_TableSourcePersist == false) ? ctx.plug.sessionvars : ctx.plug.cfg.PersistentVariables;
                                        lock (vs.Table) // verified
                                        {
                                            VariableTable vt = GetTableVariable(vs, sourcename, true);
                                            int mx = Math.Max(x, vt.Width);
                                            int my = Math.Max(y, vt.Height);                                            
                                            if (mx != vt.Width || my != vt.Height)
                                            {
                                                vt.Resize(mx, my);
                                            }
                                            string vtchanger;
                                            if (ctx.trig != null)
                                            {
                                                vtchanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.trig.LogName, GetDescription(ctx));
                                            }
                                            else
                                            {
                                                vtchanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", GetDescription(ctx));
                                            }
                                            vt.Set(x, y, newval, vtchanger);
                                        }
                                        AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/scalarset", "Scalar variable ({0}) value set to ({1})", sourcename, newval));
                                        break;
                                    }
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Text aura
                    case ActionTypeEnum.TextAura:
                        {
                            ctx.plug.TextAuraManagement(ctx, this);
                        }
                        break;
                    #endregion
                    #region Implementation - Trigger operation
                    case ActionTypeEnum.Trigger:
						{
                            Trigger t = ctx.plug.GetTriggerById(_TriggerId, ctx.trig != null ? ctx.trig.Repo : null);
                            if (t != null)
                            {
                                switch (_TriggerOp)
                                {
                                    case TriggerOpEnum.CancelAllTrigger:
                                        ctx.plug.ClearActionQueue(ctx.trig);
                                        break;
                                    case TriggerOpEnum.CancelTrigger:
                                        ctx.plug.CancelAllQueuedActionsFromTrigger(t);
                                        break;
                                    case TriggerOpEnum.FireTrigger:
                                        {
                                            LogEvent le = new LogEvent();
                                            le.Text = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TriggerText);
                                            le.Zone = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TriggerZone);
                                            le.Timestamp = DateTime.Now;
                                            ctx.plug.TestTrigger(t, le, _TriggerForceType);
                                        }
                                        break;
                                    case TriggerOpEnum.EnableTrigger:
                                        {
                                            t.Enabled = true;
                                            TreeNode tn;
                                            if (ctx.trig == null || ctx.trig.Repo == null)
                                            {
                                                tn = ctx.plug.LocateNodeHostingTrigger(ctx.plug.ui.treeView1.Nodes[0], t);
                                            }
                                            else
                                            {
                                                tn = ctx.plug.LocateNodeHostingTrigger(ctx.plug.ui.treeView1.Nodes[1], t);
                                            }
                                            if (tn != null)
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/trigenable", "Trigger '{0}' enabled", t.LogName));
                                                tn.Checked = true;
                                            }
                                            else
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/notreenodetrigenable", "Could not find tree node to modify for enabling trigger {0}", t.LogName));
                                            }
                                        }
                                        break;
                                    case TriggerOpEnum.DisableTrigger:
                                        {
                                            t.Enabled = false;
                                            TreeNode tn;
                                            if (ctx.trig == null || ctx.trig.Repo == null)
                                            {
                                                tn = ctx.plug.LocateNodeHostingTrigger(ctx.plug.ui.treeView1.Nodes[0], t);
                                            }
                                            else
                                            {
                                                tn = ctx.plug.LocateNodeHostingTrigger(ctx.plug.ui.treeView1.Nodes[1], t);
                                            }
                                            if (tn != null)
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/trigdisable", "Trigger '{0}' disabled", t.LogName));
                                                tn.Checked = false;
                                            }
                                            else
                                            {
                                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/notreenodetrigdisable", "Could not find tree node to modify for disabling trigger {0}", t.LogName));
                                            }
                                        }
                                        break;
                                    case TriggerOpEnum.CopyTrigger:
                                        {
                                            TriggernometryExport exp = new TriggernometryExport() { ExportedTrigger = t };
                                            exp = TriggernometryExport.Unserialize(exp.Serialize());
                                            exp.ExportedTrigger.Name = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TriggerText);
                                            exp.ExportedTrigger.RegularExpression = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _TriggerZone);
                                            TreeNode tn;
                                            if (ctx.trig == null || ctx.trig.Repo == null)
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[0], t.Parent);
                                            }
                                            else
                                            {
                                                tn = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.Nodes[1], t.Parent);
                                            }
                                            if (tn != null)
                                            {
                                                using (Forms.ImportForm ifo = new Forms.ImportForm(ctx.plug))
                                                {
                                                    TreeNode parent = ctx.plug.LocateNodeHostingFolder(ctx.plug.ui.treeView1.TopNode, t.Parent);
                                                    ifo.BuildTreeFromExport(exp, null, null, false);
                                                    var trigger_new = (Trigger)ifo.treeView1.Nodes[0].Tag;
                                                    ctx.plug.ui.ImportResultsFromForm(ifo,tn);
                                                }
                                            }
                                            
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                if (_TriggerOp == TriggerOpEnum.CancelAllTrigger)
                                {
                                    ctx.plug.ClearActionQueue(ctx.trig);
                                }
                                else
                                {
                                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Warning, I18n.Translate("internal/Action/notrigiderror", "No trigger id, and op is not cancel all actions, unexpected"));
                                }
                            }
						}
                        break;
                    #endregion
                    #region Implementation - Window message
                    case ActionTypeEnum.WindowMessage:
                        {
                            string window = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _WmsgTitle);
                            int code = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _WmsgCode);
                            int wparam = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _WmsgWparam);
                            int lparam = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _WmsgLparam);
                            RealPlugin.WindowsUtils.SendMessageToWindow(window, (ushort)code, wparam, lparam);
                        }
                        break;
                    #endregion
                    #region Implementation - Mouse
                    case ActionTypeEnum.Mouse:
                        {
                            int mousex = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _MouseX);
                            int mousey = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _MouseY);
                            RealPlugin.WindowsUtils.MouseEventFlags flags = 0;
                            switch (_MouseCoordType)
                            {
                                case MouseCoordEnum.Absolute:
                                    flags |= RealPlugin.WindowsUtils.MouseEventFlags.ABSOLUTE;
                                    break;
                                case MouseCoordEnum.Relative:
                                    break;
                            }
                            switch (_MouseOpType)
                            {
                                case MouseOpEnum.Move:
                                    RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MOVE, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                    break;
                                case MouseOpEnum.LeftClick:
                                    Task.Run(() =>
                                    {
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MOVE, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.LEFTDOWN, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.LEFTUP, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                    });
                                    break;
                                case MouseOpEnum.MiddleClick:
                                    Task.Run(() =>
                                    {
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MOVE, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MIDDLEDOWN, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MIDDLEUP, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                    });
                                    break;
                                case MouseOpEnum.RightClick:
                                    Task.Run(() =>
                                    {
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.MOVE, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.RIGHTDOWN, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                        Thread.Sleep(10);
                                        RealPlugin.WindowsUtils.SendMouse(flags | RealPlugin.WindowsUtils.MouseEventFlags.RIGHTUP, RealPlugin.WindowsUtils.MouseEventDataXButtons.NONE, mousex, mousey);
                                    });
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Implementation - Named callback
                    case ActionTypeEnum.NamedCallback:
                        {
                            string cbname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _NamedCallbackName);
                            string cbparm = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _NamedCallbackParam);
                            string cbreturn = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _NamedCallbackReturnScalarName);
                            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/callbackinvoke", "Invoking named callback ({0}) with parameter ({1})", cbname, cbparm));
                            ctx.plug.InvokeNamedCallback(cbname, cbparm, cbreturn);
                        }
                        break;
                    #endregion
                    #region Implementation - Party order
                    case ActionTypeEnum.PartyOrder:
                        {
                            string playername = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _PartyOrderPlayerName);
                            int partyorder = (int)ctx.EvaluateNumericExpression(ActionContextLogger, ctx, _PartyOrderPartyOrder);

                            if (partyorder > 0 && partyorder <= 8)
                            {
                                AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/partyorder", "Override party order for player ({0}) to index ({1})", playername, partyorder.ToString()));
                                var oldID = PluginBridges.BridgeFFXIV.OverridePartyOrder.IndexOf(playername);
                                if (oldID != partyorder - 1)
                                {
                                    if (oldID >= 0)
                                    {
                                        PluginBridges.BridgeFFXIV.OverridePartyOrder[oldID] = "";
                                    }
                                    PluginBridges.BridgeFFXIV.OverridePartyOrder[partyorder - 1] = playername;

                                }

                            }
                            else if ((partyorder == 0) && (playername == ""))
                            {
                                for (int i = 0; i < PluginBridges.BridgeFFXIV.OverridePartyOrder.Count; i++)
                                {
                                    PluginBridges.BridgeFFXIV.OverridePartyOrder[i] = "";
                                }
                            }
                            PluginBridges.BridgeFFXIV.UpdateState();


                        }
                        break;
                    #endregion                   
                    #region Implementation - Party order
                    case ActionTypeEnum.DeveloperAction:
                        {

                            string devactionkey = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DevActionKey);
                            string devactionvalue = ctx.EvaluateStringExpression(ActionContextLogger, ctx, _DevActionValue);
                            object param = JsonConvert.DeserializeObject(devactionvalue);
                            
                            object property = PluginBridges.BridgeFFXIV.ReflectPlugin(devactionkey, devactionvalue);

                            //val = jsonSerializer.Serialize(obj);

                            //uint offset =Convert.ToUInt32(devactionvalue, 16);
                            //PluginBridges.BridgeFFXIV.SetFFXIVSignature(devactionkey, offset);
                            //PluginBridges.BridgeFFXIV.UpdateState();


                        }
                        break;
                        #endregion
                }
            }
			catch (Exception ex)
			{
                AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/exception", "Exception: {0}", ex.Message));
            }
            ContinueChain:
            if (NextAction != null)
            {
                DateTime dt = DateTime.Now.AddMilliseconds(this._DontExecute ? 0:ctx.EvaluateNumericExpression(ActionContextLogger, ctx,  NextAction.ExecutionDelayExpression));
                ctx.plug.QueueAction(ctx, ctx.trig, qa != null ? qa.mutex : null, NextAction, dt);
            }
		}
        void ACT_Reparse(string message, uint typenum = 13) {
            DateTime now = DateTime.Now;
            //var log = now.ToString("[yyyy/MM/dd HH:mm:ss.fff]") + " " + message;
            //this.defaultListener.Write(log);
            string[] textArray1 = new string[5];
            textArray1[0] = typenum.ToString().PadLeft(2, '0');
            textArray1[1] = "|";
            textArray1[2] = now.ToString("O");
            textArray1[3] = "|";
            textArray1[4] = message;
            string logLine = string.Concat(textArray1);
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, logLine);

        }

        internal void Mywmp_PlayStateChange(int NewState)
        {
            if ((WMPLib.WMPPlayState)NewState != WMPLib.WMPPlayState.wmppsStopped)
            {
                return;
            }
            WindowsMediaPlayer wmp = null;
            lock (players) // verified
            {
                do
                {
                    wmp = null;                    
                    foreach (WindowsMediaPlayer x in players)
                    {                        
                        if (x.playState == WMPPlayState.wmppsStopped)
                        {
                            wmp = x;
                            break;
                        }
                    }
                    if (wmp != null)
                    {
                        players.Remove(wmp);
                    }
                } while (wmp != null);
            }
        }

        internal void Mywmp_MediaError(object pMediaObject)
        {
            WindowsMediaPlayer wmp = (WindowsMediaPlayer)pMediaObject;
            lock (players) // verified
            {
                players.Remove(wmp);                
            }
        }

        internal void Execute(RealPlugin.QueuedAction qa, Context ctx)
        {
            if (_Asynchronous == true)
            {
                Task t;
                if (ctx.plug != null)
                {
                    CancellationToken ct = ctx.plug.GetCancellationToken();
                    t = new Task(() =>
                    {
                        ct.ThrowIfCancellationRequested();
                        ExecutionImplementation(qa, ctx);
                        if (qa != null)
                        {
                            qa.ActionFinished();
                        }
                    });
                }
                else
                {
                    t = new Task(() =>
                    {
                        ExecutionImplementation(qa, ctx);
                        if (qa != null)
                        {
                            qa.ActionFinished();
                        }
                    });
                }
                t.Start();
            }
            else
            {
                ExecutionImplementation(qa, ctx);
                if (qa != null)
                {
                    qa.ActionFinished();
                }
            }
        }
        internal string GetInheritString(string src,Context ctx)
        {
            string val = ctx.EvaluateStringExpression(ActionContextLogger, ctx, src);
            return val.Replace("\\$", "$").Replace("\\{", "{").Replace("\\}", "}");
        }
        internal void InheritSettingsTo(Action a,Context ctx)
        {
            CopySettingsTo(a);
            Type t = this.GetType();
            PropertyInfo[] pArray = t.GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
             Type ta = a.GetType();
            Array.ForEach<PropertyInfo>(pArray, p =>
            {
                try
                {
                    PropertyInfo pa = ta.GetProperty(p.Name, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance);
                    if (pa.PropertyType == typeof(string))
                    {
                        if (p.GetValue(this) != null)
                        {
                            string val = GetInheritString((string)p.GetValue(this),ctx);
                            pa.SetValue(a, val);
                        }
                    }
                }
                catch(Exception e)
                {
                    string str = e.Message;
                }
            });
            a._ExecutionDelayExpression = GetInheritString(_ExecutionDelayExpression, ctx);
        }
        internal void CopySettingsTo(Action a)
        {
            a.Id = Id;
            a.ActionType = ActionType;
            a.OrderNumber = OrderNumber;
            a._Asynchronous = _Asynchronous;
            a._Enabled = _Enabled;
            a._ExecutionDelayExpression = _ExecutionDelayExpression;
            a._LaunchProcessCmdlineExpression = _LaunchProcessCmdlineExpression;
            a._LaunchProcessPathExpression = _LaunchProcessPathExpression;
            a._LaunchProcessWindowStyle = _LaunchProcessWindowStyle;
            a._LaunchProcessWorkingDirExpression = _LaunchProcessWorkingDirExpression;
            a._PlaySoundExclusive = _PlaySoundExclusive;
            a._PlaySoundMyself = _PlaySoundMyself;
            a._PlaySoundFileExpression = _PlaySoundFileExpression;
            a._PlaySoundVolumeExpression = _PlaySoundVolumeExpression;
            a._RefireInterrupt = _RefireInterrupt;
            a._RefireRequeue = _RefireRequeue;
            a._SystemBeepFreqExpression = _SystemBeepFreqExpression;
            a._SystemBeepLengthExpression = _SystemBeepLengthExpression;
            a._UseTTSExclusive = _UseTTSExclusive;
            a._UseTTSRateExpression = _UseTTSRateExpression;
            a._UseTTSTextExpression = _UseTTSTextExpression;
            a._UseTTSVolumeExpression = _UseTTSVolumeExpression;
            a._PlaySpeechMyself = _PlaySpeechMyself;
            a._ExecScriptAssembliesExpression = _ExecScriptAssembliesExpression;
            a._ExecScriptExpression = _ExecScriptExpression;
            a._MessageBoxIconType = _MessageBoxIconType;
            a._MessageBoxText = _MessageBoxText;
            a._VariableOp = _VariableOp;
            a._VariableName = _VariableName;
            a._DebugLevel = _DebugLevel;
            a._VariableExpression = _VariableExpression;
            a._TriggerId = _TriggerId;
            a._TriggerOp = _TriggerOp;
            a._TriggerText = _TriggerText;
            a._TriggerZone = _TriggerZone;
            a._TriggerForceType = _TriggerForceType;
            a._AuraOp = _AuraOp;
            a._AuraName = _AuraName;
            a._AuraImage = _AuraImage;
            a._AuraImageMode = _AuraImageMode;
            a._AuraXIniExpression = _AuraXIniExpression;
            a._AuraYIniExpression = _AuraYIniExpression;
            a._AuraWIniExpression = _AuraWIniExpression;
            a._AuraHIniExpression = _AuraHIniExpression;
            a._AuraOIniExpression = _AuraOIniExpression;
            a._AuraXTickExpression = _AuraXTickExpression;
            a._AuraYTickExpression = _AuraYTickExpression;
            a._AuraWTickExpression = _AuraWTickExpression;
            a._AuraHTickExpression = _AuraHTickExpression;
            a._AuraOTickExpression = _AuraOTickExpression;
            a._AuraTTLTickExpression = _AuraTTLTickExpression;
            a._FolderOp = _FolderOp;
            a._FolderId = _FolderId;
            a._EncounterOp = _EncounterOp;
            a._EncounterSwingType = _EncounterSwingType;
            a._EncounterAbilityType = _EncounterAbilityType;
            a._EncounterActorName = _EncounterActorName;
            a._EncounterDamage = _EncounterDamage;
            a._EncounterDamageType = _EncounterDamageType;
            a._EncounterTargetName = _EncounterTargetName;
            a._DiscordWebhookMessage = _DiscordWebhookMessage;
            a._DiscordWebhookURL = _DiscordWebhookURL;
            a._TextAuraOp = _TextAuraOp;
            a._TextAuraName = _TextAuraName;
            a._TextAuraExpression = _TextAuraExpression;
            a._TextAuraAlignment = _TextAuraAlignment;
            a._TextAuraXIniExpression = _TextAuraXIniExpression;
            a._TextAuraYIniExpression = _TextAuraYIniExpression;
            a._TextAuraWIniExpression = _TextAuraWIniExpression;
            a._TextAuraHIniExpression = _TextAuraHIniExpression;
            a._TextAuraOIniExpression = _TextAuraOIniExpression;
            a._TextAuraXTickExpression = _TextAuraXTickExpression;
            a._TextAuraYTickExpression = _TextAuraYTickExpression;
            a._TextAuraWTickExpression = _TextAuraWTickExpression;
            a._TextAuraHTickExpression = _TextAuraHTickExpression;
            a._TextAuraOTickExpression = _TextAuraOTickExpression;
            a._TextAuraTTLTickExpression = _TextAuraTTLTickExpression;
            a._TextAuraFontName = _TextAuraFontName;
            a._TextAuraFontSize = _TextAuraFontSize;
            a._TextAuraEffect = _TextAuraEffect;
            a._TextAuraOutlineClInt = _TextAuraOutlineClInt;
            a._TextAuraForegroundClInt = _TextAuraForegroundClInt;
            a._TextAuraBackgroundClInt = _TextAuraBackgroundClInt;
            a._TextAuraUseOutline = _TextAuraUseOutline;
            a._LogMessageText = _LogMessageText;
            a._LogLevel = _LogLevel;
            a._DiscordTts = _DiscordTts;
            a._ListVariableExpression = _ListVariableExpression;
            a._ListVariableExpressionType = _ListVariableExpressionType;
            a._ListVariableIndex = _ListVariableIndex;
            a._ListVariableName = _ListVariableName;
            a._ListVariableOp = _ListVariableOp;
            a._ListVariableTarget = _ListVariableTarget;
            a._OBSControlType = _OBSControlType;
            a._OBSSceneName = _OBSSceneName;
            a._OBSSourceName = _OBSSourceName;
            a._OBSJSONPayload = _OBSJSONPayload;
            a._LogProcess = _LogProcess;
            a._LogReparse = _LogReparse;
            a._JsonOperationType = _JsonOperationType;
            a._JsonCacheRequest = _JsonCacheRequest;
            a._JsonEndpointExpression = _JsonEndpointExpression;
            a._JsonHeaderExpression = _JsonHeaderExpression;
            a._JsonFiringExpression = _JsonFiringExpression;
            a._JsonPayloadExpression = _JsonPayloadExpression;
            a.Condition = (ConditionGroup)(Condition != null ? ((ConditionGroup)Condition).Duplicate() : null);
            a._KeyPressExpression = _KeyPressExpression;
            a._KeypressType = _KeypressType;
            a._KeyPressCode = _KeyPressCode;
            a._KeyPressWindow = _KeyPressWindow;
            a._WmsgCode = _WmsgCode;
            a._WmsgTitle = _WmsgTitle;
            a._WmsgLparam = _WmsgLparam;
            a._WmsgWparam = _WmsgWparam;
            a._DiskFileOp = _DiskFileOp;
            a._DiskFileOpVar = _DiskFileOpVar;
            a._DiskFileOpName = _DiskFileOpName;
            a._TableVariableExpression = _TableVariableExpression;
            a._TableVariableExpressionType = _TableVariableExpressionType;
            a._TableVariableName = _TableVariableName;
            a._TableVariableOp = _TableVariableOp;
            a._TableVariableTarget = _TableVariableTarget;
            a._TableVariableX = _TableVariableX;
            a._TableVariableY = _TableVariableY;
            a._MutexOpType = _MutexOpType;
            a._MutexName = _MutexName;
            a._Description = _Description;
            a._DescriptionOverride = _DescriptionOverride;
            a._NamedCallbackParam = _NamedCallbackParam;
            a._NamedCallbackName = _NamedCallbackName;
            a._NamedCallbackReturnScalarName = _NamedCallbackReturnScalarName;
            a._MouseOpType = _MouseOpType;
            a._MouseCoordType = _MouseCoordType;
            a._MouseX = _MouseX;
            a._MouseY = _MouseY;
            a._ListSourcePersist = _ListSourcePersist;
            a._ListTargetPersist = _ListTargetPersist;
            a._TableSourcePersist = _TableSourcePersist;
            a._TableTargetPersist = _TableTargetPersist;
            a._DiskPersist = _DiskPersist;
            a._VariablePersist = _VariablePersist;
            a._PartyOrderPartyOrder = _PartyOrderPartyOrder;
            a._PartyOrderPlayerName = _PartyOrderPlayerName;
            a._DevActionKey = _DevActionKey;
            a._DevActionValue = _DevActionValue;

            a._SchedulingTriggerName = _SchedulingTriggerName;
            a._SchedulingActionIndex = _SchedulingActionIndex;
            a._SchedulingActionOp = _SchedulingActionOp;
            a._DontExecute = _DontExecute;
        }

        private string SendJson(Context ctx, Action.HTTPMethodEnum method, string url, string json, IEnumerable<string> headers, bool expectNoContent)
        {
            try
            {                
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                if (headers != null && headers.Count() > 0)
                {
                    foreach (string hdr in headers)
                    {
                        httpWebRequest.Headers.Add(hdr);
                    }
                }
                switch (method)
                {
                    case HTTPMethodEnum.POST:
                        httpWebRequest.ContentType = "application/json";
                        httpWebRequest.Method = "POST";
                        using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                        {
                            streamWriter.Write(json);
                            streamWriter.Flush();
                            streamWriter.Close();
                        }
                        break;
                    case HTTPMethodEnum.GET:
                        httpWebRequest.Method = "GET";
                        break;
                }
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpResponse.StatusCode != HttpStatusCode.NoContent && expectNoContent == true)
                {
                    AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/jsonpostunexpectedresponse", "Unexpected response code: {0}", httpResponse.StatusCode));
                }
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/jsonpostexception", "Couldn't send message due to exception: {0}", ex.Message));
                return "";
            }
        }

    }

}
