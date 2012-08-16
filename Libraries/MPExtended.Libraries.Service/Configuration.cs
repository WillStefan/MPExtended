﻿#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using MPExtended.Libraries.Service.Util;
using MPExtended.Libraries.Service.ConfigurationContracts;

namespace MPExtended.Libraries.Service
{
    public class Configuration
    {
        public const int DEFAULT_PORT = 4322;

        public delegate void ConfigurationReloadedEventHandler();
        public static event ConfigurationReloadedEventHandler Reloaded;

        private static FileSystemWatcher watcher;

        private static ConfigurationContracts.Services serviceConfig = null;
        private static MediaAccess mediaConfig = null;
        private static Streaming streamConfig = null;
        private static WebMediaPortalHosting webmpHostingConfig = null;

        public static ConfigurationContracts.Services Services
        {
            get
            {
                if (serviceConfig == null)
                    serviceConfig = new ConfigurationContracts.Services(GetPath("Services.xml"), GetDefaultPath("Services.xml"));

                return serviceConfig;
            }
        }

        public static MediaAccess Media
        {
            get
            {
                if (mediaConfig == null)
                    mediaConfig = new MediaAccess(GetPath("MediaAccess.xml"), GetDefaultPath("MediaAccess.xml"));

                return mediaConfig;
            }
        }

        public static Streaming Streaming
        {
            get
            {
                if (streamConfig == null)
                    streamConfig = new Streaming(GetPath("Streaming.xml"), GetDefaultPath("Streaming.xml"));

                return streamConfig;
            }
        }

        public static WebMediaPortalHosting WebMediaPortalHosting
        {
            get
            {
                if (webmpHostingConfig == null)
                    webmpHostingConfig = new WebMediaPortalHosting(GetPath("WebMediaPortalHosting.xml"), GetDefaultPath("WebMediaPortalHosting.xml"));

                return webmpHostingConfig;
            }
        }

        public static string GetPath(string filename)
        {
            string path = Path.Combine(Installation.GetConfigurationDirectory(), filename);

            if (!File.Exists(path))
            {
                if (Installation.GetFileLayoutType() == FileLayoutType.Source)
                {
                    // When running from source they should exists
                    throw new FileNotFoundException("Couldn't find config - what did you do?!?!");
                }
                else
                {
                    // copy from default location
                    File.Copy(GetDefaultPath(filename), path);

                    // allow everyone to write to the config
                    var acl = File.GetAccessControl(path);
                    SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    FileSystemAccessRule rule = new FileSystemAccessRule(everyone, FileSystemRights.FullControl, AccessControlType.Allow);
                    acl.AddAccessRule(rule);
                    File.SetAccessControl(path, acl);
                }
            }

            return path;
        }

        public static string GetDefaultPath(string filename)
        {
            if (Installation.GetFileLayoutType() == FileLayoutType.Installed)
            {
                MPExtendedProduct product = filename.StartsWith("WebMediaPortal") ? MPExtendedProduct.WebMediaPortal : MPExtendedProduct.Service;
                return Path.Combine(Installation.GetInstallDirectory(product), "DefaultConfig", filename);
            }
            else
            {
                return GetPath(filename);
            }
        }

        internal static string PerformFolderSubstitution(string input)
        {
            // program data
            input = input.Replace("%ProgramData%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

            // mp settings
            input = Regex.Replace(input, @"%mp-([^-]+)-([^-]+)%", delegate(Match match)
            {
                var section = Mediaportal.ReadSectionFromConfigFile(match.Groups[1].Value);
                if (!section.ContainsKey(match.Groups[2].Value))
                {
                    Log.Info("Replacing unknown Mediaportal path substitution %mp-{0}-{1}% with empty string", match.Groups[1].Value, match.Groups[2].Value);
                    return String.Empty;
                }
                else
                {
                    return section[match.Groups[2].Value];
                }
            });

            return input;
        }

        public static void EnableChangeWatching()
        {
            if (watcher != null)
            {
                return;
            }

            watcher = new FileSystemWatcher(Installation.GetConfigurationDirectory(), "*.xml");
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(delegate(object sender, FileSystemEventArgs e)
            {
                string fileName = Path.GetFileName(e.FullPath);
                if (fileName == "Services.xml") serviceConfig = null;
                if (fileName == "MediaAccess.xml") mediaConfig = null;
                if (fileName == "Streaming.xml") streamConfig = null;
                if (fileName == "WebMediaPortalHosting.xml") webmpHostingConfig = null;

                if (Reloaded != null)
                {
                    Reloaded();
                }
            });

            // start watching
            watcher.EnableRaisingEvents = true;
        }

        public static void DisableChangeWatching()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher = null;
            }
        }
    }
}
