using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
namespace Triggernometry.Variables
{

    [XmlRoot(ElementName = "VariableJson")]
    public class VariableJson : Variable,IXmlSerializable
    {
        public JObject Value { get; set; } = new JObject();

        public override string ToString()
        {
            return JsonConvert.SerializeObject(Value);
        }

        public override int CompareTo(object o)
        {
            if ((o is Variable) == false)
            {
                throw new InvalidOperationException();
            }
            return -1;
        }
        public override Variable Duplicate()
        {
            VariableJson v = new VariableJson();
            v.Value = (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(Value));
            v.LastChanger = LastChanger;
            v.LastChanged = LastChanged;
            return v;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader x)
        {
            if (x.IsEmptyElement == true)
            {
                return;
            }
            x.Read();
            XmlSerializer xsv = new XmlSerializer(typeof(string));
            if (x.NodeType != XmlNodeType.EndElement)
            {
                string value;
                value = (string)xsv.Deserialize(x);
                this.Value = (JObject)JsonConvert.DeserializeObject(value);
                x.MoveToContent();
            }
            x.Read();
        }

        public void WriteXml(XmlWriter x)
        {
            XmlSerializer xsv = new XmlSerializer(typeof(string));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            xsv.Serialize(x,JsonConvert.SerializeObject(Value),ns);
        }
        public static void Sort( List<JToken> arr,Action.JvarSortMethodEnum method,string exp)
        {
            arr.Sort((a, b) => JTokenCompare(a, b, method, exp));
        }
        public static int JTokenCompare(JToken token_a,JToken token_b, Action.JvarSortMethodEnum method, string exp)
        {
            var a=token_a.SelectToken(exp);
            var b=token_b.SelectToken(exp);
            if ((a == null) && (b == null)) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            if (a.Type != b.Type) return 0;
            switch (method)
            {
                case Action.JvarSortMethodEnum.SortAlphaAsc: {
                        return a.ToString().CompareTo(b.ToString());
                    };break;
                case Action.JvarSortMethodEnum.SortAlphaDesc:
                    {
                        return (-1)*a.ToString().CompareTo(b.ToString());
                    };break;
                case Action.JvarSortMethodEnum.SortNumericAsc: {
                        var a_str = a.ToString();
                        var b_str = b.ToString();
                        var maxlen=a_str.Length>b_str.Length?a_str.Length:b_str.Length;
                        a_str.PadLeft(maxlen, ' ');
                        b_str.PadLeft(maxlen, ' ');
                        return a_str.CompareTo(b_str);
                    };break;
                case Action.JvarSortMethodEnum.SortNumericDesc:                   
                    {
                        var a_str = a.ToString();
                        var b_str = b.ToString();
                        var maxlen = a_str.Length > b_str.Length ? a_str.Length : b_str.Length;
                        a_str.PadLeft(maxlen, ' ');
                        b_str.PadLeft(maxlen, ' ');
                        return (-1)*a_str.CompareTo(b_str);
                    };break;
                default:return 0;break;
            }
            return 0;
        }

        public override JToken ToJToken()
        {
            return JToken.FromObject(Value);
        }
        public static void PushValueInto(JToken token,string value,bool _appendAsDict=false) {
            JObject obj = token as JObject;
            object item = JsonConvert.DeserializeObject(value);
            if (token.Type == JTokenType.Object)
            {
                if (((token as JObject).Properties().Count() == 1) && item.GetType() == typeof(JObject))
                {
                    if (_appendAsDict)
                    {
                        foreach (JProperty property in ((JObject)item).Properties())
                        {
                            obj[property.Name] = property.Value;

                        }
                    }
                    else
                    {
                        JArray arr = new JArray();
                        arr.Add(JToken.FromObject(token));
                        arr.Add(JToken.FromObject(item));
                        token.Replace(arr);
                    }
                }
                else if ((token as JObject).Properties().Count() == 0)
                {
                    token.Replace(JToken.FromObject(item));
                }
                else
                {
                    if (item.GetType() != typeof(JObject))
                    {
                        return;
                    }
                    else
                    {
                        foreach (JProperty property in ((JObject)item).Properties())
                        {
                            obj[property.Name] = property.Value;

                        }
                    }
                }
            }
            else {
                if (item.GetType() == typeof(JObject)) return;
                else if (item.GetType() == typeof(JArray))
                {
                    JArray arr = new JArray();
                    arr.Add(JToken.FromObject(token));
                    arr.Concat((JArray)item);
                    token.Replace(arr);
                }
                else {
                    JArray arr = new JArray();
                    arr.Add(JToken.FromObject(token));
                    arr.Add(JToken.FromObject(item));
                    token.Replace(arr);
                }
            }
        }
        public bool CreateTokens(string path) {
            bool success = false;
            List<JToken> tokens = new List<JToken> { Value };
            foreach (var step in path.Split('.'))
            {
                List<JToken> nexttokens = new List<JToken>();
                foreach (var token in tokens)
                {
                    var selecttokens = token.SelectTokens(step).ToList();
                    if (selecttokens.Count <= 0)
                    {
                        if (step.IndexOf(',') > 0) continue;
                        if (step.IndexOf('?') > 0) continue;
                        if (step.IndexOf('@') > 0) continue;
                        if (step.IndexOf('*') > 0) continue;

                        switch (token.Type)
                        {
                            case JTokenType.Object:
                                {
                                    var obj = token as JObject;
                                    obj[step] = new JObject();
                                    
                                    selecttokens.Add(obj[step]);
                                    success = true;
                                    
                                }; break;
                            default:
                                {
                                } break;
                        }
                    }
                    nexttokens.AddRange(selecttokens);
                }
                tokens = nexttokens;
                if (tokens.Count <= 0) break;
            }
            return success;
        
        }
    }

}
