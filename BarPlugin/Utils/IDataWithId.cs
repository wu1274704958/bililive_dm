using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public interface IDataWithId<T,Oth>
        where Oth : IDataWithId<T, Oth>
    {
        T GetId();
        void SetValue(Oth oth);
        void AddValue(Oth oth);
    }
}
