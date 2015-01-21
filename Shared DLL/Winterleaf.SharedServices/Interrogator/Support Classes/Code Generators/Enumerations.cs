using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using Winterleaf.SharedServices.Interrogator.Configuration;
using Winterleaf.SharedServices.Interrogator.Containers;
using Winterleaf.SharedServices.Interrogator.Parsing;

namespace Winterleaf.SharedServices.Interrogator
{
    internal class Enumerations
    {
        private readonly CodeParsing _mCodeParsing;
        private ConfigFiles MDntcConfig;
        private Logger.Logger mLogger;

        public Enumerations(ref CodeParsing cp, ref ConfigFiles cf, ref Logger.Logger logger)
        {
            _mCodeParsing = cp;
            MDntcConfig = cf;
            mLogger = logger;
        }

        private string DoTypeLookup(string senum, InitPersistData ipd)
        {
            string StartingSenum = senum;
            String PropertyType = string.Empty;

            if (ipd.mStructureType == InitPersistData.StructureType.NotSet)
                ipd.mStructureType = InitPersistData.StructureType.TypeVariable;

            senum = senum.Replace("*", "");

            if (senum == "TypePID")
                PropertyType = "int";
            else if (senum == "TypeString" || senum == "StringTableEntry" || senum.ToLower() == "string" || senum.Replace(" ", "") == "constchar")
                PropertyType = "String";
            else if (senum.Replace(" ", "").Trim() == "Vector<F32>")
                PropertyType = "VectorFloat ";
            else if (senum.Replace(" ", "").Trim() == "Vector<S32>")
                PropertyType = "VectorInt ";
            else if (senum.Replace(" ", "").Trim() == "Vector<bool>")
                PropertyType = "VectorBool ";

            else if (senum.StartsWith("SimObjectRef"))
                {
                string tmp = senum.Split('<')[1];

                if (tmp.Contains("::"))
                    tmp = tmp.Substring(tmp.IndexOf("::", System.StringComparison.Ordinal)+2);

                if (MDntcConfig.ObjectParent.ContainsKey(tmp))
                    {
                    PropertyType = Interrogator.constants.general.classPrefix + tmp;
                    ipd.mStructureType = InitPersistData.StructureType.T3DObject;
                    }
                else
                    {
                    if (MDntcConfig.ObjectParent.ContainsValue(tmp))
                        {
                        PropertyType = Interrogator.constants.general.classPrefix + tmp;
                        ipd.mStructureType = InitPersistData.StructureType.T3DObject;
                        }
                    else
                        throw new Exception("The object type of '" + tmp + "' cannot be resolved.");
                    }
                }
            else if (MDntcConfig.ObjectParent.ContainsKey(senum))
                {
                PropertyType = Interrogator.constants.general.classPrefix + senum;
                ipd.mStructureType = InitPersistData.StructureType.T3DObject;
                }
            else if (MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(senum))
                {
                PropertyType = Helpers.convertC2Cs(senum, false, ref MDntcConfig);
                if (PropertyType == "[MarshalAs(UnmanagedType.LPStr)] StringBuilder")
                    PropertyType = "string";
                }

            else if (MDntcConfig.PreGen_CPP_ObjParseDef.ContainsKey(senum))
                {
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(senum))
                    {
                    PropertyType = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[senum].cstype;
                    if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[senum].itype == CPPEntityType.Enum)
                        ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                    }
                else
                    throw new Exception("Property Type: '" + senum + "' not found in C++ Class/Enum Map To C# Class/Enum");
                }
            else if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(senum))
                {
                PropertyType = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[senum].cstype;
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[senum].itype == CPPEntityType.Enum)
                    ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                }

            else if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS.ContainsKey(ipd.MClassName + "::" + senum))
                {
                PropertyType = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ipd.MClassName + "::" + senum].cstype;
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ipd.MClassName + "::" + senum].itype == CPPEntityType.Enum)
                    ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                }

                //todo Removed this.
            else if (MDntcConfig.PreGen_CPP_ObjParseDef.ContainsKey(ipd.MClassName + "__" + senum))
                {
                PropertyType = MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ipd.MClassName + "__" + senum].cstype;
                if (MDntcConfig.PreGen_CS__TypeConvCPPtoCS[ipd.MClassName + "__" + senum].itype == CPPEntityType.Enum)
                    ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                }

            else if (_mCodeParsing.Data_Enumerations.Keys.Contains(senum.Replace("*", "")))
                {
                _mCodeParsing.Data_Enumerations[senum].mIsUsed = true;
                PropertyType = _mCodeParsing.Data_Enumerations[senum.Replace("*", "")].mName; //.Replace("::", "__");
                ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                }
            else
                {
                bool found = false;
                string formalname = ipd.MClassName + "::" + senum.Replace("*", "");
                if (formalname.StartsWith("::"))
                    {
                    //Let us dig through the #Defines.
                    foreach (DefinedValues dv in _mCodeParsing.Data_DefinedClasses)
                        {
                        if (dv._value == senum)
                            {
                            PropertyType = "R::" + dv._name;
                            formalname = PropertyType;
                            break;
                            }
                        }
                    }

                foreach (KeyValuePair<string, EnumData> menumeration in _mCodeParsing.Data_Enumerations)
                    {
                    if (menumeration.Key.Contains(formalname))
                        {
                        menumeration.Value.mIsUsed = true;
                        PropertyType = menumeration.Key; //.Replace("::", "__");
                        ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                        found = true;
                        break;
                        }
                    }
                if (!found)
                    {
                    if (_mCodeParsing.Data_ConsoleTypes.ContainsKey(senum))
                        {
                        string testtype = _mCodeParsing.Data_ConsoleTypes[senum];
                        testtype = Helpers.convertC2Cs(testtype, true, ref MDntcConfig);

                        if (MDntcConfig.ObjectParent.ContainsKey(testtype) || MDntcConfig.ObjectParent.ContainsValue(testtype))
                            {
                            PropertyType = Interrogator.constants.general.classPrefix + testtype;
                            ipd.mStructureType = InitPersistData.StructureType.T3DObject;
                            }
                        else
                            PropertyType = _mCodeParsing.Data_ConsoleTypes[senum];
                        }
                    }
                }

            if (PropertyType.Trim() == "")
                PropertyType = senum;

            if (StartingSenum == PropertyType)
                Helpers.returnvalType(PropertyType, ref MDntcConfig);
            else
                PropertyType = DoTypeLookup(PropertyType, ipd);

            PropertyType = PropertyType.Trim();

            if (PropertyType != "sbyte" && PropertyType != "int" &&// !PropertyType.StartsWith("co") && 
                !MDntcConfig.ObjectParent.ContainsKey(PropertyType) && PropertyType != "bool" && PropertyType != "double" && PropertyType != "float" && PropertyType != "uint" && PropertyType != "int" && PropertyType != "void" && PropertyType != "AngAxisF" && PropertyType != "Box3F" && PropertyType != "ColorF" && PropertyType != "ColorI" && PropertyType != "EaseF" && PropertyType != "Point2F" && PropertyType != "Point2I" && PropertyType != "Point3F" && PropertyType != "Point3I" && PropertyType != "Point4F" && PropertyType != "Polyhedron" && PropertyType != "RectF" && PropertyType != "RectI" && PropertyType != "RectSpacingI" && PropertyType != "TransformF" && PropertyType != "VectorInt" && PropertyType != "VectorFloat" && PropertyType != "VectorBool" && PropertyType != "String")
                {
                if (!PropertyType.StartsWith("Type"))
                    {
                    if (PropertyType.StartsWith("R::"))
                        {
                        if (_mCodeParsing.Data_Enumerations.ContainsKey("Type" + PropertyType.Substring(3)))
                            {
                            _mCodeParsing.Data_Enumerations["Type" + PropertyType.Substring(3)].mIsUsed = true;
                            ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                            PropertyType = "Type" + PropertyType.Substring(3);
                            }
                        }
                    }

                //still hasn't resolved to a Type class.
                if (!PropertyType.StartsWith("Type"))
                    {
                    if (_mCodeParsing.Data_Enumerations.ContainsKey("Type" + PropertyType))
                        {
                        _mCodeParsing.Data_Enumerations["Type" + PropertyType].mIsUsed = true;
                        ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                        PropertyType = "Type" + PropertyType;
                        }
                    }
                if (!PropertyType.StartsWith("Type"))
                    {
                    if (PropertyType.StartsWith("R::"))
                        {
                        foreach (DefinedValues dv in _mCodeParsing.Data_DefinedClasses)
                            {
                            if (dv._name == PropertyType.Substring(3))
                                {
                                PropertyType = "Type" + dv._value;
                                ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                                break;
                                }
                            }
                        }
                    }

                if (!PropertyType.StartsWith("Type"))
                    {
                    foreach (DefinedValues dv in _mCodeParsing.Data_DefinedClasses)
                        {
                        if (dv._name == PropertyType)
                            {
                            PropertyType = "Type" + dv._value;
                            ipd.mStructureType = InitPersistData.StructureType.TypeEnumeration;
                            break;
                            }
                        }
                    }
                }
            if (PropertyType.ToLower().StartsWith("r::"))
                Console.Write("");
            return PropertyType;
        }

        private void ValidateEnums()
        {
            foreach (KeyValuePair<string, EnumData> kvp in _mCodeParsing.Data_Enumerations)
                {
                if (kvp.Key.StartsWith("Type"))
                    {
                    if (kvp.Key == "TypeSFXStatus")
                        Console.Write("");
                    InitPersistData ipd = new InitPersistData();

                    string temp = DoTypeLookup(kvp.Key.Substring(4), ipd);

                    if (temp.Contains("LogLevel"))
                        Console.WriteLine("");

                    if (!temp.StartsWith("Type") && temp != "int")
                        kvp.Value.mRealEnum = temp;
                    else
                        {
                        if (temp.Contains("LogLevel"))
                            Console.WriteLine("");
                        if (_mCodeParsing.Data_Enumerations.ContainsKey("R::" + kvp.Key.Substring(4)))
                            {
                            _mCodeParsing.Data_Enumerations["R::" + kvp.Key.Substring(4)].mIsUsed = true;
                            kvp.Value.mRealEnum = "R::" + kvp.Key.Substring(4);
                            }
                        else
                            {
                            if (temp.Contains("LogLevel"))
                                Console.WriteLine("");
                            bool found = false;
                            foreach (DefinedValues dv in _mCodeParsing.Data_DefinedClasses)
                                {
                                if (dv._value == kvp.Key.Substring(4))
                                    {
                                    if (_mCodeParsing.Data_Enumerations.ContainsKey("R::" + dv._name))
                                        {
                                        found = true;
                                        kvp.Value.mRealEnum = "R::" + dv._name;
                                        _mCodeParsing.Data_Enumerations["R::" + dv._name].mIsUsed = true;
                                        break;
                                        }
                                    }
                                }
                            }
                        //Maybe we need to look in defines?
                        }
                    }
                }
        }

        private void GetTypes()
        {
            //Dictionary<string, int> myTypes = new Dictionary<string, int>();
            List<string> tofind = new List<string>();
            foreach (InitPersistData ipd in _mCodeParsing.Data_InitPersists)
                {
                #region "Parse"

                string t = ipd.MType;
                string senum = ipd.MType.Trim();
                if (ipd.MType.Contains("<"))
                    {
                    t = ipd.MType.Substring(0, ipd.MType.IndexOf("<")).Trim();

                    senum = ipd.MType.Substring(ipd.MType.IndexOf("<") + 1).Trim();

                    int isi = senum.IndexOf(" ");
                    if (isi == -1)
                        isi = int.MaxValue;
                    int isx = senum.IndexOf(">");
                    if (isx == -1)
                        isx = int.MaxValue;

                    if (isi < isx)
                        senum = senum.Substring(0, senum.IndexOf(" "));
                    else
                        senum = senum.Substring(0, senum.IndexOf(">"));

                    if (senum.Trim() == "")
                        senum = ipd.MType.Trim();
                    }

                #endregion

                String PropertyType = string.Empty;

                //todo mark here

                PropertyType = DoTypeLookup(senum, ipd);

                ipd.mCSharpType = PropertyType;

                ipd.mCSharpType = Helpers.convertC2Cs(ipd.mCSharpType, true, ref MDntcConfig);

                if ((ipd.mCSharpType.Replace(" ", "") == "constchar") || (ipd.mCSharpType.Replace(" ", "") == "StringTableEntry"))
                    ipd.mCSharpType = "string";
                }
        }

        public void Start(string WLECSharpProjectFolder)
        {
            ValidateEnums();
            GetTypes();
            StringBuilder Code = new StringBuilder();
            Code.Append(@"

// Copyright (C) 2012 Winterleaf Entertainment L,L,C.

#region
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinterLeaf.Engine.Classes;
using WinterLeaf.Engine.Containers;
using WinterLeaf.Engine.Enums;
using System.ComponentModel;
using WinterLeaf.Engine.Interfaces;
#endregion

namespace WinterLeaf.Engine.Enums
    {
");
            IEnumerable<KeyValuePair<string, EnumData>> values = _mCodeParsing.Data_Enumerations.Where((menumeration => (menumeration.Value.mIsUsed || menumeration.Value.IsScript || MDntcConfig.PreGen_CPP_TypeConv.ContainsKey(menumeration.Key)) && menumeration.Key != "TSStatic::MaskBits"));

            double totalEnums = values.Count();
            double currentPos = 0;

            foreach (KeyValuePair<string, EnumData> menumeration in values)
                {
                currentPos += 1.0;
                mLogger.onProgressSubChange(currentPos/totalEnums, menumeration.Value.mName);
                Code.Append(menumeration.Value.ParseToCSharp(ref mLogger) + "\r\n");
                }
            Code.Append("}");

            if (Interrogator.self.mCSProject_Engine != null)
                {
                ProjectItem pi;
                if (!Interrogator.self.findProjectItem(Interrogator.self.mCSProject_Engine.ProjectItems, "Enums\\T3D_Enums.cs", out pi))
                    throw new Exception("Could not find enumeration file.");
                Window win;
                win = pi.Open();
                win.Visible = true;
                TextDocument textDoc = (TextDocument) pi.Document.Object("TextDocument");
                EditPoint editPoint = (EditPoint) textDoc.StartPoint.CreateEditPoint();
                EditPoint endPoint = (EditPoint) textDoc.EndPoint.CreateEditPoint();
                editPoint.Delete(endPoint);
                editPoint.Insert(Code.ToString());
                pi.Save();
                win.Close();
                }
            else
                {
                try
                    {
                    using (StreamWriter file = new StreamWriter(WLECSharpProjectFolder + "\\Enums\\T3D_Enums.cs", false))
                        file.WriteLine(Code.ToString());
                    }
                catch (Exception)
                    {
                    throw new Exception("Cannot write File T3D_Enums.cs.  Is it read-only?");
                    }
                }
        }
    }
}