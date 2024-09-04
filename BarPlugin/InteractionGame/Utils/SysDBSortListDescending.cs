using BililiveDebugPlugin.DB;
using BililiveDebugPlugin.DB.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace InteractionGame.Utils
{
    public class SysDBSortListDescending<T> : PersistentSortedList<T,string>
        where T : IDataCanSort, IDataWithId<string, T>, ICloneable    {
        protected ESysDataTy _dataTy;
        protected ESysDataTy _dataExpireTime;
        protected int _durationMonth;
        protected int _ExpiredTimeHour = 9;

        public SysDBSortListDescending(ESysDataTy dataTy, ESysDataTy dataExpireTime = ESysDataTy.None,
            int duration = default,int maxCount = -1)
        {
            _dataTy = dataTy;
            _dataExpireTime = dataExpireTime;
            _durationMonth = duration;
            MaxSize = maxCount;
        }

        protected override List<T> OnLoad()
        {
            return DBMgr.Instance.GetListForSys<T>((long)_dataTy);
        }

        protected override void OnSave(List<T> datas)
        {
            DBMgr.Instance.SetListForSys<T>((long)_dataTy, datas);
        }

        protected override int SortFunc(IDataCanSort a, IDataCanSort b)
        {
            return (int)(b.GetSortVal() - a.GetSortVal());
        }
        protected virtual DateTime GetExpiredTime()
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, now.Month, now.Day, _ExpiredTimeHour, 0, 0);
        }
        protected override bool DataExpired
        {
            get
            {
                if(_dataExpireTime == ESysDataTy.None)
                    return false;
                else
                {
                    var time = DBMgr.Instance.GetSystemDataOrCreate((long)_dataExpireTime,out var isNew);
                    if (isNew)
                    {
                        DBMgr.Instance.SetSysValue((long)_dataExpireTime, GetExpiredTime(), out _);
                        return false;
                    }
                    else
                    {
                        if(DateTime.Now >= time.DateTimeValue.AddMonths(_durationMonth))
                        {
                            DBMgr.Instance.SetSysValue((long)_dataExpireTime, GetExpiredTime(), out _);
                            return true;
                        }
                        return false;
                    }
                }
            }
        }
    }
}
