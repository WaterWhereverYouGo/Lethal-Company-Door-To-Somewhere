using System;
using System.Reflection;

namespace DoorToSomewhereMod.Logger
{
    public class LocalLogger
    {

        public static LocalLogger Instance;

        public LocalLogger()
        {
            Instance = this;
        }

        public static void LogException(MethodBase methodBase, Exception e)
        {
            string functionPath = methodBase.DeclaringType.Name + "." + methodBase.Name;
            DoorToSomewhereBase.logger.LogError($"EXCEPTION OCCURRED: Plugin {DoorToSomewhereBase.modName} failed in {functionPath}. {e.Message}");
        }
    }
}
