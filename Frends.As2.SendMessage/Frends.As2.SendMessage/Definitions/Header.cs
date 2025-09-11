using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.As2.SendMessage.Definitions
{
    /// <summary>
    /// Request header.
    /// </summary>
    public class Header
    {
        /// <summary>
        /// Name of header.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of header.
        /// </summary>
        public string Value { get; set; }
    }
}
