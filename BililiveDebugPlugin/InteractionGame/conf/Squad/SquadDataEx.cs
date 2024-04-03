using System;
using System.Collections.Generic;

namespace conf.Squad
{
    public partial class SquadData
    {
        public String GetBlueprint(int g)
        {
            if (GetOverload(g, out var data))
                return data.GetBlueprint(g);
            return IsBase ? String.Concat(PB, SettingMgr.GetCountry(g)) : PB;
        }

        public bool GetOverload(int g,out SquadData data)
        {
            var country = SettingMgr.GetCountry(g);
            if (CountryMap != null && CountryMap.TryGetValue(country, out var id))
            {
                data = SquadDataMgr.GetInstance().Get(id);
                return true;
            }
            data = null;
            return false;
        }
        public double OverlaodPriceMult(int g)
        {
            double m = 1;
            if ((OverloadPriceMult?.TryGetValue(SettingMgr.GetCountry(g), out m)).GetValueOrDefault(false))
                return m;
            return 1;
        }
        public double OverlaodPrice(int g)
        {
            double v = Price;
            if ((OverloadPrice?.TryGetValue(SettingMgr.GetCountry(g), out v)).GetValueOrDefault(false))
                return v * OverlaodPriceMult(g);
            return Price * OverlaodPriceMult(g);
        }
        public double OverlaodUpPriceMult(int g)
        {
            double m = 1;
            if ((OverloadUpPriceMult?.TryGetValue(SettingMgr.GetCountry(g), out m)).GetValueOrDefault(false))
                return m;
            return 1;
        }
        public double OverlaodUpPrice(int g)
        {
            double v = UpLevelPrice;
            if ((OverloadUpPrice?.TryGetValue(SettingMgr.GetCountry(g), out v)).GetValueOrDefault(false))
                return v * OverlaodUpPriceMult(g);
            return UpLevelPrice * OverlaodUpPriceMult(g);
        }
        public bool HasOverloadPrice(int g) => (OverloadPrice?.ContainsKey(SettingMgr.GetCountry(g))).GetValueOrDefault(false) || (OverloadPriceMult?.ContainsKey(SettingMgr.GetCountry(g))).GetValueOrDefault(false);
        public double CalcScore(int g) => HasOverloadPrice(g) ? RealPrice(g) : Score;
        public double CalcTrainTime(int g) => HasOverloadPrice(g) ? RealPrice(g) * 0.33333333 : TrainTime;
        public System.String RealName(int g) => GetOverload(g,out var v) ? v.Name : Name;
        public System.Double RealPrice(int g) => GetOverload(g,out var v) ? v.OverlaodPrice(g) : OverlaodPrice(g);
        public System.Double RealScore(int g) => GetOverload(g,out var v) ? v.CalcScore(g) : CalcScore(g);
        public System.Int32 RealSquadType(int g) => GetOverload(g,out var v) ? v.SquadType : SquadType;
        public ESquadType RealSquadType_e(int g) => (ESquadType)RealSquadType(g);
        public System.Double RealTrainTime(int g) => GetOverload(g,out var v) ? v.CalcTrainTime(g) : CalcTrainTime(g);
        public System.Int32 RealType(int g) => GetOverload(g,out var v) ? v.Type : Type;
        public EType RealType_e(int g) => (EType)RealType(g);
        public System.Int32 RealNextLevel(int g) => GetOverload(g,out var v) ? v.NextLevel : NextLevel;
        public Squad.SquadData RealNextLevelRef(int g) => Squad.SquadDataMgr.GetInstance().Get(RealNextLevel(g));
        public System.Double RealUpLevelPrice(int g) => GetOverload(g, out var v) ? v.OverlaodUpPrice(g) : OverlaodUpPrice(g);
        public bool RealHasNextLevel(int g) => RealNextLevel(g) > 0;
        public int QuickSuccessionNum => 3;
        public int Level => RealId / 10_0000;
        public int Sid => RealId % 10_0000;
        public int RealId => OverloadId > 0 ? OverloadId : Id;
        public byte GetAddHp(int g)
        {
            if (SquadType != 0 && AddHP != null)
                return GetValByDict(AddHP, g, (byte)0);
            return 0;
        }
        public byte GetAddDamage(int g)
        {
            if (SquadType != 0 && AddDamage != null)
                return GetValByDict(AddDamage,g,(byte)0);
            return 0;
        }
        protected T GetValByDict<T>(Dictionary<string,T> dict,int g,T def = default(T))
        {
            if (dict.TryGetValue(SettingMgr.GetCountry(g), out var v))
                return v;
            if (dict.TryGetValue("d", out v))
                return v;
            return def;
        }
    }

    public partial class SquadDataMgr
    {
        public List<SquadData> NormalSquadDatas = new List<SquadData>();
        public List<SquadData> GiftSquadDatas = new List<SquadData>();
        public Dictionary<string,SquadData> CountrySpecialMap = new Dictionary<string,SquadData>();
        private static readonly Random _r = new Random(DateTime.Now.Millisecond);
        
        public void OnLoaded()
        {
            lock (this)
            {
                NormalSquadDatas.Clear();
                GiftSquadDatas.Clear();
                CountrySpecialMap.Clear();
                foreach (var data in _dict)
                {
                    switch (data.Value.Type_e)
                    {
                        case EType.Normal:
                            if ((data.Key / 10_0000) == 1)
                                NormalSquadDatas.Add(data.Value);
                            break;
                        case EType.Gift:
                            GiftSquadDatas.Add(data.Value);
                            break;
                        case EType.CountrySpec:
                            break;
                    }
                    if (data.Value.OverloadCountry != null && data.Value.OverloadCountry.Length > 0 && data.Value.OverloadId > 0)
                        CountrySpecialMap.Add($"{data.Value.OverloadCountry}_{data.Value.OverloadId}", data.Value);
                }
            }
        }

        public SquadData RandomSquad(List<SquadData> squadDatas)
        {
            lock (this)
            {
                if(squadDatas.Count == 0) return null;
                var r = _r.Next(0, squadDatas.Count);
                return squadDatas[r];
            }
        }
        public SquadData GetCountrySpecialSquad(int index, int level, int g)
        {
            var id = level * 10_0000 + index;
            var country = SettingMgr.GetCountry(g);
            lock (this)
            {
                if (CountrySpecialMap.TryGetValue($"{country}_{id}",out var v))
                    return v;
            }
            return null;
        }

        public SquadData RandomNormalSquad => RandomSquad(NormalSquadDatas);
        public SquadData RandomGiftSquad => RandomSquad(GiftSquadDatas);
    }
}