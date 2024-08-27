using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


public class LocalMemComm
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    protected delegate void ErrorCallbackFunction(UInt32 id, IntPtr message);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    protected delegate void RecvCallbackFunction(IntPtr message);

    [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    protected static extern UInt32 LMC_init(IntPtr mem_id, UInt32 size, ErrorCallbackFunction callback);
    [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    protected static extern void LMC_send(UInt32 id, IntPtr msg);
    [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
    protected static extern int LMC_tick(UInt32 id);
    [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
    protected static extern int LMC_has_unsend(UInt32 id);
    [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
    protected static extern void LMC_release(UInt32 id);
    [DllImport("lmc.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
    protected static extern void LMC_pop_recv(UInt32 id, RecvCallbackFunction callback);
    [DllImport("lmc.dll", CallingConvention = CallingConvention.Cdecl)]
    protected static extern void LMC_reset(UInt32 id);

    private static string RecvMsgTmp = null;
    private static object RecvMsgLockObj = new object();

    private UInt32 m_id = 0;
    private Action<String> OnErrorCallback;
    private static Dictionary<UInt32, LocalMemComm> InsDict = new Dictionary<UInt32, LocalMemComm>();

    private static void RegisterInstance(LocalMemComm inst)
    {
        if (inst.m_id > 0 && !InsDict.ContainsKey(inst.m_id))
        {
            InsDict.Add(inst.m_id, inst);
        }
    }
    private static void UnregisterInstance(LocalMemComm inst)
    {
        if (inst.m_id > 0)
        {
            InsDict.Remove(inst.m_id);
        }
    }

    public void Init(string mem_id, UInt32 size)
    {
        ErrorCallbackFunction errorCallback = ErrorCallback;
        var msm_id_ptr = Marshal.StringToHGlobalAnsi(mem_id);
        m_id = LMC_init(msm_id_ptr, size, errorCallback);
        Marshal.FreeHGlobal(msm_id_ptr);
        RegisterInstance(this);
    }

    public void Send(string msg)
    {
        if (m_id <= 0) return;
        lock (this)
        {
            var msg_ptr = Marshal.StringToHGlobalUni(msg);
            LMC_send(m_id, msg_ptr);
            Marshal.FreeHGlobal(msg_ptr);
        }
    }

    public bool Tick()
    {
        if (m_id <= 0) return false;
        lock (this)
        {
            return LMC_tick(m_id) != 0;
        }
    }

    public bool HasUnsend()
    {
        if (m_id <= 0) return false;
        lock (this)
        {
            return LMC_has_unsend(m_id) != 0;
        }
    }

    public void Release()
    {
        if (m_id <= 0) return;
        LMC_release(m_id);
        UnregisterInstance(this);
        m_id = 0;
    }

    public string PopRecv()
    {
        if (m_id <= 0) return null;
        lock (this)
        {
            RecvCallbackFunction f = RecvCallback;
            string res = null;
            lock (RecvMsgLockObj)
            {
                RecvMsgTmp = null;
                LMC_pop_recv(m_id, f);
                res = RecvMsgTmp;
            }
            return res;
        }
    }

    public void Reset()
    {
        if (m_id <= 0) return;
        lock (this)
        {
            LMC_reset(m_id);
        }
    }

    private static void RecvCallback(IntPtr msg)
    {
        RecvMsgTmp = Marshal.PtrToStringUni(msg);
    }

    private static void ErrorCallback(UInt32 id, IntPtr message)
    {
        string error = Marshal.PtrToStringAnsi(message);
        if (error != null && InsDict.TryGetValue(id, out var v))
        {
            if (v.OnErrorCallback != null)
            {
                v.OnErrorCallback.Invoke(error);
            }
        }
    }
}

