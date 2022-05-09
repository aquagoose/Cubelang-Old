using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cubelang;

public abstract partial class CubelangBase
{
    protected readonly Dictionary<string, object> Variables;
    protected readonly Dictionary<string, int> Labels;
    private int _forLoopCount;
    private string _forLoopVarName;
    private int _forLoopLine;
    private bool _inIfStatement;
    private bool _condition;

    private int _totalLoops;
    protected int MaxLoops;

    private Random _random;

    private MethodInfo[] _methodInfos;

    protected CubelangBase()
    {
        Variables = new Dictionary<string, object>();
        Labels = new Dictionary<string, int>();
        _random = new Random();
        MaxLoops = -1;

        _methodInfos = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
    }

    public virtual void Execute(string code)
    {
        // Split the code by lines.
        string[] splitLines = code.Split('\n');
        for (int l = 0; l < splitLines.Length; l++)
        {
            // Ignore blank lines and comments
            if (splitLines[l].Trim() == "" || splitLines[l].Trim().StartsWith("#"))
                continue;
            try
            {
                // Split the line by spaces - this is not ideal and may be changed later, but it's a quick and dirty
                // way to check for stuff that isn't a function
                string[] splitLine = splitLines[l].Trim().Split(" ");
                // Variable definition. Simply get the variable name and the content afterwards and add it to a dictionary.
                if (splitLine.Length > 1 && splitLine[1] == "is")
                {
                    string[] splitVar = splitLine[0].Split('[');
                    if (splitVar.Length == 1)
                    {
                        if (!Variables.TryAdd(splitLine[0], ParseString(string.Join(' ', splitLine[2..]))))
                            Variables[splitLine[0]] = ParseString(string.Join(' ', splitLine[2..]));
                    }
                    else
                    {
                        List<string> indexers = new List<string>();
                        string indexer = "";
                        int indexerLevel = 0;
                        for (int chr = 0; chr < splitLine[0].Length; chr++)
                        {
                            char c = splitLine[0][chr];

                            switch (c)
                            {
                                case '[':
                                    if (indexerLevel > 0)
                                        indexer += c;
                                    indexerLevel++;
                                    break;
                                case ']':
                                    indexerLevel--;
                                    if (indexerLevel > 0)
                                    {
                                        indexer += c;
                                        continue;
                                    }

                                    indexers.Add(indexer);
                                    indexer = "";
                                    break;
                                default:
                                    if (indexerLevel != 0)
                                        indexer += c;
                                    break;
                            }
                        }
                        
                        object parsedString = ParseString(string.Join(' ', splitLine[2..]));
                        Dictionary<string, object> dict = (Dictionary<string, object>) Variables[splitVar[0]];
                        for (int i = 0; i < indexers.Count - 1; i++)
                        {
                            dict = (Dictionary<string, object>) dict[(string) ParseString(indexers[i])];
                        }
                        
                        if (!dict.TryAdd((string) ParseString(indexers[^1]), parsedString))
                            dict[(string) ParseString(indexers[^1])] = parsedString;
                    }
                }
                // String concatenation & math operator.
                else if (splitLine.Length > 1 && splitLine[1] == "+=")
                {
                    if (Variables.TryGetValue(splitLine[0], out object value))
                    {
                        if (value is string strVal)
                        {
                            strVal += (string) ParseString(string.Join(' ', splitLine[2..]));
                            value = strVal;
                        }
                        else if (value is double doubVal)
                        {
                            doubVal += (double) ParseString(string.Join(' ', splitLine[2..]));
                            value = doubVal;
                        }
                        else if (value is int intVal)
                        {
                            intVal += (int) ParseString(string.Join(' ', splitLine[2..]));
                            value = intVal;
                        }
                        else if (value is ulong ulongVal)
                        {
                            ulongVal += (ulong) ParseString(string.Join(' ', splitLine[2..]));
                            value = ulongVal;
                        }

                        Variables[splitLine[0]] = value;
                    }
                    else
                        throw new Exception($"Variable not found: '{splitLine[0]}'");
                }
                // Repeat loop.
                else if (splitLine.Length > 1 && splitLine[0] == "repeat")
                {
                    // For loop - set a few values, the number of iterations, the name of the variable, and the line to
                    // loop back to.
                    if (splitLine[1] == "for")
                    {
                        _forLoopCount = (int) ParseString(splitLine[2]);
                        _forLoopVarName = splitLine[5];
                        Variables.Add(_forLoopVarName, 0);
                        _forLoopLine = l;
                    }
                }
                // Once this line is reached, it checks to see if the for loop is still valid. If so, it decrements the
                // remaining number of iterations, increments the variable name by 1, and goes back to the loop start.
                else if (splitLine[0] == "endrep")
                {
                    IncrementLoops();
                    _forLoopCount--;
                    if (_forLoopCount > 0)
                    {
                        Variables[_forLoopVarName] = (int) Variables[_forLoopVarName] + 1;
                        l = _forLoopLine;
                    }
                    // Otherwise, remove the temporary for loop variable, and reset the values, as we're done.
                    else
                    {
                        Variables.Remove(_forLoopVarName);
                        _forLoopCount = 0;
                        _forLoopLine = 0;
                        _forLoopVarName = "";
                    }
                }
                else if (splitLine[0] == "if" || splitLine[0].StartsWith("else"))
                {
                    if (splitLine[0].StartsWith("else"))
                    {
                        if (_condition)
                        {
                            for (int line = l + 1; line < splitLines.Length; line++)
                            {
                                if (splitLines[line].Trim() == "endif")
                                {
                                    l = line;
                                    _inIfStatement = false;
                                    break;
                                }
                            }
                            
                            continue;
                        }
                        if (splitLine.Length == 1 && splitLine[0] == "else")
                            continue;
                        else
                            splitLine = splitLine[1..];
                    }
                    
                    _inIfStatement = true;
                    string ifStatement = string.Join(' ', splitLine[1..]);
                    bool inSpeechMarks = false;
                    string cacheStr = "";
                    List<string> strParams = new List<string>();
                    List<object> parameters = new List<object>();
                    int bracketLevels = 0;
                    for (int chr = 0; chr < ifStatement.Length; chr++)
                    {
                        char c = ifStatement[chr];

                        switch (c)
                        {
                            case '\'':
                            case '"':
                                cacheStr += c;
                                
                                if (inSpeechMarks && ifStatement[chr - 1] == '\\')
                                    continue;

                                inSpeechMarks = !inSpeechMarks;
                                break;
                            case '(':
                                cacheStr += c;
                                bracketLevels++;
                                break;
                            case ')':
                                cacheStr += c;
                                bracketLevels--;
                                break;
                            case ' ':
                                if (inSpeechMarks || bracketLevels > 0)
                                {
                                    cacheStr += c;
                                    continue;
                                }

                                strParams.Add(cacheStr.Trim());
                                cacheStr = "";
                                break;
                            default:
                                cacheStr += c;
                                break;
                        }
                    }
                    
                    if (cacheStr.Trim().Length > 0)
                        strParams.Add(cacheStr.Trim());

                    ConditionType conditionType = ConditionType.Boolean;
                    
                    for (int i = 0; i < strParams.Count; i++)
                    {
                        switch (strParams[i])
                        {
                            case "is":
                                if (strParams[i + 1] == "not")
                                {
                                    conditionType = ConditionType.NotEqualTo;
                                    
                                    switch (strParams[i + 1])
                                    {
                                        case "gthan":
                                            conditionType = ConditionType.LessThan;
                                            break;
                                        case "lthan":
                                            conditionType = ConditionType.GreaterThan;
                                            break;
                                        case "gequal":
                                            conditionType = ConditionType.LessEqual;
                                            break;
                                        case "lequal":
                                            conditionType = ConditionType.GreaterEqual;
                                            break;
                                    }
                                    
                                    break;
                                }

                                conditionType = ConditionType.EqualTo;

                                switch (strParams[i + 1])
                                {
                                    case "gthan":
                                        conditionType = ConditionType.GreaterThan;
                                        break;
                                    case "lthan":
                                        conditionType = ConditionType.LessThan;
                                        break;
                                    case "gequal":
                                        conditionType = ConditionType.GreaterEqual;
                                        break;
                                    case "lequal":
                                        conditionType = ConditionType.LessEqual;
                                        break;
                                }
                                break;
                            case "not":
                                if (i == 0)
                                {
                                    conditionType = ConditionType.Negate;
                                    break;
                                }
                                break;
                            case "gthan":
                            case "lthan":
                            case "gequal":
                            case "lequal":
                                break;
                            default:
                                parameters.Add(ParseString(strParams[i]));
                                break;
                        }
                    }

                    _condition = false;
                    
                    switch (conditionType)
                    {
                        case ConditionType.Boolean:
                            if (parameters[0] is bool && (bool) parameters[0])
                                _condition = true;
                            break;
                        case ConditionType.Negate:
                            if (parameters[0] is bool && !(bool) parameters[0])
                                _condition = true;
                            break;
                        case ConditionType.EqualTo:
                            if (parameters[0].GetType() == parameters[1].GetType() && str(parameters[0]) == str(parameters[1]))
                                _condition = true;
                            break;
                        case ConditionType.NotEqualTo:
                            if (parameters[0].GetType() == parameters[1].GetType() && str(parameters[0]) != str(parameters[1]))
                                _condition = true;
                            break;
                        case ConditionType.GreaterThan:
                            if (parameters[0].GetType() == parameters[1].GetType() && (int) parameters[0] > (int) parameters[1])
                                _condition = true;
                            break;
                        case ConditionType.LessThan:
                            if (parameters[0].GetType() == parameters[1].GetType() && (int) parameters[0] < (int) parameters[1])
                                _condition = true;
                            break;
                        case ConditionType.GreaterEqual:
                            if (parameters[0].GetType() == parameters[1].GetType() && (int) parameters[0] >= (int) parameters[1])
                                _condition = true;
                            break;
                        case ConditionType.LessEqual:
                            if (parameters[0].GetType() == parameters[1].GetType() && (int) parameters[0] <= (int) parameters[1])
                                _condition = true;
                            break;
                    }

                    if (!_condition)
                    {
                        for (int line = l + 1; line < splitLines.Length; line++)
                        {
                            if (splitLines[line].Trim() == "endif")
                            {
                                l = line;
                                _inIfStatement = false;
                                break;
                            }

                            if (splitLines[line].Trim().StartsWith("else"))
                            {
                                l = line - 1;
                                break;
                            }
                        }
                    }
                }
                else if (splitLine[0] == "endif")
                {
                    if (_inIfStatement)
                    {
                        _inIfStatement = false;
                        continue;
                    }

                    throw new Exception("Syntax error: 'endif'");
                }
                else if (splitLine[0] == "continue")
                {
                    for (int line = l; line < splitLines.Length; line++)
                    {
                        if (splitLines[line].Trim() == "endrep")
                        {
                            l = line - 1;
                            break;
                        }
                    }
                }
                else if (splitLine[0] == "label")
                {
                    if (!Labels.TryAdd(splitLine[1], l))
                        Labels[splitLine[1]] = l;
                }
                else if (splitLine[0] == "goto")
                {
                    l = int.TryParse(splitLine[1], out int val) ? val - 1 : Labels[splitLine[1]];
                    IncrementLoops();
                }
                else if (splitLine[0] == "loop")
                {
                    int value = (int) Variables[splitLine[2]] - 1;
                    Variables[splitLine[2]] = value;
                    if (value >= 0)
                    {
                        l = int.TryParse(splitLine[1], out int val) ? val - 1 : Labels[splitLine[1]];
                        IncrementLoops();
                    }
                }
                else if (splitLine[0] == "stop")
                    break;
                // Otherwise, we check to see if the line is a function.
                else if (!ParseFunction(splitLines[l]).success)
                    throw new Exception($"Syntax error: {splitLines[l]}");
            }
            // Send exception to command line.
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"Error at line {l + 1}: {e.Message}");
            }
        }
    }

    // Checks to see if the given string is a function. If it is, execute it, and return any value it returns, and return
    // a success code.
    private (object result, bool success) ParseFunction(string line)
    {
        List<string> stringParams = new List<string>();
        List<object> parameters = new List<object>();
        bool isFunction = false;
        string functionName = "";
        string cacheStr = "";
        int bracketLevel = 0;
        bool isInString = false;
        stringParams.Clear();
        parameters.Clear();

        // Loop through each character in the line.
        for (int chr = 0; chr < line.Length; chr++)
        {
            char c = line[chr];
                
            switch (c)
            {
                case '(':
                    if (isInString)
                    {
                        cacheStr += '(';
                        continue;
                    }

                    // Since there is an opening bracket we now know this is a function!
                    isFunction = true;
                    functionName = functionName.Trim();
                    bracketLevel++;
                    // Using bracket level allows us to ignore all functions within a function.
                    if (bracketLevel > 1)
                        cacheStr += '(';
                    break;
                case ')':
                    // Decrement the bracket levels and append a closing bracket if we are still in a string.
                    if (!isInString)
                        bracketLevel--;
                    if (bracketLevel > 0 || isInString)
                        cacheStr += ')';
                    break;
                // Accepts both apostrophes and speech marks
                case '\'':
                case '"':
                    // Change our value of "isInString" UNLESS there is a backslash before the speech mark.
                    cacheStr += c;
                    if (isInString && line[chr - 1] == '\\')
                        continue;
                    isInString = !isInString;
                    break;
                // If we are not in a string, and our bracket level is 1, we add a new parameter. This means that commas
                // in strings get ignored, and also commas in any functions within functions.
                case ',':
                    if (isInString || bracketLevel > 1)
                    {
                        cacheStr += ',';
                        continue;
                    }

                    stringParams.Add(cacheStr.Trim());
                    cacheStr = "";
                    break;
                default:
                    if (!isFunction)
                        functionName += c;
                    else
                        cacheStr += c;
                    break;
            }
        }
        
        if (isFunction)
        {
            // Adds any remaining parameters, if any
            if (cacheStr.Trim().Length > 0)
                stringParams.Add(cacheStr.Trim());

            // Parse each parameter into the correct objects.
            foreach (string param in stringParams)
                parameters.Add(ParseString(param));

            bool found = false;
            
            foreach (MethodInfo info in _methodInfos)
            {
                if (info.Name != functionName)
                    continue;

                found = true;
                
                // For now something can either have an object array or parameters, no inbetweens.
                ParameterInfo[] pInfo = info.GetParameters();
                if (pInfo.Length > 0 && pInfo[0].ParameterType == typeof(object[]))
                {
                    object result = info.Invoke(this, new []{ parameters.ToArray() });
                    return (result, true);
                }

                if (pInfo.Length == parameters.Count)
                {
                    for (int i = 0; i < pInfo.Length; i++)
                    {
                        if (pInfo[i].ParameterType != parameters[i].GetType())
                        {
                            if (pInfo[i].ParameterType != typeof(object))
                                goto NEXTITEM;
                        }
                    }
                    object result = info.Invoke(this, parameters.ToArray());
                    return (result, true);
                }
                
                NEXTITEM: ;
            }
            
            OOPS: ;

            // No method with the right parameters were found!
            string msg;
            if (found)
                msg =
                    $"Incorrect parameters supplied for function \"{functionName}\". The following function(s) with the given name are available:\n";
            else
                msg = $"Could not find a function with name \"{functionName}\".";

            bool similarFound = false;
            foreach (MethodInfo info in _methodInfos)
            {
                if (found && info.Name != functionName)
                    continue;
                else if (!functionName.ToLower().Contains(info.Name.ToLower()))
                    continue;

                if (!found && !similarFound)
                {
                    similarFound = true;
                    msg += " However, function(s) with similar name(s) were found:\n";
                }
                
                ParameterInfo[] parameterInfos = info.GetParameters();
                msg += $"\t{info.Name}(";
                for (int i = 0; i < parameterInfos.Length; i++)
                {
                    msg += $"{parameterInfos[i].Name}";
                    msg += $": {GetTypeOf(parameterInfos[i].ParameterType)}";
                    
                    if (i < parameterInfos.Length - 1)
                        msg += ", ";
                }

                msg += $") -> {GetTypeOf(info.ReturnType)}\n";
            }

            throw new Exception(msg);
        }

        return (null, false);
    }

    // ParseString, parses any string given into the correct value. Converts numbers to numbers, and "expands" out
    // variables and functions into a full string that can be read by any functions that need it.
    private object ParseString(string param)
    {
        if (param == "none")
            return null;
        // Parse numbers if possible.
        if (int.TryParse(param, out int intParam))
            return intParam;
        if (ulong.TryParse(param, out ulong ulongParam))
            return ulongParam;
        if (double.TryParse(param, out double doubleParam))
            return doubleParam;

        if (bool.TryParse(param, out bool boolParam))
            return boolParam;

        // Parse variables if possible.
        List<string> indexers = new List<string>();
        string varName = "";
        string indexer = "";
        bool isInIndexer = false;
        for (int chr = 0; chr < param.Length; chr++)
        {
            char c = param[chr];

            switch (c)
            {
                case '[':
                    isInIndexer = true;
                    break;
                case ']':
                    isInIndexer = false;
                    indexers.Add(indexer);
                    indexer = "";
                    break;
                default:
                    if (isInIndexer)
                        indexer += c;
                    else
                        varName += c;
                    break;
            }
        }

        if (Variables.TryGetValue(varName, out object val))
        {
            if (indexers.Count == 0)
                return val;
            else
            {
                if (val.GetType() == typeof(List<object>))
                {
                    if (indexers.Count > 1)
                        throw new Exception("Invalid indexer");
                    int value = (int) ParseString(indexers[0]);
                    return ((List<object>) val)[value == -1 ? ^1 : value];
                }
                else if (val.GetType() == typeof(Dictionary<string, object>))
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>) Variables[varName];
                    for (int i = 0; i < indexers.Count - 1; i++)
                    {
                        dict = (Dictionary<string, object>) dict[(string) ParseString(indexers[i])];
                    }
                    return dict[(string) ParseString(indexers[^1])];
                }
            }
        }
        
        // Parse functions if possible.
        (object result, bool success) = ParseFunction(param);
        if (success)
            return result;

        bool isInString = false;
        string varCache = "";
        string finalStr = "";
        int bracketLevel = 0;

        // This works somewhat similarly to ParseFunction
        for (int chr = 0; chr < param.Length; chr++)
        {
            char c = param[chr];

            switch (c)
            {
                case '\'':
                case '"':
                    if (isInString && param[chr - 1] == '\\' || bracketLevel > 0)
                    {
                        if (bracketLevel > 0)
                            varCache += c;
                        else
                            finalStr += c;
                    }
                    else
                        isInString = !isInString;
                    break;
                // Checks to see if the character in front is a quote, if so, remove the backslash.
                case '\\':
                    if (isInString)
                    {
                        if (param[chr + 1] is '"' or '\'')
                            continue;
                        if (param[chr + 1] is 'n')
                            finalStr += '\n';

                        chr++;
                        continue;
                    }
                    finalStr += c;
                    break;
                // String interpolation! Similar to the function parsing, we simply increment the bracket count allowing
                // brackets inside of brackets to allow functions within functions etc.
                // Doesn't quite work properly, bug fix required.
                case '{':
                    bracketLevel++;
                    if (bracketLevel > 1)
                        varCache += '{';
                    break;
                case '}':
                    bracketLevel--;
                    if (bracketLevel == 0)
                    {
                        // If the bracket level is 0, recursively run the ParseString method to parse whatever was
                        // inside it.
                        finalStr += str(ParseString(varCache));
                        varCache = "";
                    }
                    else
                    {
                        varCache += '}';
                    }
                    break;
                default:
                    if (bracketLevel > 0)
                        varCache += c;
                    else
                        finalStr += c;
                    break;
            }
        }

        return finalStr;
    }

    private void IncrementLoops()
    {
        _totalLoops++;
        if (MaxLoops != -1 && _totalLoops > MaxLoops)
            throw new Exception($"Maximum number of loops reached ({MaxLoops})");
    }

    public string GetTypeOf(Type type)
    {
        if (type == typeof(bool))
            return "bool";
        else if (type == typeof(string))
            return "str";
        else if (type == typeof(int))
            return "i32";
        else if (type == typeof(ulong))
            return "u64";
        else if (type == typeof(double))
            return "f64";
        else if (type == typeof(List<object>))
            return "List";
        else if (type == typeof(Dictionary<string, object>))
            return "Dict";
        else if (type == typeof(object[]))
            return "any[...]";
        else if (type == typeof(object))
            return "any";
        else if (type == typeof(void))
            return "none";
        else
            return type.Name;
    }
}