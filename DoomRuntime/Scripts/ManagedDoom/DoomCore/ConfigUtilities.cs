//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Device;

namespace ManagedDoom
{
    public static class ConfigUtilities
    {
        private static readonly string[] iwadNames = new string[]
        {
            "DOOM2.WAD",
            "PLUTONIA.WAD",
            "TNT.WAD",
            "DOOM.WAD",
            "DOOM1.WAD",
            "FREEDOOM2.WAD",
            "FREEDOOM1.WAD"
        };

        public static string OverrideBaseDir;

        private static string GetBaseDir()
        {
            if (!string.IsNullOrEmpty(OverrideBaseDir))
                return OverrideBaseDir;

            var asmLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(asmLocation) ?? "";
        }

        public static string GetDataPath()
        {
            var baseDir = GetBaseDir();
            var dir = Path.Combine(baseDir, "Config");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetConfigPath()
        {
            return Path.Combine(GetDataPath(), "managed-doom.cfg");
        }

        public static string GetDefaultIwadPath()
        {
            var exeDirectory = GetDataPath();
            foreach (var name in iwadNames)
            {
                var path = Path.Combine(exeDirectory, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            foreach (var name in iwadNames)
            {
                var path = Path.Combine(currentDirectory, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new Exception("No IWAD was found!");
        }

        public static bool IsIwad(string path)
        {
            var name = Path.GetFileName(path).ToUpper();
            return iwadNames.Contains(name);
        }

        public static string[] GetWadPaths(CommandLineArgs args)
        {
            var paths = new List<string>();

            if (args.iwad.Present && !string.IsNullOrEmpty(args.iwad.Value))
            {
                var iwadPath = args.iwad.Value;
                if (!Path.IsPathRooted(iwadPath))
                    iwadPath = Path.GetFullPath(iwadPath);
                if (!File.Exists(iwadPath))
                    throw new Exception($"IWAD path from args not found: {iwadPath}");
                paths.Add(iwadPath);
            }

            if (args.file.Present && args.file.Value != null)
            {
                foreach (var pwad in args.file.Value)
                {
                    var p = pwad;
                    if (!Path.IsPathRooted(p))
                        p = Path.GetFullPath(p);
                    if (File.Exists(p))
                        paths.Add(p);
                }
            }

            if (paths.Count == 0)
            {
                var defaultIwad = GetDefaultIwadPath();
                paths.Add(defaultIwad);
            }

            return paths.ToArray();
        }

    }
}
