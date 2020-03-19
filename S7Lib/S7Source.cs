using System;

using SimaticLib;


namespace S7Lib
{
    class S7Source
    {
        /// <summary>
        /// Imports source into project
        /// </summary>
        /// <param name="parent">Parent S7SWItem container object</param>
        /// <param name="sourceFilePath">Path to source file</param>
        /// <param name="sourceType">SW object type</param>
        /// <returns>0 on success, -1 otherwise</returns>
        public static int ImportSource(S7SWItems parent, string sourceFilePath,
            S7SWObjType sourceType = S7SWObjType.S7Source)
        {
            var log = Api.CreateLog();
            string sourceName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
            try    
            {
                var item = parent.Add(sourceName, sourceType, sourceFilePath);
            }
            catch (Exception exc)
            {
                log.Error($"Could not import source {sourceName} from {sourceFilePath}: ", exc);
                return -1;
            }
            return 0;
        }
    }
}
