using InteractionGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils;

namespace BililiveDebugPlugin.Utils
{
    class LocalMemComm
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        protected delegate void ErrorCallbackFunction(int level, IntPtr message);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        protected delegate void RecvCallbackFunction(IntPtr message);

        [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        protected static extern void LMC_init(IntPtr mem_id, UInt32 size, ErrorCallbackFunction callback);
        [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        protected static extern void LMC_send(IntPtr msg);
        [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int LMC_tick();
        [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int LMC_has_unsend();
        [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void LMC_release();
        [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        protected static extern void LMC_pop_recv(RecvCallbackFunction callback);

        private static string RecvMsgTmp = null;

        public void Init(string mem_id, UInt32 size)
        {
            ErrorCallbackFunction errorCallback = ErrorCallback;
            var msm_id_ptr = Marshal.StringToHGlobalAnsi(mem_id);
            LMC_init(msm_id_ptr, size, errorCallback);
            Marshal.FreeHGlobal(msm_id_ptr);
        }

        public void Send(string msg)
        {
            lock (this)
            {
                var msg_ptr = Marshal.StringToHGlobalUni(msg);
                LMC_send(msg_ptr);
                Marshal.FreeHGlobal(msg_ptr);
            }
        }

        public bool Tick()
        {
            lock (this)
            {
                return LMC_tick() != 0;
            }
        }

        public bool HasUnsend()
        {
            lock (this)
            {
                return LMC_has_unsend() != 0;
            }
        }

        public void Release()
        {
            LMC_release();
        }

        public string PopRecv()
        {
            lock (this)
            {
                RecvCallbackFunction f = RecvCallback;
                RecvMsgTmp = null;
                LMC_pop_recv(f);
                return RecvMsgTmp;
            }
        }

        private static void RecvCallback(IntPtr msg)
        {
            RecvMsgTmp = Marshal.PtrToStringUni(msg);
        }

        private static void ErrorCallback(int level, IntPtr message)
        {
            string error = Marshal.PtrToStringAnsi(message);
            System.Console.WriteLine("LMC error = " + error);
        }
    }
}
