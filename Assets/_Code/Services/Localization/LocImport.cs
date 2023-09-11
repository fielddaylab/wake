using BeauUtil.Tags;
using BeauUtil;
using Leaf.Compiler;
using Leaf;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.IO;
using BeauData;

namespace Aqua
{
    public static class LocImport
    {
        private static string inImportFilePath = "Assets/_Content/Text/Localization/TranslationToImport.csv";
        private static string outImportFilePathRoot = "Assets\\_Content\\Text\\";

        private static char CSV_DELIM = '^'; // Ensure this matches the delim when the csv is exported!

        [MenuItem("Aqualab/Localization/Import Spanish Translation")] 
        public static void ImportSpanishTranslation() {
            int translationIndex = 2; // the column index of the translated text

            ImportTranslation(FourCC.Parse("ES"), translationIndex);
        }

        private static void ImportTranslation(FourCC langCode, int translationIndex) {
            FileStream importStream = File.OpenRead(inImportFilePath);
            StreamReader sr = new StreamReader(importStream);

            int nameIndex = 0;
            StringBuilder stringBuilder = new StringBuilder();
            string prevName = ""; // Name of previous node. Used for adding multiple translation lines in a single node
            // Iterate through all csv lines
            sr.ReadLine(); // skip initial line
            while (!sr.EndOfStream) {
                string currLine = sr.ReadLine();
                bool restoreQuote = currLine.Contains("\"\"\"") && !currLine.Contains("\".\"\""); // TODO: still somehow winding up with a couple entries ending with "."
                StringSlice[] array = StringSlice.Split(currLine, new StringUtils.CSV.Splitter(true), (StringSplitOptions)0);

                if (array.Length != 6) {
                    Debug.Log("Note: unexpected number of slices for line " + array[0] + ". Has " + array.Length + " slices, expected 6");
                }

                // extract line name
                string nameStr = array[nameIndex].ToString();
                // Parse '|' into "__"
                if (nameStr.Contains("|")) {
                    nameStr = nameStr.Replace("|", "__");
                }
                if (nameStr.Contains(":")) {
                    nameStr = nameStr.Replace(":", "_c_");
                }

                /*
                // remove unique line identifiers
                if (nameStr.Contains(":")) {
                    nameStr = nameStr.Substring(0, nameStr.IndexOf(":"));
                }
                */

                /*
                if (!nameStr.Equals(prevName)) {
                    // add spacing between nodes
                    stringBuilder.Append("\n\n");

                    // set up new node
                    stringBuilder.Append(":: " + nameStr);
                }
                stringBuilder.Append("\n");
                */

                // add spacing between nodes
                stringBuilder.Append("\n\n");

                // set up new node
                stringBuilder.Append(":: " + nameStr);
                stringBuilder.Append("\n");


                // Add lines
                stringBuilder.Append(array[translationIndex]);
                if (restoreQuote) { stringBuilder.Append("\""); }

                prevName = nameStr;
            }

            importStream.Close();


            string codeStr = langCode.ToString().Trim();
            File.WriteAllText(outImportFilePathRoot + codeStr + "\\" + codeStr + "-Loc.aqloc", stringBuilder.Flush());

            Debug.Log(codeStr + " translation imported!");
        }
    }
}
