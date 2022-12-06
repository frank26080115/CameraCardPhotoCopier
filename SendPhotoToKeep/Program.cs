using System;
using System.IO;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    try
                    {
                        string fpath = Path.GetFullPath(arg);
                        if (File.Exists(fpath) == false)
                        {
                            continue;
                        }
                        string? dir = Path.GetDirectoryName(fpath);
                        if (dir == null)
                        {
                            continue;
                        }

                        string keepdir;
                        string keepdir1 = Path.Combine(dir, "good");
                        string keepdir2 = Path.Combine(dir, "keep");
                        if (Directory.Exists(keepdir1))
                        {
                            keepdir = keepdir1;
                        }
                        else if (Directory.Exists(keepdir2))
                        {
                            keepdir = keepdir2;
                        }
                        else
                        {
                            keepdir = keepdir1;
                            Directory.CreateDirectory(keepdir);
                        }
                        string justname = Path.GetFileName(fpath);
                        string destpath = Path.Combine(keepdir, justname);
                        File.Move(fpath, destpath);
                        if (justname.EndsWith(".JPG"))
                        {
                            string nname = fpath.Substring(0, fpath.Length - 4) + ".ARW";
                            if (File.Exists(nname))
                            {
                                justname = Path.GetFileName(nname);
                                destpath = Path.Combine(keepdir, justname);
                                File.Move(nname, destpath);
                            }
                        }
                        else if (justname.EndsWith(".ARW"))
                        {
                            string nname = fpath.Substring(0, fpath.Length - 4) + ".JPG";
                            if (File.Exists(nname))
                            {
                                justname = Path.GetFileName(nname);
                                destpath = Path.Combine(keepdir, justname);
                                File.Move(nname, destpath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Console.WriteLine();
                        Console.WriteLine("Press Any Key to continue");
                        Console.ReadKey();
                    }
                }
            }
            else
            {
                Console.WriteLine("Hello World!");
            }
        }
    }
}