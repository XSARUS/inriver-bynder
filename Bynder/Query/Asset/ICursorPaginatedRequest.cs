using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Query.Asset
{
    public interface ICursorPaginatedRequest
    {
        int? Limit { get; }
        void SetCursor(string cursor);
    }
}
