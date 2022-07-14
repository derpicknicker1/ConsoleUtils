﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum CmdParameterTypes
{
    STRING,
    INT,
    BOOL,
    DECIMAL
}

public enum CmdCommandTypes
{
    VERB,
    PARAMETER,
    FLAG,
    MULTIPE_PARAMETER,
    UNNAMED
}

public class CmdParameter
{

    public CmdParameterTypes Type { get; set; }
    public object Value { get; set; }
    public long IntValue { get; set; }
    public bool BoolValue { get; set; }
    public decimal DecimalValue { get; set; }
    public string String { get { return Value != null ? Value.ToString() : null; } }

    public CmdParameter(CmdParameterTypes Type, object Value)
    {
        this.Type = Type;
        this.Value = Value;
        try { IntValue = Convert.ToInt64(Value); } catch { }
        try { this.BoolValue = Convert.ToBoolean(Value); } catch { }
        try { this.DecimalValue = Convert.ToDecimal(Value); } catch { }
    }
}

public class CmdParameters : List<CmdParameter>
{
    public new CmdParameters Add(CmdParameter item)
    {
        base.Add(item);
        return this;
    }
    public new CmdParameters Add(CmdParameterTypes type, object value)
    {
        base.Add(new CmdParameter(type, value));
        return this;
    }

}

public class CmdOption
{


    public string Name { get; set; }
    public string ShortName { get; set; }
    public CmdCommandTypes CmdType { get; set; }
    public CmdParameters Parameters { get; set; }
    public CmdParameters Values { get; set; }
    public string Description { get; set; }
    public int Count { get; set; }
    public bool WasUserSet { get; set; }

    public CmdOption(string Name)
    {
        this.Name = Name;
    }
    public CmdOption(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description, string aliasFor = null)
    {
        this.Name = Name;
        this.ShortName = Shortname;
        this.CmdType = CmdType;
        this.Parameters = CmdParams;
        this.Values = new CmdParameters();
        this.Description = Description;

        InitDefaultValues();
    }

    public void InitDefaultValues()
    {
        //this.Values.Add(new CmdParameters());
        this.Values.AddRange(this.Parameters);
    }
    public long Int
    {
        get { return Ints.Single(); }
    }
    public long Long
    {
        get { return Longs.Single(); }
    }
    public string String
    {
        get { return Strings.Single(); }
    }
    public bool Bool
    {
        get { return Bools.Single(); }
    }
    public decimal Decimal
    {
        get { return Decimals.Single(); }
    }
    public int[] Ints
    {
        get { return GetInts(); }
    }
    public long[] Longs
    {
        get { return GetLongs(); }
    }
    public string[] Strings
    {
        get { return GetStrings(); }
    }

    public bool[] Bools
    {
        get { return GetBools(); }
    }

    public decimal[] Decimals
    {
        get { return GetDecimals(); }
    }

    public long[] GetLongs()
    {
        return this.Values.Select(x => x.IntValue).ToArray(); 
    }
    public int[] GetInts()
    {
        return this.Values.Select(x => (int)x.IntValue).ToArray();
    }

    public bool[] GetBools()
    {
        
        try
        {
            return this.Values.Select(x => x.BoolValue).ToArray();
        }
        catch
        {
            return new bool[] { false };
        }
    }

    public string[] GetStrings()
    {
        return this.Values.Where(x => x.String != null).Select(x => x.String).ToArray();
    }

    public decimal[] GetDecimals()
    {
        return this.Values.Select(x => x.DecimalValue).ToArray();
    }

}

public class CmdParser : KeyedCollection<string, CmdOption>
{
    private string _longParamPrefix = "--";
    private string _shortParamPrefix = "-";

    private Queue<string> fifo = new Queue<string>();

    public string DefaultParameter { get; set; }
    public string DefaultVerb { get; set; }

    public bool IsVerb
    {
        get; private set;
    }

    public bool HasFlag(string flag)
    {
        return this[flag].Bool;
    }

    public bool IsParameterNullOrEmpty(string parameter)
    {
        if (!this.Contains(parameter))
            return true;
        if (this[parameter].Strings.Length < 1)
            return true;
        if (string.IsNullOrEmpty(this[parameter].Strings[0])) 
            return true;
        return false;
    }
    public bool Exists(string parameter)
    {
        return !IsParameterNullOrEmpty(parameter);
    }

    public string[] Verbs
    {
        get
        {   
            string[] verbs = this.Where(c => c.CmdType == CmdCommandTypes.VERB && c.WasUserSet).Select(x => x.Name).ToArray();
            return verbs.Length > 0 ? verbs : DefaultVerb != null ? new string[] { DefaultVerb } : new string[0];
        }
    }

    public CmdParser(string[] Args)
    {
        foreach (var arg in Args)
            fifo.Enqueue(arg);

    }

    private bool TryGetValue(string key, out CmdOption item)
    {

        if (this.Contains(key))
        {
            item = this[key];
            return true;
        }
        else
        {
            item = null;
            return false;
        }
    }

