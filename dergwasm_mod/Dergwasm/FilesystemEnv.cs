using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Derg
{
    // Provides the filesystem environment for the WASM VM. These functions are defined by
    // Emscripten.
    //
    // The filesystem for Resonite starts with the fsRoot slot, which is passed in the
    // constructor. This represents the root of the filesystem ("/"). Children of this
    // slot are either directories or files. Both are slots, but files additionally
    // have a ValueField<string> component attached to them, which contains the contents
    // of the file.
    //
    // Directory slots can further have directory or file children.
    //
    // Except for the root slot, the name of the file or directory is the name of its slot.
    //
    // The code acts so that the parent of the root slot is the root slot itself.
    public class FilesystemEnv
    {
        public Machine machine;
        public Slot fsRoot;
        public EmscriptenEnv env;
        public EmscriptenWasi wasi;
        string cwd = "/";

        public FilesystemEnv(
            Machine machine,
            Slot fsRootSlot,
            EmscriptenEnv emscriptenEnv,
            EmscriptenWasi wasi
        )
        {
            this.machine = machine;
            this.fsRoot = fsRootSlot;
            this.env = emscriptenEnv;
            this.wasi = wasi;
        }

        // Functions callable from WASM.
        public void RegisterHostFuncs()
        {
            machine.RegisterReturningHostFunc<int, int>("env", "__syscall_chdir", __syscall_chdir);
            machine.RegisterReturningHostFunc<int, int>("env", "__syscall_rmdir", __syscall_rmdir);
            machine.RegisterReturningHostFunc<int, int, int>(
                "env",
                "__syscall_getcwd",
                __syscall_getcwd
            );
            machine.RegisterReturningHostFunc<int, int, int, int>(
                "env",
                "__syscall_mkdirat",
                __syscall_mkdirat
            );
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "env",
                "__syscall_openat",
                __syscall_openat
            );
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "env",
                "__syscall_renameat",
                __syscall_renameat
            );
            machine.RegisterReturningHostFunc<int, int, int, int>(
                "env",
                "__syscall_unlinkat",
                __syscall_unlinkat
            );
            machine.RegisterReturningHostFunc<int, int, int, int, int>(
                "env",
                "__syscall_newfstatat",
                __syscall_newfstatat
            );
            machine.RegisterReturningHostFunc<int, int, int, int>(
                "env",
                "__syscall_poll",
                __syscall_poll
            );
            machine.RegisterReturningHostFunc<int, int, int, int>(
                "env",
                "__syscall_getdents64",
                __syscall_getdents64
            );
            machine.RegisterReturningHostFunc<int, int, int>(
                "env",
                "__syscall_fstat64",
                __syscall_fstat64
            );
            machine.RegisterReturningHostFunc<int, int, int>(
                "env",
                "__syscall_stat64",
                __syscall_stat64
            );
            machine.RegisterReturningHostFunc<int, int, int>(
                "env",
                "__syscall_lstat64",
                __syscall_lstat64
            );
            machine.RegisterReturningHostFunc<int, int, int, int>(
                "env",
                "__syscall_statfs64",
                __syscall_statfs64
            );
        }

        // From emscripten/system/lib/libc/musl/include/fcntl.h
        static class StatConst
        {
            public const int AT_FDCWD = -100;
            public const int AT_SYMLINK_NOFOLLOW = 0x100;
            public const int AT_REMOVEDIR = 0x200;
            public const int AT_SYMLINK_FOLLOW = 0x400;
            public const int AT_NO_AUTOMOUNT = 0x800;
            public const int AT_EMPTY_PATH = 0x1000;
            public const int AT_STATX_SYNC_TYPE = 0x6000;
            public const int AT_STATX_SYNC_AS_STAT = 0x0000;
            public const int AT_STATX_FORCE_SYNC = 0x2000;
            public const int AT_STATX_DONT_SYNC = 0x4000;
            public const int AT_RECURSIVE = 0x8000;
        }

        // A struct returned by stat. 96 bytes.
        //
        // The layout is based on emscripten/src/generated_struct_info32.json.
        [StructLayout(LayoutKind.Sequential)]
        public struct Stat
        {
            // ID of device containing the file.
            public int st_dev;

            // File type and mode.
            public int st_mode;

            // Number of hard links.
            public int st_nlink;

            // User ID of owner.
            public int st_uid;

            // Group ID of owner.
            public int st_gid;

            // Device ID (if special file).
            public int st_rdev;

            // Total size, in bytes. If this is a symbolic link (which we don't support)
            // then it's the length of the pathname it contains, without a terminating
            // NUL byte.
            //
            // Although this is a ulong, we only use files to store strings, and .NET can
            // only store strings up to 2GB in size.
            public ulong st_size;

            // The preferred block size for efficient filesystem I/O.
            public int st_blksize;

            // Number of 512-byte blocks allocated. For Resonite, this is
            // just ceil(st_size/512).
            public int st_blocks;

            // Time of last access, seconds part.
            public long st_atime_sec;

            // Time of last access, nanoseconds part.
            public long st_atime_nsec;

            // Time of last modification, seconds part.
            public long st_mtime_sec;

            // Time of last modification, nanoseconds part.
            public long st_mtime_nsec;

            // Time of last status change, seconds part.
            public long st_ctime_sec;

            // Time of last status change, nanoseconds part.
            public long st_ctime_nsec;

            // Inode number.
            public long st_ino;
        }

        // A directory entry struct. 280 bytes.
        //
        // The layout is based on emscripten/src/generated_struct_info32.json.
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct Dirent
        {
            // Inode number.
            [FieldOffset(0)]
            public ulong d_ino;

            // This is an opaque value, not an offset.
            // See https://man7.org/linux/man-pages/man3/readdir.3.html.
            [FieldOffset(8)]
            public ulong d_off;

            // Length of this record = namesize + 19
            [FieldOffset(16)]
            public ushort d_reclen;

            // The type of the file:
            // DT_DIR (4) = directory
            // DT_REG (8) = regular file
            [FieldOffset(18)]
            public byte d_type;

            // The filename, NUL-terminated.
            [FieldOffset(19)]
            public fixed byte d_name[256];
        }

        public static class OpenFlags
        {
            public const int O_RDONLY = 0;
            public const int O_WRONLY = 1;
            public const int O_RDWR = 2;
            public const int O_CREAT = 0100;
            public const int O_EXCL = 0200;
            public const int O_TRUNC = 01000;
            public const int O_APPEND = 02000;
            public const int O_DIRECTORY = 0200000;
        }

        // Calculate the path relative to the path for the given directory file descriptor.
        // If the dirfd is AT_FDCWD, then the path is relative to the cwd.
        string calculateAt(int dirfd, string path, bool allowEmpty = false)
        {
            if (path.StartsWith("/")) // It's an absolute path.
                return path;

            string dir;
            if (dirfd == StatConst.AT_FDCWD)
            {
                dir = cwd;
            }
            else
            {
                if (!wasi.streams.ContainsKey(dirfd))
                {
                    DergwasmMachine.Msg($"calculateAt: invalid dirfd: {dirfd}");
                    return null;
                }
                dir = wasi.streams[dirfd].path;
            }

            if (path.Length == 0)
            {
                if (!allowEmpty)
                {
                    DergwasmMachine.Msg("calculateAt: empty path");
                    return null;
                }
                return dir;
            }

            if (dir == "/")
            {
                return dir + path;
            }
            return dir + "/" + path;
        }

        bool slot_is_regular_file(Slot slot)
        {
            return slot.GetComponent<ValueField<string>>() != null;
        }

        int chdir_absolute(string path)
        {
            if (path == "/")
            {
                cwd = "/";
                return 0;
            }
            if (path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }

            List<string> normalized_elements = new List<string>();
            Slot slot = fsRoot;

            foreach (string element in path.Split('/'))
            {
                if (element == "")
                    continue;
                if (element == ".")
                    continue;
                if (element == "..")
                {
                    if (slot.Parent == slot)
                        continue;
                    slot = slot.Parent;
                    normalized_elements.RemoveAt(normalized_elements.Count - 1);
                    continue;
                }
                slot = slot.FindChild(element);
                if (slot == null)
                {
                    DergwasmMachine.Msg($"chdir_absolute: no such file or directory: {path}");
                    return -Errno.ENOENT;
                }
                normalized_elements.Add(element);
            }
            if (slot_is_regular_file(slot))
            {
                DergwasmMachine.Msg($"chdir_absolute: not a directory: {path}");
                return -Errno.ENOTDIR;
            }
            cwd = path;
            return 0;
        }

        int get_slot_for_absolute_path(string path, out Slot slot, out string normalized_path)
        {
            slot = fsRoot;
            normalized_path = "";
            List<string> normalized_elements = new List<string>();

            foreach (string element in path.Split('/'))
            {
                if (element == "")
                    continue;
                if (element == ".")
                    continue;
                if (element == "..")
                {
                    if (slot.Parent == slot)
                        continue;
                    slot = slot.Parent;
                    normalized_elements.RemoveAt(normalized_elements.Count - 1);
                    continue;
                }
                slot = slot.FindChild(element);
                if (slot == null)
                {
                    DergwasmMachine.Msg(
                        $"get_slot_for_absolute_path: no such file or directory: {path}"
                    );
                    return -Errno.ENOENT;
                }
                normalized_elements.Add(element);
            }
            normalized_path = "/" + string.Join("/", normalized_elements);
            return 0;
        }

        string basename(string path)
        {
            string[] elements = path.Split('/');
            return elements[elements.Length - 1];
        }

        string dirname(string path)
        {
            string[] elements = path.Split('/');
            if (elements.Length == 1)
                return "/";
            return string.Join("/", elements.Take(elements.Length - 1));
        }

        // Makes a directory or file slot at the given absolute path.
        int mknod(string path, bool as_file, out Slot slot)
        {
            slot = null;

            Slot parentSlot;
            int err = get_slot_for_absolute_path(dirname(path), out parentSlot, out _);
            if (err != 0)
                return err;

            string name = basename(path);
            if (name == "." || name == "..")
            {
                DergwasmMachine.Msg($"mknode: invalid name: {name}");
                return -Errno.EINVAL;
            }
            slot = parentSlot.AddSlot(name);

            if (as_file)
            {
                slot.AttachComponent<ValueField<string>>();
            }
            return 0;
        }

        // syscalls, from emscripten/src/library_syscall.js

        // Changes the current working directory of the WASM machine.
        public int __syscall_chdir(Frame frame, int pathPtr)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            DergwasmMachine.Msg($"__syscall_chdir: path={path}");
            if (path.StartsWith("/"))
            {
                return chdir_absolute(path);
            }
            return chdir_absolute(cwd + "/" + path);
        }

        public int __syscall_rmdir(Frame frame, int pathPtr)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            Slot slot;
            int err;
            if (path.StartsWith("/"))
            {
                err = get_slot_for_absolute_path(path, out slot, out _);
            }
            else
            {
                err = get_slot_for_absolute_path(cwd + "/" + path, out slot, out _);
            }
            if (err != 0)
                return err;
            if (slot.ReferenceID == fsRoot.ReferenceID)
            {
                DergwasmMachine.Msg($"__syscall_rmdir: cannot remove root directory");
                return -Errno.EACCES;
            }
            slot.Destroy();
            return 0;
        }

        // Gets the current working directory of the WASM machine, putting it into the
        // given buffer, which is of the given size.
        public int __syscall_getcwd(Frame frame, int buf, int size)
        {
            DergwasmMachine.Msg($"__syscall_getcwd: cwd={cwd}");
            if (size == 0)
                return -Errno.EINVAL;
            byte[] bytes = Encoding.UTF8.GetBytes(cwd);
            if (size < bytes.Length + 1)
                return -Errno.ERANGE;
            Array.Copy(bytes, 0, machine.Memory0, buf, bytes.Length);
            machine.Memory0[buf + bytes.Length] = 0;
            return 0;
        }

        public int __syscall_mkdirat(Frame frame, int dirfd, int pathPtr, int mode)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            path = calculateAt(dirfd, path);
            return mknod(path, false, out _);
        }

        // Opens the given file. The path is relative to the given directory file descriptor.
        //
        // We currently do not allow creation or writing of files, so the flags must not contain
        // O_CREAT, O_WRONLY, or O_RDWR.
        //
        // We also currently don't allow opening directories.
        //
        // Returns the opened file descriptor on success, or -ERRNO on failure.
        public int __syscall_openat(Frame frame, int dirfd, int pathPtr, int flags, int mode)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            path = calculateAt(dirfd, path);
            DergwasmMachine.Msg($"__syscall_openat: path={path}");

            Slot slot;
            string normalized_path;
            int err = get_slot_for_absolute_path(path, out slot, out normalized_path);
            if (err != 0)
                return err;

            int unsupported_mask =
                OpenFlags.O_CREAT | OpenFlags.O_WRONLY | OpenFlags.O_RDWR | OpenFlags.O_DIRECTORY;
            if ((flags & unsupported_mask) != 0)
            {
                string unsupported_flags = "";
                if ((flags & OpenFlags.O_CREAT) != 0)
                    unsupported_flags += "O_CREAT ";
                if ((flags & OpenFlags.O_WRONLY) != 0)
                    unsupported_flags += "O_WRONLY ";
                if ((flags & OpenFlags.O_RDWR) != 0)
                    unsupported_flags += "O_RDWR ";
                if ((flags & OpenFlags.O_DIRECTORY) != 0)
                    unsupported_flags += "O_DIRECTORY ";
                DergwasmMachine.Msg($"__syscall_openat: unsupported flags: {unsupported_flags}");
                return -Errno.EINVAL;
            }

            int fd = wasi.createStream(slot, normalized_path).fd;
            DergwasmMachine.Msg($"__syscall_openat: fd={fd}");
            return fd;
        }

        public int __syscall_renameat(
            Frame frame,
            int olddirfd,
            int oldpathPtr,
            int newdirfd,
            int newpathPtr
        ) => throw new NotImplementedException();

        public int __syscall_unlinkat(Frame frame, int dirfd, int pathPtr, int flags) =>
            throw new NotImplementedException();

        public int __syscall_newfstatat(Frame frame, int dirfd, int pathPtr, int buf, int flags)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            bool noFollow = (flags & StatConst.AT_SYMLINK_NOFOLLOW) != 0;
            bool allowEmpty = (flags & StatConst.AT_EMPTY_PATH) != 0;
            if (
                (
                    flags
                    & ~(
                        StatConst.AT_SYMLINK_NOFOLLOW
                        | StatConst.AT_EMPTY_PATH
                        | StatConst.AT_NO_AUTOMOUNT
                    )
                ) != 0
            )
            {
                DergwasmMachine.Msg($"__syscall_newfstatat: Unsupported flags: 0x{flags:X8}");
                return -Errno.EINVAL;
            }
            DergwasmMachine.Msg(
                $"__syscall_newfstatat: dirfd={dirfd}, path={path}, noFollow={noFollow}, allowEmpty={allowEmpty}"
            );
            throw new NotImplementedException();
        }

        public int __syscall_poll(Frame frame, int fdsPtr, int nfds, int timeout) =>
            throw new NotImplementedException();

        // Reads directory entries from the given directory file descriptor. The dirp buffer contains
        // `count` bytes. You can call this function multiple times to read all the entries.
        public int __syscall_getdents64(Frame frame, int fd, int dirp, int count) =>
            throw new NotImplementedException();

        public int __syscall_fstat64(Frame frame, int fd, int buf) =>
            throw new NotImplementedException();

        // Stats the given file. The buffer must be large enough to hold a Stat struct.
        public int __syscall_stat64(Frame frame, int pathPtr, int buf)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            DergwasmMachine.Msg($"__syscall_stat64: path={path}");
            Slot slot;
            int err = get_slot_for_absolute_path(path, out slot, out _);
            if (err != 0)
                return err;
            bool is_file = slot_is_regular_file(slot);
            Stat stat = new Stat();
            stat.st_dev = 0; // Always 0
            stat.st_mode = (is_file ? 0x8000 : 0x4000) | 0755; // rwxr-xr-x
            stat.st_nlink = 0; // Not supported
            stat.st_uid = 0; // Not supported
            stat.st_gid = 0; // Not supported
            stat.st_rdev = 0; // Not supported
            stat.st_size = 0;
            if (is_file)
            {
                string contents = slot.GetComponent<ValueField<string>>().Value;
                UTF8Encoding utf8 = new UTF8Encoding();
                stat.st_size = (ulong)utf8.GetByteCount(contents);
            }
            stat.st_blksize = 1024; // I guess?
            stat.st_blocks = 1; // Technicaly correct
            stat.st_atime_sec = 0; // Not supported
            stat.st_atime_nsec = 0; // Not supported
            stat.st_mtime_sec = 0; // Not supported
            stat.st_mtime_nsec = 0; // Not supported
            stat.st_ctime_sec = 0; // Not supported
            stat.st_ctime_nsec = 0; // Not supported
            stat.st_ino = 0; // Not supported

            IntPtr ptr = IntPtr.Zero;
            try
            {
                int size = Marshal.SizeOf(stat);
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(stat, ptr, true);
                Marshal.Copy(ptr, machine.Memory0, buf, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return 0;
        }

        public int __syscall_lstat64(Frame frame, int pathPtr, int buf)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            DergwasmMachine.Msg($"__syscall_lstat64: path={path}");
            throw new NotImplementedException();
        }

        public int __syscall_statfs64(Frame frame, int pathPtr, int size, int buf)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            DergwasmMachine.Msg($"__syscall_statfs64: path={path}, size={size}");
            throw new NotImplementedException();
        }
    }
}
