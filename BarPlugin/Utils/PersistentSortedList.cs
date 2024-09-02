using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public abstract class PersistentSortedList<T,IDT>
        where T : IDataCanSort, IDataWithId<IDT,T>
        where IDT : IEquatable<IDT>
    {
        private List<T> datas;
        public List<T> Datas => datas;
        protected int MaxSize = -1;
        public void Load()
        {
            if (DataExpired)
            {
                datas = new List<T>();
                return;
            }
            datas = OnLoad();
            if (datas == null)
                datas = new List<T>();
        }
        public void Save()
        {
            CheckDataSize();
            OnSave(datas);
        }
        private void CheckDataSize()
        {
            if (MaxSize <= -1)
                return;
            while(datas.Count > 0 && datas.Count > MaxSize)
            {
                datas.RemoveAt(datas.Count - 1);
            }
        }
        public bool TestBeNecessaryAddAndReSort(IDataCanSort sortedFirstData)
        {
            if (MaxSize < 0)
                return true;
            if (datas.Count >= MaxSize && SortFunc(datas[MaxSize - 1], sortedFirstData) <= 0)
                return false;
            return true;
        }
        public void Append(List<T> data,bool useAdd = true)
        {
            foreach(var it in data)
            {
                var j = datas.Find(a=> a.GetId().Equals(it.GetId()));
                if (j != null)
                {
                    if(useAdd)
                        j.AddValue(it);
                    else
                        j.SetValue(it);
                }
                else
                    datas.Add(it);
            }
        }
        public void Sort()
        {
            datas.Sort((a,b)=>SortFunc(a,b));
        }
        protected abstract void OnSave(List<T> datas);
        protected abstract List<T> OnLoad();
        protected abstract int SortFunc(IDataCanSort a, IDataCanSort b);
        protected virtual bool DataExpired => false;
    }
}
