using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using ProtoBuf;
using System.Reflection;
using Utils;
using InteractionGame;

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
        readonly static uint MAX_MSG_SIZE = 4096 * 100;

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
        private ConcurrentQueue<KeyValuePair<short,byte[]>> MsgQueue = new ConcurrentQueue<KeyValuePair<short, byte[]>>();
        private byte[] TmpBuf = new byte[MAX_MSG_SIZE - 3];
        private MemoryStream m_TmpStream;// = new MemoryStream();

        public bool Init()
        {
            //Assembly ass = Assembly.LoadFrom("System.Runtime.CompilerServices.Unsafe.dll");
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
        
        public int SendMessage(short id,byte[] msg,bool pushQueue = true)
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

            if (msg != null && msg.Length > MAX_MSG_SIZE + 5)
            {
                Locator.Instance.Get<DebugPlugin>().Log($"Send Message to big id = {id} len = {msg.Length}");
                return -3;
            }
            switch(Marshal.ReadByte(lpShareMemory))
            {
                case 0:
                    Marshal.WriteByte(lpShareMemory, 1);
                    Marshal.WriteInt16(lpShareMemory + 1, id);
                    Marshal.WriteInt16(lpShareMemory + 3, (short)(msg?.Length ?? 0));
                    if(msg != null && msg.Length > 0)
                        Marshal.Copy(msg, 0, lpShareMemory + 5, msg.Length);
                    //CopyMemory(lpShareMemory + 3, init, (uint)len);
                    break;
                default:
                    ret = -2;
                    if(pushQueue)MsgQueue.Enqueue(new KeyValuePair<short, byte[]>(id, msg));
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
        public void waitClean()
        {
            while(MsgQueue.TryPeek(out var it))
            {
                var ret = SendMessage(it.Key, it.Value, false);
                if (ret == 0)
                    MsgQueue.TryDequeue(out _);
            }
        }

        public void SendMsg<T>(short id, T obj)
            where T:class
        {
            //try
            //{
                if(obj == null)
                {
                    SendMessage(id, null);
                    return;
                }
                m_TmpStream = new MemoryStream();
                Serializer.SerializeWithLengthPrefix(m_TmpStream, obj,PrefixStyle.Fixed32BigEndian);
                var msg = m_TmpStream.ToArray();
                var ret = SendMessage(id, msg);
                System.Diagnostics.Debug.Assert(ret == 0 || ret == -2);
            //}
            //catch (Exception e) { 
            //    Locator.Instance.Get<IContext>().Log($"SendMsg error: {e.Message}");
            //}
        }

        private static int strlen(IntPtr init)
        {
            int len = 0;
            while (Marshal.ReadInt16(init) != 0)
            {
                len += 2;
                init += 2;
            }
            return len;
        }

    }
}
