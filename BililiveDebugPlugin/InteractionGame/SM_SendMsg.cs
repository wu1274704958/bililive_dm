﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);


        private static readonly int INVALID_HANDLE_VALUE = -1;
        private static readonly int ERROR_ALREADY_EXISTS = 0xB7;//183
        private static readonly uint PAGE_READWRITE = 0x04;
        private static readonly uint FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;
        private static readonly int FILE_MAP_READ = 0x0004;
        private static readonly int FILE_MAP_WRITE = 0x0002;
        private static readonly IntPtr NULL = IntPtr.Zero;

        private IntPtr hFileMapping = IntPtr.Zero;
        private ConcurrentQueue<KeyValuePair<short,string>> MsgQueue = new ConcurrentQueue<KeyValuePair<short, string>>();

        public bool Init()
        {
            hFileMapping = CreateFileMapping(INVALID_HANDLE_VALUE,
                NULL,
                PAGE_READWRITE,
                0,
                MAX_MSG_SIZE,
                FILE_MAP_NAME);
            if (NULL == hFileMapping)
            {
                return false;
            }
            ResetMem();
            return true;
        }

        private int ResetMem()
        {
            if (hFileMapping == IntPtr.Zero)
                return -1;
            int ret = 0;
            var lpShareMemory = MapViewOfFile(hFileMapping,
                FILE_MAP_ALL_ACCESS,
                0,
                0,      //memory start address  
                MAX_MSG_SIZE);     //all memory space  
            if (NULL == lpShareMemory)
            {
                ret = GetLastError();
                goto END;
            }
            byte[] mem = new byte[MAX_MSG_SIZE];
            for(int i = 0; i < mem.Length; i++)
                mem[i] = 0;
            Marshal.Copy(mem,0,lpShareMemory,(int)MAX_MSG_SIZE);
            END:
            if (NULL != lpShareMemory) UnmapViewOfFile(lpShareMemory);
            return ret;
        }

        public void Dispose()
        {
            if (NULL != hFileMapping) CloseHandle(hFileMapping);
        }

        public int SendMessage(short id,string msg,bool pushQueue = true)
        {
            if(hFileMapping == IntPtr.Zero)
                return -1;
            int ret = 0;
            var lpShareMemory = MapViewOfFile(hFileMapping,
                FILE_MAP_ALL_ACCESS,
                0,
                0,      //memory start address  
                MAX_MSG_SIZE);     //all memory space  
            if (NULL == lpShareMemory)
            {
                ret = GetLastError();
                goto END;
            }
            switch(Marshal.ReadByte(lpShareMemory))
            {
                case 0:
                    Marshal.WriteByte(lpShareMemory, 1);
                    Marshal.WriteInt16(lpShareMemory + 1, id);
                    IntPtr init = Marshal.StringToHGlobalAnsi(msg);
                    uint len = (uint)strlen(init) + 1;
                    CopyMemory(lpShareMemory + 3, init, len);
                    Marshal.FreeHGlobal(init);
                    break;
                default:
                    ret = -2;
                    if(pushQueue)MsgQueue.Enqueue(new KeyValuePair<short, string>(id, msg));
                    goto END;
                    break;
            }
            END:
            if (NULL != lpShareMemory) UnmapViewOfFile(lpShareMemory);
           
            return ret;
        }

        public void flush()
        {
            if (MsgQueue.Count == 0) return;
            if(MsgQueue.TryPeek(out var it))
            {
                var ret = SendMessage(it.Key, it.Value,false);
                if(ret == 0)
                    MsgQueue.TryDequeue(out _);
            }
        }

        public void SendMsg(short id, object msg)
        {
            try
            {
                var str = JsonConvert.SerializeObject(msg);
                SendMessage(id, str);
            }catch (Exception ex) { 
                
            }
        }

        private static int strlen(IntPtr init)
        {
            int len = 0;
            while (Marshal.ReadByte(init) != 0)
            {
                len += 1;
                init += 1;
            }
            return len;
        }

    }
}
