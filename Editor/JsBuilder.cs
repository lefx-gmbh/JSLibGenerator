using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.VersionControl;

namespace LEFX.JsLibGen
{
    ///<author email="a.schaub@lefx.de">Aron Schaub</author>
    public abstract class JsBuilder
    {
        public string ParentDir { get; set; }

        public Type Type
        {
            get => type;
            set
            {
                if (value != type)
                {
                    CSharpToJsFunctions.Clear();
                    JsToCSharpFunctions.Clear();
                }

                type = value;
            }
        }

        public bool DebugPrintsEnabled { get; set; }
        protected readonly List<MethodInfo> CSharpToJsFunctions = new List<MethodInfo>();
        protected readonly List<MethodInfo> JsToCSharpFunctions = new List<MethodInfo>();
        private Type type;

        public virtual void Generate()
        {
            CreateFolders();
        }

        public void AddCSharpToJsFunction(MethodInfo info)
        {
            CSharpToJsFunctions.Add(info);
        }

        public void AddJsToCSharpFunction(MethodInfo info)
        {
            JsToCSharpFunctions.Add(info);
        }

        protected void CreateFolders()
        {
            var basePath = "Assets";
            foreach (string folder in ParentDir.Split("/"))
            {
                string targetFolder = Path.Join(basePath, folder);
                if (!AssetDatabase.IsValidFolder(targetFolder))
                {
                    AssetDatabase.CreateFolder(basePath, folder);
                }

                basePath = targetFolder;
            }
        }

        protected void WriteAsset(string fileEnding, string content)
        {
            string filePath = Path.Join("Assets", ParentDir, $"{Type.Name}.{fileEnding}");
            if (!File.Exists(filePath) || File.ReadAllText(filePath) != content)
            {
                Provider.Checkout(filePath, CheckoutMode.Asset);
                File.WriteAllText(filePath, content);
                AssetDatabase.ImportAsset(filePath);
            }
        }
    }

    class JsLibBuilder : JsBuilder
    {
        public override void Generate()
        {
            CreateFolders();
            var content = "mergeInto(LibraryManager.library, {\n";
            foreach (var info in CSharpToJsFunctions)
            {
                string funcName = info.Name[(info.Name.IndexOf("_", StringComparison.Ordinal) + 1)..].FirstCharacterToLower();
                ParameterInfo[] parameterInfos = info.GetParameters();
                content += $"\t{info.Name}: function ({string.Join(',', parameterInfos.Select(v => v.Name))}) {{";
                if (DebugPrintsEnabled)
                {
                    if (parameterInfos.Length > 0)
                        content += $@"console.log('{info.Name} ('+{string.Join("+', '+", parameterInfos.Select(v =>
                        {
                            if (v.ParameterType == typeof(string))
                                return $"UTF8ToString({v.Name})";
                            else
                                return v.Name;
                        }))}+')');";
                    else
                        content += $"console.log('{info.Name} ()');";
                }

                IEnumerable<string> parameters = parameterInfos.Select(v =>
                {
                    string paramName = v.Name;
                    return v.ParameterType == typeof(string) ? $"UTF8ToString({paramName})" : paramName;
                });
                content += $"Module.{Type.Name}.{funcName}({string.Join(',', parameters)}); }},\n";
            }

            content += "});";
            WriteAsset("jslib", content);
        }
    }

    class JsPreBuilder : JsBuilder
    {
        public override void Generate()
        {
            CreateFolders();
            string content = $"Module['{Type.Name}'] = Module['{Type.Name}'] || {{}};\n" +
                             $"class {Type.Name} {{\n";
            // $"\tmappings = [];\n";
            foreach (var info in CSharpToJsFunctions)
            {
                string funcName = info.Name[(info.Name.IndexOf("_", StringComparison.Ordinal) + 1)..].FirstCharacterToLower();
                if (funcName.Equals("_name_"))
                {
                    // string paramName = info.GetParameters().Select(v => v.Name).FirstOrDefault();
                    // content += $"\t{funcName} ({paramName}) {{";
                    // content += $"Module.{Type.Name}.mappings.push({paramName});";
                    // content += $"}}\n";
                }
                else
                {
                    content += $"\t{funcName} ({string.Join(',', info.GetParameters().Select(v => v.Name))}) {{";
                    // if (DebugPrintsEnabled) //    
                    //     content += $"throw '{info.Name} is not implemented.';";
                    content += $"}}\n";
                }
            }

            foreach (var info in JsToCSharpFunctions)
            {
                string funcName = info.Name[(info.Name.IndexOf("_", StringComparison.Ordinal) + 1)..];
                content += $"\t{funcName.FirstCharacterToLower()} (id, {string.Join(',', info.GetParameters().Select(v => v.Name))}) {{";
                // content += $"for (var mapping of Module.{Type.Name}.mappings) {{";
                content += $"Module.SendMessage(id,'{funcName}'";
                content = info.GetParameters().Aggregate(content, (current, parameter) => current + ", " + parameter.Name);
                content += ");";
                content += "}\n";
            }

            content += $"}}\nModule.{Type.Name} = new {Type.Name}();";
            WriteAsset("jspre", content);
        }
    }
}