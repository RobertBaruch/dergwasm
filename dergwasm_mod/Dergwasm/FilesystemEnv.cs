using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Derg
{
    // Provides the filesystem environment for the WASM VM. These functions are defined by
    // Emscripten.
    public class FilesystemEnv
    {
        public Machine machine;
        public Slot fsRoot;
        public EmscriptenEnv env;

        public FilesystemEnv(Machine machine, Slot fsRootSlot, EmscriptenEnv emscriptenEnv)
        {
            this.machine = machine;
            this.fsRoot = fsRootSlot;
            this.env = emscriptenEnv;
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

        [StructLayout(LayoutKind.Explicit)]
        public struct Stat
        {
            [FieldOffset(0)]
            public int st_dev;

            [FieldOffset(4)]
            public int st_ino;

            [FieldOffset(8)]
            public int st_nlink;

            [FieldOffset(12)]
            public int st_mode;

            [FieldOffset(16)]
            public int st_uid;

            [FieldOffset(20)]
            public int st_gid;

            [FieldOffset(24)]
            public int st_rdev;

            [FieldOffset(28)]
            public int st_size;

            [FieldOffset(32)]
            public int st_blksize;

            [FieldOffset(36)]
            public int st_blocks;

            [FieldOffset(40)]
            public long st_atime_sec;

            [FieldOffset(48)]
            public int st_atime_nsec;

            [FieldOffset(52)]
            public long st_mtime_sec;

            [FieldOffset(60)]
            public int st_mtime_nsec;

            [FieldOffset(64)]
            public long st_ctime_sec;

            [FieldOffset(72)]
            public int st_ctime_nsec;
        }

        private void write_stat(uint statPtr, Stat stat)
        {
            machine.MemSet(statPtr + 0, stat.st_dev);
            machine.MemSet(statPtr + 4, stat.st_ino);
            machine.MemSet(statPtr + 8, stat.st_mode);
            machine.MemSet(statPtr + 12, stat.st_nlink);
            machine.MemSet(statPtr + 16, stat.st_uid);
            machine.MemSet(statPtr + 20, stat.st_gid);
            machine.MemSet(statPtr + 24, stat.st_rdev);
            machine.MemSet(statPtr + 28, stat.st_size);
            machine.MemSet(statPtr + 32, stat.st_blksize);
            machine.MemSet(statPtr + 36, stat.st_blocks);
            machine.MemSet(statPtr + 40, stat.st_atime_sec);
            machine.MemSet(statPtr + 48, stat.st_atime_nsec);
            machine.MemSet(statPtr + 52, stat.st_mtime_sec);
            machine.MemSet(statPtr + 60, stat.st_mtime_nsec);
            machine.MemSet(statPtr + 64, stat.st_ctime_sec);
            machine.MemSet(statPtr + 72, stat.st_ctime_nsec);
        }

        // syscalls, from emscripten/src/library_syscall.js

        // Changes the current working directory of the WASM machine.
        public int __syscall_chdir(Frame frame, int pathPtr) => throw new NotImplementedException();

        public int __syscall_rmdir(Frame frame, int pathPtr) => throw new NotImplementedException();

        public int __syscall_getcwd(Frame frame, int buf, int size) =>
            throw new NotImplementedException();

        public int __syscall_mkdirat(Frame frame, int dirfd, int pathPtr, int mode) =>
            throw new NotImplementedException();

        public int __syscall_openat(Frame frame, int dirfd, int pathPtr, int flags, int mode) =>
            throw new NotImplementedException();

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

        public int __syscall_getdents64(Frame frame, int fd, int dirp, int count) =>
            throw new NotImplementedException();

        public int __syscall_fstat64(Frame frame, int fd, int buf) =>
            throw new NotImplementedException();

        public int __syscall_stat64(Frame frame, int pathPtr, int buf)
        {
            string path = env.GetUTF8StringFromMem(pathPtr);
            DergwasmMachine.Msg($"__syscall_stat64: path={path}");
            return -Errno.ENOENT;
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
