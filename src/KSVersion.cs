using System;
using System.Collections.Generic;
//using System.Net.WebClient;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KerbalStuffVersion_BOM
{
    class ModNotify
    {
    }
    class CheckModVersion
    {
        private string modApiUrl = "https://kerbalstuff.com/api/mod/";
        private string selfIdent = "";
        private string version = "0";
        public int selfVersion = 1;
        private string BOMVer = "1.0";
        public Dictionary<string, string> dataLog = new Dictionary<string, string>();//this.dataLog.Add("cat", 2);
        public int modID {//what is you mod id with kerbalStuff
            set {
                this.selfIdent = string.Concat("ksp-mod-",value.ToString());
                this.modID=value;
            } 
        }
        public int hrInterval = 24;//hourly interval in which to check for updates - 12 (half day) - 24 (one day) - 84 (half week) - 168 (one week)
        public CheckModVersion(string modVersion, int id, string stringID){
            version = modVersion;
            if (id <= 0) { modID = 0; }
            else { modID = id; }
            if (stringID.Trim().Length > 0) { selfIdent = stringID; }
            //else{} modID sets selfIdent for us
        }
        public bool UpdateCheck(){
            byte[] emptyByte = null;
            //HashTable headers = new Hashtable();
            //Dictionary<string, string> headers = new Dictionary<string, string>();
            //headers.Add("User-Agent", "KS-ModStatistics/BOM-Updater" + BOMVer + "/" + version + " (" + selfVersion.ToString() + ")");
            // Start a download of the given URL
            //var www = new WWW(modApiUrl, emptyByte, headers);

        }
        public void logGet()
        {
            //DeserializeObject(String json);
        }
        public void logSet(bool urlResult)
        {

            //} while (KSP.IO.File.Exists<IDisposable>(filename));
            //System.IO.File.WriteAllBytes(filename, bytes);
            
            KSP.IO.TextWriter fileWriter = KSP.IO.TextWriter.CreateForType<IDisposable>("asdf.asdf");//thanks kethane SVN :) https://mmi-kethane-plugin.googlecode.com/svn-history/r2/trunk/Controller.cs
            fileWriter.WriteLine(JsonConvert.SerializeObject(dataLog));
            fileWriter.Close();
            //KSP.IO.File.WriteAllText<MuMechJebPod2>(KSP.IO.File.ReadAllText<MuMechJebPod2>(KSPUtil.ApplicationRootPath + "Parts/mumech_MechJebPod2/default.craft"), KSPUtil.ApplicationRootPath + "Ships/__mechjebpod_tmp.craft");
            //System.IO.File.Copy(KSPUtil.ApplicationRootPath + "Parts/mumech_MechJebPod2/default.craft", KSPUtil.ApplicationRootPath + "Ships/__mechjebpod_tmp.craft", true);

        }
    }

    /*
     * FROM https://raw.githubusercontent.com/pweingardt/KSPMissionController/master/plugin/Parser.cs
     * KSP.IO Handler
    */

    /// <summary>
    /// Parses a file into an object. Uses reflection.
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// Constant NamespacePrefix to avoid security issues
        /// </summary>
        private const String NamespacePrefix = "KerbalStuffVersion_BOM.";

        /// <summary>
        /// Writes the object into the passed file. (uses KSP.IO)
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <param name="path">Path.</param>
        public void writeObject(object obj, String path)
        {
            KSP.IO.TextWriter writer = KSP.IO.TextWriter.CreateForType<KerbalStuffVersion_BOM>(path);
            writeObject(writer, obj);
            writer.Close();
        }

        private void writeObject(KSP.IO.TextWriter writer, object obj)
        {
            Type t = obj.GetType();
            writer.WriteLine(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        /// Parses the passed file (uses KSP.IO) and returns the parsed object
        /// </summary>
        /// <returns>The file.</returns>
        /// <param name="path">Path.</param>
        public object readFile(String path)
        {
            KSP.IO.TextReader reader = KSP.IO.TextReader.CreateForType<KerbalStuffVersion_BOM>(path);
            try
            {
                return readObject(reader);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                return null;
            }
            finally
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Parses the object.
        /// </summary>
        /// <returns>The object.</returns>
        /// <param name="reader">Reader.</param>
        private object readObject(KSP.IO.TextReader reader)
        {
            object obj = null;

            // Get the name of the class an create an instance
            String n = nextLine(reader);
            obj = readObject(reader, n);

            return obj;
        }

        private object readObject(KSP.IO.TextReader reader, String name)
        {
            Type t = Type.GetType(NamespacePrefix + name);
            object obj = Activator.CreateInstance(t);
            Dictionary<string, string> dataIn = new Dictionary<string, string>();

            String n;
            // now parse the lines and put them into the dictionary.
            // if there is another object inside, parse it and invoke "add"
            while ((n = nextLine(reader)) != null )
            {
                string vname = "" + n;
                string value = "" + n;
                dataIn.Add(vname, value);
            }
            return obj;
        }

        /// <summary>
        /// Reads the next single line
        /// </summary>
        /// <returns>The line.</returns>
        /// <param name="reader">Reader.</param>
        private String nextLine(KSP.IO.TextReader reader)
        {
            String str = reader.ReadLine();
            return str;
        }
    }
}