    public void Parse()
    {
        while (fifo.Count > 0)
        {
            var inputArgument = fifo.Dequeue();
            var currentArgument = inputArgument;

            string parseKey = null;
            string longName = null;
            string shortName = null;

            if (currentArgument != null && currentArgument.StartsWith(_longParamPrefix))
            {
                longName = currentArgument.Substring(2);
                IsVerb = false;
                parseKey = this.Where(x => x.Name == longName).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                    currentArgument = parseKey;
            }
            else if (currentArgument != null && currentArgument.StartsWith(_shortParamPrefix))
            {
                shortName = currentArgument.Substring(1);
                IsVerb = false;
                parseKey = this.Where(x => x.ShortName == shortName).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                    currentArgument = parseKey;
            }
            else
            {
                parseKey = this.Where(x => x.Name == currentArgument).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                    currentArgument = parseKey;
                IsVerb = true;
            }


            ;

            if (this.TryGetValue(currentArgument, out CmdOption arg))     // known command
            {
                string name = arg.Name;
                int parameterCount = arg.Parameters.Count;
                string expectedParamsString = string.Join(", ", arg.Parameters.Select(x => x.Type.ToString()).ToArray());

                arg.WasUserSet = true;
                

                /*
                if (arg.CmdType != CmdCommandTypes.MULTIPE_PARAMETER)
                    this[currentArgument].Values.Add(r);
                */


                if (arg.CmdType == CmdCommandTypes.FLAG)
                {
                    CmdParameter cmdParam = new CmdParameter(CmdParameterTypes.BOOL, true);
                    this[currentArgument].Values[0] = cmdParam;
                }
                else
                {
                    //CmdParameters c = this[currentArgument].Parameters;
                    for(int i = 0; i < this[currentArgument].Parameters.Count; i++)
                    //foreach (var p in this[currentArgument].Parameters)
                    {
                        CmdParameter r = new CmdParameter(this[currentArgument].Parameters[i].Type, null);
                        string f = fifo.Dequeue();
                        if (r.Type == CmdParameterTypes.BOOL)
                        {
                            string low = f.ToLower().Trim();
                            if (low == "0" || low == "false" || low == "off" || low == "disabled" || low == "disable" || low == "no")
                                r.Value = false;
                            else if (low == "1" || low == "true" || low == "on" || low == "enabled" || low == "enable" || low == "yes")
                                r.Value = true;
                            else
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");
                        }
                        else if (r.Type == CmdParameterTypes.INT)
                        {
                            int v = 0;
                            bool success = false;

                            if (f.StartsWith("0x"))
                                success = int.TryParse(f.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out v);
                            else
                                success = int.TryParse(f, out v);

                            if (!success)
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            r.Value = v;
                            r.IntValue = v;
                            r.DecimalValue = v;
                            r.BoolValue = Convert.ToBoolean(v);

                        }
                        else if (r.Type == CmdParameterTypes.DECIMAL)
                        {
                            decimal v = 0;

                            if (!decimal.TryParse(f, out v))
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            r.Value = v;
                            r.IntValue = (int)v;
                            r.DecimalValue = v;
                            r.BoolValue = Convert.ToBoolean(v);
                        }
                        else if (r.Type == CmdParameterTypes.STRING)
                        {
                            r.BoolValue = f != null;
                            r.Value = f;
                        }

                        //if (i < this[currentArgument].Values.Count)
                        //this[currentArgument].Values[i] = r;
                        if (arg.CmdType == CmdCommandTypes.MULTIPE_PARAMETER)
                        {   if(this[currentArgument].Count == 0)
                                this[currentArgument].Values[i] = r;
                            else
                            { 
                                this[currentArgument].Values.Add(r);
                            }
                                

                        }
                        else if (this[currentArgument].Values.Count == 1)
                        {
                            this[currentArgument].Values[i] = r;
                        }
                        else
                            throw new ArgumentException($"Multiple parameter fuckup @\"{currentArgument}\"!");

                        
                    }

                    this[currentArgument].Count++;
                }
            } 
            else if (inputArgument.StartsWith(_longParamPrefix) || inputArgument.StartsWith(_shortParamPrefix)){
                throw new ArgumentException($"unknown parameter \"{inputArgument}\"");
            }
            else                                                // unnamed
            {
                if(this.DefaultParameter != null)
                {
                    this[this.DefaultParameter].Values.Add(CmdParameterTypes.STRING, currentArgument);
                }                    
            }
 

        }
    }


    protected override string GetKeyForItem(CmdOption item)
    {
        if (item == null)
            throw new ArgumentNullException("option");
        if (item.Name != null && item.Name.Length > 0)
            return item.Name;
        throw new InvalidOperationException("Option has no names!");
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, string Description)
    {
        CmdParameters defParam = new CmdParameters();

        if (CmdType == CmdCommandTypes.FLAG)
            base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { CmdParameterTypes.BOOL, false } }, Description));
        else
            base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters(), Description));
        
        return this;
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, CmdParams, Description));
        return this;
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameterTypes Type, object DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { Type, DefaultValue } }, Description));
        return this;
    }
    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, bool DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { CmdParameterTypes.BOOL, DefaultValue } }, Description));
        return this;
    }
}