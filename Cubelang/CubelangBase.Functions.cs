using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjectDict = System.Collections.Generic.Dictionary<string, object>;

namespace Cubelang;

public abstract partial class CubelangBase
{
    #region Strings

    public string upper(string text)
    {
        return text.ToUpper();
    }

    public string lower(string text)
    {
        return text.ToLower();
    }

    public string str(object value)
    {
        string finalObj = value.ToString();
        if (value.GetType() == typeof(List<object>))
        {
            finalObj = "List(";
            int i = 0;
            foreach (object item in (List<object>) value)
            {
                if (item is string)
                    finalObj += '"' + item.ToString() + '"';
                else
                    finalObj += str(item);
                if (i++ < ((List<object>) value).Count - 1)
                    finalObj += ", ";
            }

            finalObj += ")";
        }
        else if (value.GetType() == typeof(ObjectDict))
        {
            finalObj = "Dict(";
            int i = 0;
            foreach (KeyValuePair<string, object> item in (ObjectDict) value)
            {
                finalObj += item.Key + ": ";
                if (item.Value is string)
                    finalObj += '"' + item.Value.ToString() + '"';
                else
                    finalObj += str(item.Value);
                if (i++ < ((ObjectDict) value).Count - 1)
                    finalObj += ", ";
            }

            finalObj += ")";
        }
        
        return finalObj;
    }
    
    public int len(string str)
    {
        return str.Length;
    }
    
    public string elementAt(string str, int index)
    {
        return str[index].ToString();
    }
    
    public string range(string str, int startRange, int endRange)
    {
        return str[startRange..endRange];
    }

    public bool contains(string str, string value)
    {
        return str.Contains(value);
    }

    public bool startsWith(string str, string value) => str.StartsWith(value);

    public bool endsWith(string str, string value) => str.EndsWith(value);

    public string trim(string str) => str.Trim();

    #endregion

    #region Lists

    public List<object> List(object[] elements)
    {
        return new List<object>(elements);
    }

    public void addElement(List<object> list, object value)
    {
        list.Add(value);
    }

    public object elementAt(List<object> list, int index)
    {
        return list[index];
    }

    public int len(List<object> list)
    {
        return list.Count;
    }

    public string join(List<object> list, string joinStr)
    {
        return string.Join(joinStr, list);
    }

    public List<object> split(string str, string splitChar)
    {
        string[] strSplit = str.Split(splitChar);
        List<object> split = new List<object>();
        foreach (string item in strSplit)
            split.Add(item);
        return split;
    }

    public List<object> range(List<object> list, int startRange, int endRange)
    {
        List<object> rangeList = new List<object>();
        for (int i = startRange; i < endRange; i++)
            rangeList.Add(list[i]);
        return rangeList;
    }

    public void set(List<object> list, int position, object value)
    {
        list[position] = value;
    }

    public bool contains(List<object> list, object value)
    {
        return list.Contains(value);
    }
    
    #endregion

    #region Dictionaries

    public ObjectDict Dict() => new ObjectDict();

    public void set(ObjectDict dict, string key, object value)
    {
        if (!dict.TryAdd(key, value))
            dict[key] = value;
    }

    public object get(ObjectDict dict, string key) => dict[key];
    
    public int len(ObjectDict dict) => dict.Count;

    #endregion

    public int randI32(int low, int high)
    {
        return _random.Next(low, high);
    }

    public int i32(object value)
    {
        try
        {
            if (value is string s)
                return int.Parse(s);
            return (int) value;
        }
        catch (Exception)
        {
            throw new Exception("Given value cannot be parsed as i32.");
        }
    }
    
    public ulong u64(object value)
    {
        try
        {
            if (value is string s)
                return ulong.Parse(s);
            return (ulong) value;
        }
        catch (Exception)
        {
            throw new Exception("Given value cannot be parsed as i32.");
        }
    }
    
    public double f64(object value)
    {
        try
        {
            if (value is string s)
                return double.Parse(s);
            return (double) value;
        }
        catch (Exception)
        {
            throw new Exception("Given value cannot be parsed as i32.");
        }
    }

    public int add(int a, int b) => a + b;

    public int sub(int a, int b) => a - b;

    public int mul(int a, int b) => a * b;
    
    public double add(double a, double b) => a + b;

    public double sub(double a, double b) => a - b;

    public double mul(double a, double b) => a * b;

    public double div(double a, double b) => a / b;
    
    public string json_stringify(Dictionary<string, object> dict)
    {
        return JsonConvert.SerializeObject(dict, Formatting.Indented);
    }

    public ObjectDict json_parse(string json)
    {
        ObjectDict dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        
        for (int i = 0; i < dict.Count; i++)
        {
            KeyValuePair<string, object> obj = dict.ElementAt(i);
            if (obj.Value is JObject jObj)
            {
                dict[obj.Key] = Deserialize(jObj);
            }
            else if (obj.Value is JArray array)
            {
                dict[obj.Key] = Deserialize(array);
            }
        }

        return dict;
    }

    private ObjectDict Deserialize(JObject jObject)
    {
        ObjectDict dict = jObject.ToObject<ObjectDict>();
        for (int i = 0; i < dict.Count; i++)
        {
            KeyValuePair<string, object> obj = dict.ElementAt(i);
            if (obj.Value is JObject jObj)
            {
                dict[obj.Key] = Deserialize(jObj);
            }
            else if (obj.Value is JArray array)
            {
                dict[obj.Key] = Deserialize(array);
            }
        }

        return dict;
    }

    private List<object> Deserialize(JArray array)
    {
        List<object> list = array.ToObject<List<object>>();
        for (int i = 0; i < list.Count; i++)
        {
            object obj = list[i];
            if (obj is JObject jObj)
            {
                list[i] = Deserialize(jObj);
            }
            else if (obj is JArray arr)
            {
                list[i] = Deserialize(arr);
            }
        }

        return list;
    }
}