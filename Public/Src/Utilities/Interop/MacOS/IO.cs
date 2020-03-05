// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

using static BuildXL.Interop.Dispatch;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace BuildXL.Interop.MacOS
{
    /// <summary>
    /// The IO class offers interop calls for I/O based tasks into operating system facilities
    /// </summary>
    public static class IO
    {

        /// <summary>
        /// Errorno codes
        /// </summary>
        public enum Errno : int
        {
            EPERM = 1, // Operation not permitted
            ENOENT = 2, // No such file or directory
            ESRCH = 3, // No such process
            EINTR = 4, // Interrupted system call
            EIO = 5, // I/O error
            ENXIO = 6, // No such device or address
            E2BIG = 7, // Arg list too long
            ENOEXEC = 8, // Exec format error
            EBADF = 9, // Bad file number
            ECHILD = 10, // No child processes
            EDEADLK = 11, // Try again
            ENOMEM = 12, // Out of memory
            EACCES = 13, // Permission denied
            EFAULT = 14, // Bad address
            ENOTBLK = 15, // Block device required
            EBUSY = 16, // Device or resource busy
            EEXIST = 17, // File exists
            EXDEV = 18, // Cross-device link
            ENODEV = 19, // No such device
            ENOTDIR = 20, // Not a directory
            EISDIR = 21, // Is a directory
            EINVAL = 22, // Invalid argument
            ENFILE = 23, // File table overflow
            EMFILE = 24, // Too many open files
            ENOTTY = 25, // Not a typewriter
            ETXTBSY = 26, // Text file busy
            EFBIG = 27, // File too large
            ENOSPC = 28, // No space left on device
            ESPIPE = 29, // Illegal seek
            EROFS = 30, // Read-only file system
            EMLINK = 31, // Too many links
            EPIPE = 32, // Broken pipe
            EDOM = 33, // Math argument out of domain of func
            ERANGE = 34, // Math result not representable
            EAGAIN = 35, // Resource temporarily unavailable
            ELOOP = 62, // Too many levels of symbolic links
        }

        public static readonly DateTime UnixEpoch = new DateTime(year: 1970, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);

        [StructLayout(LayoutKind.Sequential)]
        public struct Timespec
        {
            public long Tv_sec;
            public long Tv_nsec;

            public static long SecToNSec(long sec) => sec * 1000000000;

            public DateTime ToUtcTime()
            {
                return UnixEpoch.AddTicks((SecToNSec(Tv_sec) + Tv_nsec) / 100);
            }

            private static DateTime ConvertTimeToDateTimeKindUtc(DateTime time)
            {
                if (time.Kind == DateTimeKind.Unspecified)
                {
                    throw new ArgumentException("DateTimeKind.Unspecified is not supported. Use Local or Utc times.", "time");
                }

                if (time.Kind == DateTimeKind.Local)
                {
                    time = time.ToUniversalTime();
                }

                return time;
            }

            public static Timespec CreateFromUtcDateTime(DateTime time)
            {
                var utcTime = ConvertTimeToDateTimeKindUtc(time);
                return new Timespec()
                {
                    Tv_sec = (long)(utcTime - UnixEpoch).TotalSeconds
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct StatBuffer
        {
            public int DeviceID;
            public ulong InodeNumber;
            public ushort Mode;
            public ushort HardLinks;
            public uint UserID;
            public uint GroupID;
            public long Size;
            public long TimeLastAccess;
            public long TimeNSecLastAccess;
            public long TimeLastModification;
            public long TimeNSecLastModification;
            public long TimeLastStatusChange;
            public long TimeNSecLastStatusChange;
            public long TimeCreation;
            public long TimeNSecCreation;

            public DateTime GetLastAccessUtcTime() => ToUtcDateTime(TimeLastAccess, TimeNSecLastAccess);
            public DateTime GetLastModificationUtcTime() => ToUtcDateTime(TimeLastModification, TimeNSecLastModification);
            public DateTime GetLastStatusChangeUtcTime() => ToUtcDateTime(TimeLastStatusChange, TimeNSecLastStatusChange);
            public DateTime GetCreationUtcTime() => ToUtcDateTime(TimeCreation, TimeNSecCreation);

            public DateTime ToUtcDateTime(long sec, long nsec) => new Timespec { Tv_sec = sec, Tv_nsec = nsec }.ToUtcTime();
        }

        /// <summary>
        /// C# representation of the native struct statfs64
        ///
        /// CODESYNC: https://github.com/apple/darwin-xnu/blob/master/bsd/sys/mount.h#L103-L120
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct StatFsBuffer
        {
            /// <summary>fundamental file system block size</summary>
            public uint f_bsize;

            /// <summary>optimal transfer block size</summary>
            public int f_iosize;

            /// <summary>total data blocks in file system</summary>
            public ulong f_blocks;

            /// <summary>free blocks in fs</summary>
            public ulong f_bfree;

            /// <summary>free blocks avail to non-superuser</summary>
            public ulong f_bavail;

            /// <summary>total file nodes in file system</summary>
            public ulong f_files;

            /// <summary>free file nodes in fs</summary>
            public ulong f_ffree;

            /// <summary>file system id</summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=2)]
            public int[] f_fsid;

            /// <summary>user that mounted the filesystem</summary>
            public int f_owner;

            /// <summary>type of filesystem</summary>
            public uint f_type;

            /// <summary>copy of mount exported flags</summary>
            public uint f_flags;

            /// <summary>fs sub-type (flavor)</summary>
            public uint f_fssubtype;

            /// <summary>fs type name</summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=16)]
            public string f_fstypename;

            /// <summary>directory on which mounted</summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=Constants.MaxPathLength)]
            public string f_mntonname;

            /// <summary>mounted filesystem</summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=Constants.MaxPathLength)]
            public string f_mntfromname;

            /// <summary>For future use</summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst=8)]
            public uint[] f_reserved;
        }

        public enum FilePermissions : int
        {
            S_ISUID = 0x0800, // Set user ID on execution
            S_ISGID = 0x0400, // Set group ID on execution
            S_ISVTX = 0x0200, // Save swapped text after use (sticky).
            S_IRUSR = 0x0100, // Read by owner
            S_IWUSR = 0x0080, // Write by owner
            S_IXUSR = 0x0040, // Execute by owner
            S_IRGRP = 0x0020, // Read by group
            S_IWGRP = 0x0010, // Write by group
            S_IXGRP = 0x0008, // Execute by group
            S_IROTH = 0x0004, // Read by other
            S_IWOTH = 0x0002, // Write by other
            S_IXOTH = 0x0001, // Execute by other

            S_IRWXG = (S_IRGRP | S_IWGRP | S_IXGRP),
            S_IRWXU = (S_IRUSR | S_IWUSR | S_IXUSR),
            S_IRWXO = (S_IROTH | S_IWOTH | S_IXOTH),

            ACCESSPERMS = (S_IRWXU | S_IRWXG | S_IRWXO), // 0777
            ALLPERMS = (S_ISUID | S_ISGID | S_ISVTX | S_IRWXU | S_IRWXG | S_IRWXO), // 07777
            DEFFILEMODE = (S_IRUSR | S_IWUSR | S_IRGRP | S_IWGRP | S_IROTH | S_IWOTH), // 0666

            S_IFMT = 0xF000, // Bits which determine file type
            S_IFDIR = 0x4000, // Directory
            S_IFCHR = 0x2000, // Character device
            S_IFBLK = 0x6000, // Block device
            S_IFREG = 0x8000, // Regular file
            S_IFIFO = 0x1000, // FIFO
            S_IFLNK = 0xA000, // Symbolic link
            S_IFSOCK = 0xC000, // Socket
        }

        /// <summary>
        /// Implements the standard unix 'stat' command. 
        /// </summary>
        /// <returns>
        /// Upon successful completion a value of 0 is returned and the result is stored in <paramref name="statBuf"/>; 
        /// otherwise, a value of -1 is returned and <see cref="Marshal.GetLastWin32Error"/> is set to indicate the error.
        /// </returns>
        public static int StatFile(string path, bool followSymlink, ref StatBuffer statBuf) => IsMacOS
            ? Impl_Mac.StatFile(path, followSymlink, ref statBuf)
            : Impl_Linux.StatFile(path, followSymlink, ref statBuf);

        /// <summary>
        /// Same as <see cref="StatFile" /> except that the target file is given as a file descriptor (<paramref name="fd" />).
        /// </summary>
        public static int StatFileDescriptor(SafeFileHandle fd, ref StatBuffer statBuf) => IsMacOS
            ? Impl_Mac.StatFileDescriptor(fd, ref statBuf)
            : Impl_Linux.StatFileDescriptor(fd, ref statBuf);

        /// <summary>
        /// Gets the name (e.g., "EXT4", "APFS", etc.) of the filesystem on which file <paramref name="fd" /> resides.
        /// </summary>
        /// <returns>
        /// There result (filesystem name) is stored in the <paramref name="fsTypeName"/> buffer.
        /// The return value is the length of that buffer or -1 upon error.
        /// </returns>
        public static int GetFileSystemType(SafeFileHandle fd, StringBuilder fsTypeName, long bufferSize) => IsMacOS
            ? Impl_Mac.GetFileSystemType(fd, fsTypeName, bufferSize)
            : Impl_Linux.GetFileSystemType(fd, fsTypeName, bufferSize);

        /// <summary>
        /// This routine returns information about a mounted file system.
        /// The <paramref name="path"/> argument is the path name of any file or directory
        /// within the mounted file system.  The <paramref name="buf"/> argument is a pointer
        /// to a <see cref="StatFsBuffer"/> structure.
        /// </summary>
        /// <returns>
        /// 0 on success, -1 otherwise.
        /// </returns>
        [DllImport(Libraries.LibC, SetLastError = true, EntryPoint = "statfs64")]
        public static extern int StatFs([MarshalAs(UnmanagedType.LPStr)] string path, ref StatFsBuffer buf);

        /// <summary>
        /// Returns the root mount for a given path or <c>null</c> in case of an error.
        /// </summary>
        /// <param name="path">Path name of any file or directory within the mounted file system</param>
        /// <returns>String containing the mount name</returns>
        public static string GetMountNameForPath(string path)
        {
            var statFsBuffer = new StatFsBuffer();
            var error = StatFs(path, ref statFsBuffer);
            if (error != 0)
            {
                return null;
            }

            return statFsBuffer.f_mntonname;
        }

        /// <summary>
        /// Sets the creation, modification, change and access time of a file specified at path
        /// </summary>
        /// <returns>Returns zero in case of success, otherwise error</returns>
        public static int SetTimeStampsForFilePath(string path, bool followSymlink, StatBuffer buffer) => IsMacOS
            ? Impl_Mac.SetTimeStampsForFilePath(path, followSymlink, buffer)
            : Impl_Linux.SetTimeStampsForFilePath(path, followSymlink, buffer);

        /// <summary>
        /// Sets atime and mtime to current time.
        /// </summary>
        public static int Touch(string pathname, bool followSymlink)
        {
            var timespec = Timespec.CreateFromUtcDateTime(DateTime.UtcNow);
            var buf = new StatBuffer
            {
                TimeLastAccess = timespec.Tv_sec,
                TimeNSecLastAccess = timespec.Tv_nsec,
                TimeLastModification = timespec.Tv_sec,
                TimeNSecLastModification = timespec.Tv_nsec,
            };
            return SetTimeStampsForFilePath(pathname, followSymlink, buf);
        }

        /// <summary>
        /// Read the value of a symbolic link specified by <paramref name="link"/>
        /// Returns number of bytes placed in buf, and -1 otherwise.
        /// </summary>
        public static long SafeReadLink(string link, StringBuilder buffer, long length) => IsMacOS
            ? Impl_Mac.SafeReadLink(link, buffer, length)
            : Impl_Linux.SafeReadLink(link, buffer, length);

        /// <summary>
        /// Gets the file permissions flag for the entry at <paramref name="path"/>
        /// </summary>
        /// <returns>Returns zero in case of success, otherwise error</returns>
        public static int GetFilePermissionsForFilePath(string path, bool followSymlink = true) => IsMacOS
            ? Impl_Mac.GetFilePermissionsForFilePath(path, followSymlink)
            : Impl_Linux.GetFilePermissionsForFilePath(path, followSymlink);

        /// <summary>
        /// Sets the file permissions flag for the entry at <paramref name="path"/>
        /// </summary>
        public static int SetFilePermissionsForFilePath(string path, FilePermissions permissions, bool followSymlink = true) => IsMacOS
            ? Impl_Mac.SetFilePermissionsForFilePath(path, permissions, followSymlink)
            : Impl_Linux.SetFilePermissionsForFilePath(path, permissions, followSymlink);

        /// <summary>
        /// Opens a file at a specified path.
        /// </summary>
        public static SafeFileHandle Open(string pathname, OpenFlags flags, FilePermissions permission) => IsMacOS
            ? Impl_Mac.Open(pathname, flags, permission)
            : Impl_Linux.Open(pathname, flags, permission);

        /// <summary>
        /// Creates a symbolic link at <paramref name="symlinkFilePath"/> pointing to <paramref name="target"/>.
        /// Returns 0 upon successful completion, and -1 otherwise.
        /// </summary>
        public static int symlink(string target, string symlinkFilePath)
            => Impl_Common.symlink(target, symlinkFilePath);

        /// <summary>
        /// Creates a new hardlink for file / directory specified by <paramref name="link"/> at <paramref name="hardlinkFilePath"/>.
        /// Returns 0 upon successful completion, and -1 otherwise.
        /// </summary>
        public static int link(string link, string hardlinkFilePath)
            => Impl_Common.link(link, hardlinkFilePath);

        /// <summary>
        /// Flags for <see cref="Open"/>
        /// </summary>
        [Flags]
        public enum OpenFlags : int
        {
            O_RDONLY = 0x0000,    // open for reading only
            O_WRONLY = 0x0001,    // open for writing only
            O_RDWR = 0x0002,      // open for reading and writing
            O_NONBLOCK = 0x0004,  // do not block on open or for data to become available
            O_APPEND = 0x0008,    // append on each write
            O_CREAT = 0x0200,     // create file if it does not exist
            O_TRUNC = 0x0400,     // truncate size to 0
            O_EXCL = 0x0800,      // error if O_CREAT and the file exists
            O_SHLOCK = 0x0010,    // atomically obtain a shared lock
            O_EXLOCK = 0x0020,    // atomically obtain an exclusive lock
            O_NOFOLLOW = 0x0100,  // do not follow symlinks
            O_SYMLINK = 0x200000, // allow open of symlinks
            O_EVTONLY = 0x8000,   // descriptor requested for event notifications only
            O_CLOEXEC = 0x1000000 // mark as close-on-exec
        }

        /// <summary>
        /// Flags for <see cref="CloneFile(string, string, CloneFileFlags)"/>.
        /// </summary>
        [Flags]
        public enum CloneFileFlags : int
        {
            CLONE_NONE        = 0x0000, // No flag.
            CLONE_NOFOLLOW    = 0x0001, // Don't follow symbolic links.
            CLONE_NOOWNERCOPY = 0x0002, // Don't copy ownership information from source.
        }

        /// <summary>
        /// Creates a copy on write clones of files.
        /// </summary>
        [DllImport(Libraries.LibC, SetLastError = true, EntryPoint = "clonefile")]
        public static extern int CloneFile(string source, string destination, CloneFileFlags flags);

        private static readonly string s_user = Environment.GetEnvironmentVariable("USER") ?? string.Empty;

        public const string AppleInternal             = "/AppleInternal";
        public const string Applications              = "/Applications";
        public const string Bin                       = "/bin";
        public const string BinBash                   = "/bin/bash";
        public const string BinSh                     = "/bin/sh";
        public const string Dev                       = "/dev";
        public const string Etc                       = "/etc";
        public const string EtcMasterPasswd           = "/etc/master.passwd";
        public const string Library                   = "/Library";
        public const string LibraryPreferencesLogging = "/Library/Preferences/Logging";
        public const string Private                   = "/private";
        public const string PrivateVar                = "/private/var";
        public const string Proc                      = "/proc";
        public const string Sbin                      = "/sbin";
        public const string System                    = "/System";
        public const string SystemLibrary             = "/System/Library";
        public const string TmpDir                    = "/tmp";
        public const string Usr                       = "/usr";
        public const string UsrBin                    = "/usr/bin";
        public const string UsrInclude                = "/usr/include";
        public const string UsrLib                    = "/usr/lib";
        public const string UsrLibexec                = "/usr/libexec";
        public const string UsrShare                  = "/usr/share";
        public const string UsrStandalone             = "/usr/standalone";
        public const string UsrSbin                   = "/usr/sbin";
        public const string Var                       = "/var";

        public static readonly string UserProvisioning      = $"/Users/{s_user}/Library/MobileDevice/Provisioning Profiles";
        public static readonly string UserKeyChainsDb       = $"/Users/{s_user}/Library/Keychains/login.keychain-db";
        public static readonly string UserKeyChains         = $"/Users/{s_user}/Library/Keychains/login.keychain";
        public static readonly string UserCFTextEncoding    = $"/Users/{s_user}/.CFUserTextEncoding";
        public static readonly string UserPreferences       = $"/Users/{s_user}/Library/Preferences";

    }
}
#pragma warning restore CS1591
