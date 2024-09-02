using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public abstract class PersistentSortedList<T>
        where T : IDataCanSort
    {
        private List<T> datas;
        private int MaxSize = -1;
        public void Load()
        {
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
            if(datas.Count >= MaxSize && SortFunc(datas[MaxSize - 1],sortedFirstData) > 0)
            {
                return true;
            }
            return false;
        }
        protected abstract void OnSave(List<T> datas);
        protected abstract List<T> OnLoad();
        protected abstract int SortFunc(IDataCanSort a, IDataCanSort b);
    }
}
