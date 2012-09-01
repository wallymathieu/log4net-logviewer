using System.IO;
using System;
using Core;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LogViewer
{
    public class FileUtil
    {
        public static FileStream OpenReadOnly(string fileName, long position=0)
        {
            var s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (position < s.Length)
            {
                s.Position = position;
            }
            else 
            {
                throw new OutOfBoundsException();
            }
            return s;
        }
        // This method is taken from Joe Woodbury's article at: http://www.codeproject.com/KB/cs/mrutoolstripmenu.aspx

        /// <summary>
        /// Shortens a pathname for display purposes.
        /// </summary>
        /// <param labelName="pathname">The pathname to shorten.</param>
        /// <param labelName="maxLength">The maximum number of characters to be displayed.</param>
        /// <remarks>Shortens a pathname by either removing consecutive components of a path
        /// and/or by removing characters from the end of the filename and replacing
        /// then with three elipses (...)
        /// <para>In all cases, the root of the passed path will be preserved in it's entirety.</para>
        /// <para>If a UNC path is used or the pathname and maxLength are particularly short,
        /// the resulting path may be longer than maxLength.</para>
        /// <para>This method expects fully resolved pathnames to be passed to it.
        /// (Use Path.GetFullPath() to obtain this.)</para>
        /// </remarks>
        /// <returns></returns>
        public static string ShortenPathname(string pathname, int maxLength)
        {
            if (pathname.Length <= maxLength)
                return pathname;

            string root = Path.GetPathRoot(pathname);
            if (root.Length > 3)
                root += Path.DirectorySeparatorChar;

            string[] elements = pathname.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            int filenameIndex = elements.GetLength(0) - 1;

            if (elements.GetLength(0) == 1) // pathname is just a root and filename
            {
                if (elements[0].Length > 5) // long enough to shorten
                {
                    // if path is a UNC path, root may be rather long
                    if (root.Length + 6 >= maxLength)
                    {
                        return root + elements[0].Substring(0, 3) + "...";
                    }
                    else
                    {
                        return pathname.Substring(0, maxLength - 3) + "...";
                    }
                }
            }
            else if ((root.Length + 4 + elements[filenameIndex].Length) > maxLength) // pathname is just a root and filename
            {
                root += "...\\";

                int len = elements[filenameIndex].Length;
                if (len < 6)
                    return root + elements[filenameIndex];

                if ((root.Length + 6) >= maxLength)
                {
                    len = 3;
                }
                else
                {
                    len = maxLength - root.Length - 3;
                }
                return root + elements[filenameIndex].Substring(0, len) + "...";
            }
            else if (elements.GetLength(0) == 2)
            {
                return root + "...\\" + elements[1];
            }
            else
            {
                int len = 0;
                int begin = 0;

                for (int i = 0; i < filenameIndex; i++)
                {
                    if (elements[i].Length > len)
                    {
                        begin = i;
                        len = elements[i].Length;
                    }
                }

                int totalLength = pathname.Length - len + 3;
                int end = begin + 1;

                while (totalLength > maxLength)
                {
                    if (begin > 0)
                        totalLength -= elements[--begin].Length - 1;

                    if (totalLength <= maxLength)
                        break;

                    if (end < filenameIndex)
                        totalLength -= elements[++end].Length - 1;

                    if (begin == 0 && end == filenameIndex)
                        break;
                }

                // assemble final string

                for (int i = 0; i < begin; i++)
                {
                    root += elements[i] + '\\';
                }

                root += "...\\";

                for (int i = end; i < filenameIndex; i++)
                {
                    root += elements[i] + '\\';
                }

                return root + elements[filenameIndex];
            }
            return pathname;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
        string path,
            [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder longPath,
            int longPathLength
            );
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
        string path,
            [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder shortPath,
            int shortPathLength
            );
    }
    public class OutOfBoundsException : Exception
    {
    }
    public class FileWithPosition
    {
        public string FileName { get; private set; }
        private long position = 0;
        public FileWithPosition(string filename)
        {
            this.FileName = filename;
        }

        public IEnumerable<LogEntry> Read(LogEntryParser parser)
        {
            using (var file = FileUtil.OpenReadOnly(FileName, position))
            {
                foreach (var item in parser.Parse(file))
                {
                    yield return item;
                }
                position = file.Position;
            }
        }

        public bool FileHasBecomeLarger()
        {
            using (var f = FileUtil.OpenReadOnly(FileName))
            {
                return (f.Length > position);
            }
        }

        public bool FileNameMatch(string otherFileName) 
        {
            return !string.IsNullOrEmpty(otherFileName) && Path.GetFullPath(otherFileName).Equals(Path.GetFullPath(this.FileName), 
                StringComparison.InvariantCultureIgnoreCase);
        }
        public bool FileNameInFolder(string folder) 
        {
            return System.IO.Path.GetFullPath(folder).Equals(System.IO.Path.GetFullPath(FileName),
                StringComparison.InvariantCultureIgnoreCase);
        }
        public void ResetPosition()
        {
            this.position = 0;
        }

        public bool Exists() 
        {
            return System.IO.File.Exists(FileName);
        }
    }
}