using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiUDatabaseUpdater.Eshop
{
    class TitleSet
    {
        public HashSet<EshopTitle> Titles { get; private set; }
        public bool Modified { get; set; }

        public TitleSet()
        {
            Titles = new HashSet<EshopTitle>();
            Modified = false;
        }
    }
}
