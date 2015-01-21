using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Winterleaf.SharedServices.Util
{
    public class Solution
    {
        //internal class SolutionParser
        //Name: Microsoft.Build.Construction.SolutionParser
        //Assembly: Microsoft.Build, Version=4.0.0.0

        private static readonly Type s_SolutionParser;
        private static readonly PropertyInfo s_SolutionParser_solutionReader;
        private static readonly MethodInfo s_SolutionParser_parseSolution;
        private static readonly PropertyInfo s_SolutionParser_projects;

        static Solution()
        {
            s_SolutionParser = Type.GetType("Microsoft.Build.Construction.SolutionParser, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, false);
            if (s_SolutionParser != null)
                {
                s_SolutionParser_solutionReader = s_SolutionParser.GetProperty("SolutionReader", BindingFlags.NonPublic | BindingFlags.Instance);
                s_SolutionParser_projects = s_SolutionParser.GetProperty("Projects", BindingFlags.NonPublic | BindingFlags.Instance);
                s_SolutionParser_parseSolution = s_SolutionParser.GetMethod("ParseSolution", BindingFlags.NonPublic | BindingFlags.Instance);
                }
        }

        public Solution(string solutionFileName)
        {
            try
                {
                if (s_SolutionParser == null)
                    throw new InvalidOperationException("Can not find type 'Microsoft.Build.Construction.SolutionParser' are you missing a assembly reference to 'Microsoft.Build.dll'?");
                object solutionParser = s_SolutionParser.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).First().Invoke(null);
                using (StreamReader streamReader = new StreamReader(solutionFileName))
                    {
                    s_SolutionParser_solutionReader.SetValue(solutionParser, streamReader, null);
                    s_SolutionParser_parseSolution.Invoke(solutionParser, null);
                    }
                List<SolutionProject> projects = new List<SolutionProject>();
                Array array = (Array)s_SolutionParser_projects.GetValue(solutionParser, null);
                for (int i = 0; i < array.Length; i++)
                    projects.Add(new SolutionProject(array.GetValue(i)));
                Projects = projects;
                }
            catch (Exception err)
                {
                MessageBox.Show("Error occured: " + err.Message);
                }
            
        }

        public List<SolutionProject> Projects { get; private set; }
    }

    [DebuggerDisplay("{ProjectName}, {RelativePath}, {ProjectGuid}")]
    public class SolutionProject
    {
        private static readonly Type s_ProjectInSolution;
        private static readonly PropertyInfo s_ProjectInSolution_ProjectName;
        private static readonly PropertyInfo s_ProjectInSolution_RelativePath;
        private static readonly PropertyInfo s_ProjectInSolution_ProjectGuid;

        static SolutionProject()
        {
            s_ProjectInSolution = Type.GetType("Microsoft.Build.Construction.ProjectInSolution, Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", false, false);
            if (s_ProjectInSolution != null)
                {
                s_ProjectInSolution_ProjectName = s_ProjectInSolution.GetProperty("ProjectName", BindingFlags.NonPublic | BindingFlags.Instance);
                s_ProjectInSolution_RelativePath = s_ProjectInSolution.GetProperty("RelativePath", BindingFlags.NonPublic | BindingFlags.Instance);
                s_ProjectInSolution_ProjectGuid = s_ProjectInSolution.GetProperty("ProjectGuid", BindingFlags.NonPublic | BindingFlags.Instance);
                }
        }

        public SolutionProject(object solutionProject)
        {
            ProjectName = s_ProjectInSolution_ProjectName.GetValue(solutionProject, null) as string;
            RelativePath = s_ProjectInSolution_RelativePath.GetValue(solutionProject, null) as string;
            ProjectGuid = s_ProjectInSolution_ProjectGuid.GetValue(solutionProject, null) as string;
        }

        public string ProjectName { get; private set; }
        public string RelativePath { get; private set; }
        public string ProjectGuid { get; private set; }
    }
}