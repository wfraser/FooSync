using System;
using System.Collections.Generic;

namespace FooSync.ConsoleApp
{
    /// <summary>
    /// Command-Line Argument Parser.
    /// Set the static members OPTIONS and FLAGS to specify the options and flags supported.
    /// Prefix any letter with an ampersand to denote the following letter as a short form.
    /// </summary>
    class ProgramArguments
    {
        private static readonly string[] OPTIONS = { "&directory" };
        private static readonly string[] FLAGS = { "&help", "hash", "casesensitive" };

        public ProgramArguments()
        {
            Options = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
            Values = new List<string>();
        }

        public ProgramArguments(string[] args)
        {
            Options = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
            Values = new List<string>();

            if (_flags == null)
            {
                _options = new HashSet<string>();
                _shortOptions = new Dictionary<string, string>();
                _flags = new HashSet<string>();
                _shortFlags = new Dictionary<string, string>();

                for (int i = 0; i < OPTIONS.Length; i++)
                {
                    int index = OPTIONS[i].IndexOf('&');
                    if (index != -1)
                    {
                        _options.Add(OPTIONS[i].Replace("&", ""));
                        _shortOptions.Add(OPTIONS[i][index + 1].ToString(), OPTIONS[i].Replace("&", ""));
                    }
                    else
                    {
                        _options.Add(OPTIONS[i]);
                    }
                }

                for (int i = 0; i < FLAGS.Length; i++)
                {
                    int index = FLAGS[i].IndexOf('&');
                    if (index != -1)
                    {
                        _flags.Add(FLAGS[i].Replace("&", ""));
                        _shortFlags.Add(FLAGS[i][index + 1].ToString(), FLAGS[i].Replace("&", ""));
                    }
                    else
                    {
                        _flags.Add(FLAGS[i]);
                    }
                }
            }
            
            bool parsingFlags = true;
            for (int i = 0; i < args.Length; i++)
            {
                if (parsingFlags)
                {
                    bool flagSet = true;
                    var a = args[i];
                    
                    if (a.StartsWith("--"))
                    {
                        a = a.Substring(2);
                    }
                    else if (a.StartsWith("-") || a.StartsWith("/"))
                    {
                        a = a.Substring(1);
                    }
                    else
                    {
                        parsingFlags = false;
                        Values.Add(args[i]);
                        continue;
                    }

                    if (a.StartsWith("no") && _flags.Contains(a.Substring(2)))
                    {
                        flagSet = false;
                        a = a.Substring(2);
                    }

                    string val = null;
                    bool valueFromArgs = false;
                    int splitIndex = a.IndexOf(":");
                    if (splitIndex != -1 || (splitIndex = a.IndexOf("=")) != -1)
                    {
                        val = a.Substring(splitIndex + 1);
                        a = a.Substring(0, splitIndex);
                    }
                    else
                    {
                        if (args.Length > i + 1)
                        {
                            val = args[i + 1];
                        }
                        else
                        {
                            val = null;
                        }
                        valueFromArgs = true;
                    }

                    if (_flags.Contains(a))
                    {
                        Flags.Add(a, flagSet);
                    }
                    else if (_shortFlags.ContainsKey(a))
                    {
                        Flags.Add(_shortFlags[a], flagSet);
                    }
                    else if (_options.Contains(a))
                    {
                        Options.Add(a, val);
                        if (valueFromArgs)
                        {
                            i++;
                        }
                    }
                    else if (_shortOptions.ContainsKey(a))
                    {
                        Options.Add(_shortOptions[a], val);
                        if (valueFromArgs)
                        {
                            i++;
                        }
                    }
                    else
                    {
                        parsingFlags = false;
                        Values.Add(args[i]);
                    }
                }
                else
                {
                    Values.Add(args[i]);
                }
            }
        }

        public Dictionary<string, string> Options { get; private set; }
        public Dictionary<string, bool> Flags { get; private set; }
        public List<string> Values { get; private set; }

        private static HashSet<string> _options = null;
        private static Dictionary<string, string> _shortOptions = null;
        private static HashSet<string> _flags = null;
        private static Dictionary<string, string> _shortFlags = null;
    }
}
