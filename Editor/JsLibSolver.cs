using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace LEFX.JsLibGen
{
    ///<author email="a.schaub@lefx.de">Aron Schaub</author>
    public static class JsLibSolver
    {
        [InitializeOnLoadMethod]
        public static void CreateJsLibs()
        {
            Debug.Log("CreateJsLibs");

            const string parentDir = "Generated/JsLib";

            JsBuilder jsLibBuilder = new JsLibBuilder();
            JsBuilder jsPreBuilder = new JsPreBuilder();
#if DEBUG
            jsLibBuilder.DebugPrintsEnabled = jsPreBuilder.DebugPrintsEnabled = true;
#endif

            foreach (var type in FindTypesWithJsLibAttribute())
            {
                if (type.Namespace != null)
                    jsLibBuilder.ParentDir = jsPreBuilder.ParentDir = Path.Combine(parentDir, type.Namespace.Replace(".", "/")).Replace("\\", "/");
                jsLibBuilder.Type = jsPreBuilder.Type = type;
                foreach (var info in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static).Where(m => m.GetCustomAttributes<DllImportAttribute>(false).Any(attribute => attribute != null)))
                {
                    jsLibBuilder.AddCSharpToJsFunction(info);
                    jsPreBuilder.AddCSharpToJsFunction(info);
                }

                foreach (var info in type.GetMethods().Where(m => m.GetCustomAttributes<JsCallbackAttribute>(false).Any(attribute => attribute != null)))
                {
                    jsLibBuilder.AddJsToCSharpFunction(info);
                    jsPreBuilder.AddJsToCSharpFunction(info);
                }

                jsLibBuilder.Generate();
                jsPreBuilder.Generate();
            }
        }

        public static IEnumerable<Type> FindTypesWithJsLibAttribute()
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                from type in assembly.GetTypes()
                where type.GetCustomAttributes<JsLibAttribute>(false)
                    .Any(attribute => attribute != null)
                select type;
        }
    }
}