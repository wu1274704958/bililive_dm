using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BililiveDebugPlugin.InteractionGame
{
//    {
//    "Title": "aaaa",
//    "Items" : [
//        {
//            "Name" : "aaa",
//            "Score" : 16,
//            "Icon" :  "asdasd"
//        }
//    ]
//  }
    public class SM_SendMsg
    {
        readonly static string MUTEX_NAME = "SM_Mutex";
        readonly static string FILE_MAP_NAME = "FM_RANK_MSG";
        readonly static uint MAX_MSG_SIZE = 4096 * 4;

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll", EntryPoint = "GetLastError")]
        public static extern int GetLastError();
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateMutex(IntPtr lpMutexAttributes,Int32 bInitialOwner,string lpName);
        //public struct _SECURITY_ATTRIBUTES
        //{
        //    uint nLength;
        //    UInt32 lpSecurityDescriptor;
        //    Int32 bInheritHandle;
        //}
        [DllImport("kernel32.dll")]
        public static extern Int32 ReleaseMutex(IntPtr hMutex);


        private static readonly int INVALID_HANDLE_VALUE = -1;
        private static readonly int ERROR_ALREADY_EXISTS = 0xB7;//183
        private static readonly uint PAGE_READWRITE = 0x04;
        private static readonly uint FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;
        private static readonly int FILE_MAP_READ = 0x0004;
        private static readonly int FILE_MAP_WRITE = 0x0002;
        private static readonly IntPtr NULL = IntPtr.Zero;

        public static int SendMessage(string msg)
        {
            IntPtr hMutex = IntPtr.Zero;
            IntPtr hFileMapping = IntPtr.Zero;
            IntPtr lpShareMemory = IntPtr.Zero;
            IntPtr hServerWriteOver = IntPtr.Zero;
            IntPtr hClientReadOver = IntPtr.Zero;

            int ret = 0;
            hFileMapping = CreateFileMapping(INVALID_HANDLE_VALUE,
                NULL,
                PAGE_READWRITE,
                0,
                MAX_MSG_SIZE,
                FILE_MAP_NAME);
            if (NULL == hFileMapping)
            {
                ret = GetLastError();
                goto END;
            }

            lpShareMemory = MapViewOfFile(hFileMapping,
                FILE_MAP_ALL_ACCESS,
                0,
                0,      //memory start address  
                MAX_MSG_SIZE);     //all memory space  
            if (NULL == lpShareMemory)
            {
                ret = GetLastError();
                goto END;
            }
            hMutex = CreateMutex(NULL, 0, MUTEX_NAME);
            if (NULL == hMutex || ERROR_ALREADY_EXISTS == GetLastError())
            {
                ret = GetLastError();
                goto END;
            }//多个线程互斥访问  


        END:
            if (NULL != hServerWriteOver) CloseHandle(hServerWriteOver);
            if (NULL != hClientReadOver) CloseHandle(hClientReadOver);
            if (NULL != lpShareMemory) UnmapViewOfFile(lpShareMemory);
            if (NULL != hFileMapping) CloseHandle(hFileMapping);
            if (NULL != hMutex) ReleaseMutex(hMutex);
            return ret;
        }

    }
}
