﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Triggernometry.Variables
{

    public sealed class VariableDictionary : Variable
    {
        private Dictionary<string, Variable> Values { get; set; } = new Dictionary<string, Variable>();
        
        public override string ToString()
        {
            return String.Join(",", Values);
        }
        public override JToken ToJToken()
        {
            var obj = new JObject();
            foreach (var key in Values.Keys)
            {
                obj[key] = Values[key].ToJToken();
            }
            return JToken.FromObject(obj);
        }
        public bool isEmpty()
        {
            return Values.Count == 0;
        }
        public override int CompareTo(object o)
        {
            if ((o is Variable) == false)
            {
                throw new InvalidOperationException();
            }
            if (o is VariableScalar)
            {
                return 1;
            }
            if (o is VariableList)
            {
                return 1;
            }
            if (o is VariableDictionary)
            {
                VariableDictionary v = (VariableDictionary)o;                
                if (v.Values.Keys.Count > Values.Keys.Count)
                {
                    return -1;
                }
                if (v.Values.Keys.Count < Values.Keys.Count)
                {
                    return 1;
                }
                List<string> a = new List<string>(Values.Keys);
                List<string> b = new List<string>(v.Values.Keys);
                a.Sort();
                b.Sort();
                for (int i = 0; i < a.Count; i++)
                {
                    int res = a[i].CompareTo(b[i]);
                    if (res != 0)
                    {
                        return res;
                    }
                    res = Values[a[i]].CompareTo(v.Values[a[i]]);
                    if (res != 0)
                    {
                        return res;
                    }
                }
                return 0;
            }
            return -1;
        }

        public override Variable Duplicate()
        {
            VariableDictionary v = new VariableDictionary();
            foreach (KeyValuePair<string, Variable> kp in Values)
            {
                v.SetValue(kp.Key, kp.Value.Duplicate());
            }
            v.LastChanger = LastChanger;
            v.LastChanged = LastChanged;
            return v;
        }
        public void CopyFrom(VariableDictionary source)
        {
            PluginBridges.BridgeFFXIV.ClearCombatant(this);
            foreach (KeyValuePair<string, Variable> kp in source.Values)
            {
                Values[kp.Key] = kp.Value.Duplicate();
            }
            LastChanger = source.LastChanger;
            LastChanged = source.LastChanged;
        }
        public Variable GetValue(string id)
        {
            if (Values.ContainsKey(id) == true)
            {
                return Values[id];
            }
            return new VariableScalar();
        }

        public void SetValue(string id, int val)
        {
            InternalSetValue(id, new VariableScalar() { Value = val.ToString() });
        }

        public void SetValue(string id, float val)
        {
            InternalSetValue(id, new VariableScalar() { Value = I18n.ThingToString(val) });
        }

        public void SetValue(string id, double val)
        {
            InternalSetValue(id, new VariableScalar() { Value = I18n.ThingToString(val) });            
        }

        public void SetValue(string id, string val)
        {
            InternalSetValue(id, new VariableScalar() { Value = val });
        }

        public void SetValue(string id, Variable val)
        {
            InternalSetValue(id, val.Duplicate());
        }

        private void InternalSetValue(string id, Variable val)
        {
            Values[id] = val;
            LastChanged = DateTime.Now;
            LastChanger = "VariableDictionary";
        }

    }

}
