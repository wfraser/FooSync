///
/// Codewise/ArgumentParser/ProgramArguments.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;

namespace Codewise.ArgumentParser
{
    /// <summary>
    /// Command-Line Argument Parser.
    /// 
    /// Parses a command line into options, flags, and ordinal values, based on switches it is
    /// configured to recognize.
    /// Switches can start with "/" (Win32 style), "-" for single letter switches (Unix style), 
    /// or "--" for multi-letter switches (GNU style).
    /// </summary>
    class ProgramArguments
    {
        /// <summary>
        /// These arrays configure what switches the parser will recognize.
        /// Prefix any letter with an ampersand to denote that letter as a short form.
        /// </summary>
        private static readonly string[] OPTIONS = { "&directory" };
        private static readonly string[] FLAGS = { "&help", "hash", "casesensitive" };

        /// <summary>
        /// Collects arguments like "--foo=bar", "--foo:bar", "--foo bar", or "-f bar"
        /// </summary>
        public Dictionary<string, string> Options { get; private set; }

        /// <summary>
        /// Collects arguments like "--banana", "--nobanana", or "-b"
        /// </summary>
        public Dictionary<string, bool> Flags { get; private set; }

        /// <summary>
        /// Collects any remaining arguments that are referenced by position
        /// instead of switches.
        /// </summary>
        public List<string> Ordinals { get; private set; }

        private static HashSet<string>            _optionSwitches      = null;
        private static Dictionary<string, string> _shortOptionSwitches = null;
        private static HashSet<string>            _flagSwitches        = null;
        private static Dictionary<string, string> _shortFlagSwitches   = null;

        /// <summary>
        /// Configure the parser.
        /// </summary>
        static ProgramArguments()
        {
            _optionSwitches      = new HashSet<string>();
            _shortOptionSwitches = new Dictionary<string, string>();
            _flagSwitches        = new HashSet<string>();
            _shortFlagSwitches   = new Dictionary<string, string>();

            for (int i = 0; i < OPTIONS.Length; i++)
            {
                int index = OPTIONS[i].IndexOf('&');
                if (index != -1)
                {
                    _optionSwitches.Add(OPTIONS[i].Replace("&", string.Empty));
                    _shortOptionSwitches.Add(OPTIONS[i][index + 1].ToString(), OPTIONS[i].Replace("&", string.Empty));
                }
                else
                {
                    _optionSwitches.Add(OPTIONS[i]);
                }
            }

            for (int i = 0; i < FLAGS.Length; i++)
            {
                int index = FLAGS[i].IndexOf('&');
                if (index != -1)
                {
                    _flagSwitches.Add(FLAGS[i].Replace("&", string.Empty));
                    _shortFlagSwitches.Add(FLAGS[i][index + 1].ToString(), FLAGS[i].Replace("&", string.Empty));
                }
                else
                {
                    _flagSwitches.Add(FLAGS[i]);
                }
            }
        }

        /// <summary>
        /// Create an empty ProgramArguments instance.
        /// </summary>
        public ProgramArguments()
        {
            Options = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
            Ordinals = new List<string>();
        }

        /// <summary>
        /// Create an initialized ProgramArguments instance by parsing the given command line.
        /// </summary>
        /// <param name="args">Array of command-line arguments to parse.</param>
        public ProgramArguments(string[] args)
        {
            Options = new Dictionary<string, string>();
            Flags = new Dictionary<string, bool>();
            Ordinals = new List<string>();

            Parse(args);
        }

        /// <summary>
        /// Parse the given command line arguments into the Options, Flags, and Ordinals
        /// properties.
        /// </summary>
        /// <param name="args">List of command-line arguments to parse.</param>
        public void Parse(IList<string> args)
        {            
            bool parsingFlags = true;
            for (int i = 0; i < args.Count; i++)
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
                        Ordinals.Add(args[i]);
                        continue;
                    }

                    if (a.StartsWith("no") && _flagSwitches.Contains(a.Substring(2)))
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
                        if (args.Count > i + 1)
                        {
                            val = args[i + 1];
                        }
                        else
                        {
                            val = null;
                        }
                        valueFromArgs = true;
                    }

                    if (_flagSwitches.Contains(a))
                    {
                        Flags.Add(a, flagSet);
                    }
                    else if (_shortFlagSwitches.ContainsKey(a))
                    {
                        Flags.Add(_shortFlagSwitches[a], flagSet);
                    }
                    else if (_optionSwitches.Contains(a))
                    {
                        Options.Add(a, val);
                        if (valueFromArgs)
                        {
                            i++;
                        }
                    }
                    else if (_shortOptionSwitches.ContainsKey(a))
                    {
                        Options.Add(_shortOptionSwitches[a], val);
                        if (valueFromArgs)
                        {
                            i++;
                        }
                    }
                    else
                    {
                        parsingFlags = false;
                        Ordinals.Add(args[i]);
                    }
                }
                else
                {
                    Ordinals.Add(args[i]);
                }
            }
        } // public void Parse(IList<string> args)

    }
}
