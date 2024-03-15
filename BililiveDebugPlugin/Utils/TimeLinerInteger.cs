using System;

namespace Utils
{
    public class TimeLinerInteger
        {
            private DateTime UpdateTime;
            private int factor;
            private double Value;
            public double AddFactor { get;set; } = 0.0;
            public double Rest = 0.0;
            public int Limit { get; private set; } = int.MaxValue;
            
            public TimeLinerInteger(int value, int factor = 1,int limit = int.MaxValue)
            {
                this.factor = factor;
                Value = value;
                UpdateTime = DateTime.Now;
                Limit = limit;
            }
            public double val
            {
                get
                {
                    UpdateVal();
                    return Value;
                }
            }

            public int Factor => factor;

            private void UpdateVal()
            {
                if(Value >= Limit)
                {
                    UpdateTime = DateTime.Now;
                    return;
                }
                var ts = DateTime.Now - UpdateTime;
                if (ts.TotalSeconds > 0)
                {
                    var old = Value;
                    var add = (int)(ts.TotalSeconds * factor);
                    Rest += AddFactor * add;
                    if(Rest > 1)
                    {
                        var v = Math.Truncate(Rest);
                        add += (int)v;
                        Rest -= v;
                    }
                    var @new = Value + add;
                    if (old != @new)
                    {
                        Value = @new;
                        UpdateTime = UpdateTime.AddSeconds(ts.TotalSeconds);
                    }
                }
            }
            public void SetNewFactor(int f)
            {
                if (f != factor)
                {
                    UpdateVal();
                    factor = f;
                }
            }
            public double Append(double a)
            {
                UpdateVal();
                Value += a;
                return Value;
            }
            public double Sub(double a)
            {
                UpdateVal();
                Value -= a;
                return Value;
            }
            public double SubNotNeg(double a)
            {
                UpdateVal();
                Value -= a;
                if (Value < 0)
                    Value = 0;
                return Value;
            }
        }
}