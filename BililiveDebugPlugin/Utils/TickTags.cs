
using System.Text;

public class TickTagDynGeneric : TickTag
{
    protected TickTag[] m_Generic;

    public TickTagDynGeneric()
    { }
    public TickTagDynGeneric(TickTag t)
    {
        m_Generic = new TickTag[] { t };
    }
    public TickTagDynGeneric(TickTag[] ts)
    {
        m_Generic = ts;
    }
    
    public TickTag this[int i]
    {
        get
        {
            if (i >= 0 && i < m_Generic.Length)
                return m_Generic[i];
            return null;
        }
    }

    public override bool Eq(TickTag obj)
    {
        if (obj.GetType() == GetType() && obj is TickTagDynGeneric ttdc && ttdc.m_Generic != null && ttdc.m_Generic.Length == m_Generic.Length)
        {
            for (int i = 0; i < m_Generic.Length; ++i)
            {
                if(!m_Generic[i].Eq(ttdc.m_Generic[i]))
                    return false;
            }
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(GetType().FullName);
        for (int i = 0; i < m_Generic.Length; ++i)
        {
            sb.Append(m_Generic[i].ToString());
            if (i + 1 < m_Generic.Length)
                sb.Append(',');
        }
        return sb.ToString();
    }

    public void SetGeneric(TickTag[] ts)
    {
        m_Generic = ts;
    }
}

public class TickTagDynGenericNum : TickTag
{
    protected int[] m_Generic;
    public TickTagDynGenericNum(){}
    public TickTagDynGenericNum(int t)
    {
        m_Generic = new int[] { t };
    }
    public TickTagDynGenericNum(int[] ts)
    {
        m_Generic = ts;
    }

    public override bool Eq(TickTag obj)
    {
        if (obj.GetType() == GetType() && obj is TickTagDynGenericNum ttdc && ttdc.m_Generic != null && ttdc.m_Generic.Length == m_Generic.Length)
        {
            for (int i = 0; i < m_Generic.Length; ++i)
            {
                if(m_Generic[i] != ttdc.m_Generic[i])
                    return false;
            }
            return true;
        }
        return false;
    }
    
    public int this[int i]
    {
        get
        {
            if (i >= 0 && i < m_Generic.Length)
                return m_Generic[i];
            return -1;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(GetType().FullName);
        for (int i = 0; i < m_Generic.Length; ++i)
        {
            sb.Append(m_Generic[i].ToString());
            if (i + 1 < m_Generic.Length)
                sb.Append(',');
        }
        return sb.ToString();
    }
    public void SetGeneric(int[] ts)
    {
        m_Generic = ts;
    }
}