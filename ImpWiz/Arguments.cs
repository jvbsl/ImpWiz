using System.Collections.Generic;

namespace ImpWiz
{
    public class Arguments
    {
        public bool ShowHelp { get; private set; }
        
        public List<string> InputFiles { get; }
        public List<string> OutputFiles { get; }

        public Arguments()
        {
            ShowHelp = false;
            InputFiles = new List<string>();
            OutputFiles = new List<string>();
        }

        private static string ParsePath(string dir)
        {
            if (dir.Length >= 2)
            {
                if (dir[0] == '"')
                    dir = dir.Substring(1);
                if (dir[^1] == '"')
                    dir = dir.Substring(0, dir.Length - 1);
                return dir;
            }

            return null;
        }

        public void ParseArguments(string[] args)
        {
            bool parseInputs = false, parseOutputs = false;
            foreach (var arg in args)
            {
                if (arg.StartsWith("--help"))
                {
                    ShowHelp = true;
                    parseInputs = parseOutputs = false;
                    break;
                }
                else if (arg.StartsWith("-i"))
                {
                    parseOutputs = false;
                    parseInputs = true;
                }
                else if (arg.StartsWith("-o"))
                {
                    parseInputs = false;
                    parseOutputs = true;
                }
                else if (parseInputs)
                {
                    InputFiles.Add(arg);
                }
                else if (parseOutputs)
                {
                    OutputFiles.Add(arg);
                }
            }

            for (int i = OutputFiles.Count; i < InputFiles.Count; i++)
            {
                OutputFiles.Add(InputFiles[i]);
            }
        }
    }
}