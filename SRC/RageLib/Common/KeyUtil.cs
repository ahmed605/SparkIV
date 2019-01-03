/**********************************************************************\

 RageLib
 Copyright (C) 2008  Arushan/Aru <oneforaru at gmail.com>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.

\**********************************************************************/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using Microsoft.Win32;
using Ookii.Dialogs;

namespace RageLib.Common
{
    public abstract class KeyUtil
    {
        public static string dir { get; set;}

        public static class StringExtensions
        {
            public static bool IsNullOrWhiteSpace(string value)
            {
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (!char.IsWhiteSpace(value[i]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
        public abstract string ExecutableName { get; }
        protected abstract string[] PathRegistryKeys { get; }
        protected abstract uint[] SearchOffsets { get; }

        public string FindGameDirectory()
        {

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ExecutableName)))
            {
                dir = Directory.GetCurrentDirectory();
            }
            else
            {
                if (ExecutableName == "GTAIV.exe" && File.Exists(@"path.iv") && !StringExtensions.IsNullOrWhiteSpace(File.ReadAllText(@"path.iv")) && File.Exists(Path.Combine(File.ReadAllText(@"path.iv"), ExecutableName)))
                {
                    dir = File.ReadAllText(@"path.iv");
                }
                else
                {
                    if (ExecutableName == "EFLC.exe" && File.Exists(@"path.eflc") && !StringExtensions.IsNullOrWhiteSpace(File.ReadAllText(@"path.eflc")) && File.Exists(Path.Combine(File.ReadAllText(@"path.eflc"), ExecutableName)))
                    {
                        dir = File.ReadAllText(@"path.eflc");
                    }
                    else
                    {
                        var keys = PathRegistryKeys;
                        foreach (var s in keys)
                        {
                            RegistryKey key;
                            if ((key = Registry.LocalMachine.OpenSubKey(s)) != null)
                            {
                                if (File.Exists(Path.Combine(key.GetValue("InstallFolder").ToString(), ExecutableName)))
                                {
                                    dir = key.GetValue("InstallFolder").ToString();
                                    key.Close();
                                    break;
                                }
                                else
                                {
                                    var fbd = new VistaFolderBrowserDialog();
                                    fbd.Description = "Select game folder";
                                    //DialogResult result = fbd.ShowDialog();
                                    if (fbd.ShowDialog() == DialogResult.OK && !StringExtensions.IsNullOrWhiteSpace(fbd.SelectedPath))
                                    {
                                        dir = fbd.SelectedPath;
                                        break;
                                    }
                                    else
                                    {
                                        MessageBox.Show("Please select game folder.");
                                        Application.Exit();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return dir;
        }

        public byte[] FindKey(string gamePath)
        {
            var gameExe = Path.Combine(gamePath, ExecutableName);

            const string validHash = "DEA375EF1E6EF2223A1221C2C575C47BF17EFA5E";
            byte[] key = null;

            var fs = new FileStream(gameExe, FileMode.Open, FileAccess.Read);

            foreach (var u in SearchOffsets)
            {
                if (u <= fs.Length - 32)
                {
                    var tempKey = new byte[32];
                    fs.Seek(u, SeekOrigin.Begin);
                    fs.Read(tempKey, 0, 32);

                    var hash = BitConverter.ToString(SHA1.Create().ComputeHash(tempKey)).Replace("-", "");
                    if (hash == validHash)
                    {
                        key = tempKey;
                        break;
                    }
                }
            }

            fs.Close();

            return key;
        }
        public class GetDir
        {
            public static string Get()
            {
                return KeyUtil.dir;
            }
        }
    }
}