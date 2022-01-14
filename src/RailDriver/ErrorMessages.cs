using System;
using System.Collections.Generic;
using System.Text;

namespace RailDriver
{
    internal static class ErrorMessages
    {
        internal static readonly Dictionary<int, string> Messages = new Dictionary<int, string>
        {
            { 0, "000 Success" },
            { 101, "101 " },
            { 102, "102 " },
            { 104, "104 " },
            { 105, "105 " },
            { 106, "106 " },
            { 107, "107 " },
            { 108, "108 " },
            { 109, "109 " },
            { 110, "110 " },
            { 111, "111 " },
            { 112, "112 " },

            { 201, "201 " },
            { 202, "202 " },
            { 203, "203 Already Connected" },
            { 207, "207 Cannot open read handle" },
            { 204, "204 " },
            { 205, "205 " },
            { 208, "208 Cannot open write handle" },
            { 209, "209 Cannot open either handle" },
            { 210, "210 " },

            { 301, "301 Bad interface handle" },
            { 302, "302 readSize is zero" },
            { 303, "303 Interface not valid" },
            { 304, "304 Ring buffer empty." },
            { 305, "305 " },
            { 307, "307 No readBuffer" },
            { 308, "308 Device disconnected" },
            { 309, "309 Read error. ( unplugged )" },
            { 310, "310 Bytes read not equal readSize" },
            { 311, "311 dest.Length<ReportSize" },

            { 401, "401 Bad interface handle" },
            { 402, "402 Write length is zero" },
            { 403, "403 wData.Length<ReportSize" },
            { 404, "404 WriteBuffer full--retry" },
            { 405, "405 No write buffer" },
            { 406, "406 Interface not valid" },
            { 407, "407 No writeBuffer" },
            { 408, "408 Device disconnected" },
            { 409, "409 Write error. ( unplugged )" },
            { 410, "410 byteCount != writeSize" },
            { 411, "411 Timed out in write." },
            { 412, "412 Report ID error" },
            { 501, "501 " },
            { 502, "502 Read length is zero" },
            { 503, "503 dest.Length<ReportSize" },
            { 504, "504 No data yet." },
            { 507, "507 Interface not valid." },

            { 601, "601 " },
            { 602, "602 " },

            { 701, "701 " },
            { 702, "702 Interface not valid" },
            { 703, "703 Input ReportSize Zero" },
            { 704, "704 Data Handler Already Exists" },

            { 801, "801 " },
            { 802, "802 Interface not valid" },
            { 803, "803 " },
            { 804, "804 Error Handler Already Exists" },
        };
    }
}
